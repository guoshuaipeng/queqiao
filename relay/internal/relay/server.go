package relay

import (
	"bufio"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"log/slog"
	"net"
	"strings"
	"sync"
	"sync/atomic"
	"time"
)

const (
	DefaultIdleTimeout = 10 * time.Minute
	DefaultMaxSessions = 64
	controlDeadline    = 30 * time.Second
	maxLineBytes       = 16 * 1024
)

// Config holds listen and resource limits for a small VPS (e.g. 2C2G).
type Config struct {
	Addr               string
	DataPath           string
	IdleTimeout        time.Duration
	MaxSessions        int
	Logger             *slog.Logger
	Store              *Store // optional; created from DataPath if nil
	AuditAddr          string // RELAY_AUDIT_ADDR; default loopback when token set
	AuditToken         string // RELAY_AUDIT_TOKEN; empty = audit HTTP off
	AuditRetentionDays int    // default 90
}

// Server is a JSONL-control TCP relay with guest + account pairing.
type Server struct {
	cfg       Config
	store     *Store
	ln        net.Listener
	mu        sync.Mutex
	guests    map[string]*pairSlot // code -> waiting agent
	devices   map[string]*pairSlot // userID|deviceID -> waiting agent
	pipes     int                  // active transparent pipes
	slots     int                  // waiting + piping slots
	closing   atomic.Bool
	wg        sync.WaitGroup
	ownStore  bool
	auditHTTP *AuditHTTP
}

type pairSlot struct {
	key     string
	kind    string // guest | account
	mode    string // guest | account
	agent   net.Conn
	agentBr *bufio.Reader
	helper  net.Conn
	userID  int64
	paired  chan struct{}
	done    chan struct{}
	once    sync.Once
}

type clientConn struct {
	s      *Server
	conn   net.Conn
	br     *bufio.Reader
	userID int64
	authed bool
}

// NewServer constructs a relay server. Call Close to release the store if owned.
func NewServer(cfg Config) (*Server, error) {
	if cfg.IdleTimeout <= 0 {
		cfg.IdleTimeout = DefaultIdleTimeout
	}
	if cfg.MaxSessions <= 0 {
		cfg.MaxSessions = DefaultMaxSessions
	}
	if cfg.Logger == nil {
		cfg.Logger = slog.Default()
	}
	if cfg.Addr == "" {
		cfg.Addr = ":17891"
	}
	srv := &Server{
		cfg:     cfg,
		guests:  make(map[string]*pairSlot),
		devices: make(map[string]*pairSlot),
	}
	if cfg.Store != nil {
		srv.store = cfg.Store
	} else {
		st, err := OpenStore(cfg.DataPath)
		if err != nil {
			return nil, err
		}
		srv.store = st
		srv.ownStore = true
	}
	if n, err := srv.store.PurgeGuestAuditOlderThan(cfg.AuditRetentionDays); err != nil {
		srv.cfg.Logger.Error("purge guest_audit", "err", err)
	} else if n > 0 {
		srv.cfg.Logger.Info("purged guest_audit", "rows", n)
	}
	ah, err := StartAuditHTTP(srv.store, cfg.AuditAddr, cfg.AuditToken, cfg.Logger)
	if err != nil {
		if srv.ownStore {
			_ = srv.store.Close()
		}
		return nil, err
	}
	srv.auditHTTP = ah
	return srv, nil
}

func (s *Server) ListenAndServe() error {
	ln, err := net.Listen("tcp", s.cfg.Addr)
	if err != nil {
		return err
	}
	return s.Serve(ln)
}

// Serve accepts on an existing listener (tests pass a local one).
func (s *Server) Serve(ln net.Listener) error {
	s.ln = ln
	for {
		conn, err := ln.Accept()
		if err != nil {
			if s.closing.Load() {
				return nil
			}
			return err
		}
		s.wg.Add(1)
		go func() {
			defer s.wg.Done()
			s.handleConn(conn)
		}()
	}
}

func (s *Server) Close() error {
	s.closing.Store(true)
	var err error
	if s.ln != nil {
		err = s.ln.Close()
	}
	s.mu.Lock()
	for _, slot := range s.guests {
		s.finishSlotLocked(slot)
	}
	s.guests = make(map[string]*pairSlot)
	for _, slot := range s.devices {
		s.finishSlotLocked(slot)
	}
	s.devices = make(map[string]*pairSlot)
	s.mu.Unlock()
	s.wg.Wait()
	if s.auditHTTP != nil {
		if cerr := s.auditHTTP.Close(); cerr != nil && err == nil {
			err = cerr
		}
	}
	if s.ownStore {
		if cerr := s.store.Close(); cerr != nil && err == nil {
			err = cerr
		}
	}
	return err
}

func (s *Server) Addr() net.Addr {
	if s.ln == nil {
		return nil
	}
	return s.ln.Addr()
}

func (s *Server) PipeCount() int {
	s.mu.Lock()
	defer s.mu.Unlock()
	return s.pipes
}

func (s *Server) handleConn(conn net.Conn) {
	cc := &clientConn{
		s:    s,
		conn: conn,
		br:   bufio.NewReaderSize(conn, maxLineBytes),
	}
	defer func() {
		_ = conn.Close()
	}()

	for {
		_ = conn.SetDeadline(time.Now().Add(controlDeadline))
		line, err := cc.br.ReadString('\n')
		if err != nil {
			if !errors.Is(err, io.EOF) {
				s.cfg.Logger.Debug("control read end", "remote", conn.RemoteAddr().String(), "err", err)
			}
			return
		}
		line = strings.TrimSpace(line)
		if line == "" {
			continue
		}

		_ = conn.SetDeadline(time.Time{})

		var req map[string]any
		if err := json.Unmarshal([]byte(line), &req); err != nil {
			_ = cc.reply(map[string]any{"ok": false, "op": "", "error": "invalid json"})
			continue
		}
		op := strings.ToLower(strings.TrimSpace(asString(req["op"])))
		if op == "" {
			_ = cc.reply(map[string]any{"ok": false, "op": "", "error": "missing op"})
			continue
		}

		paired, err := cc.dispatch(op, req)
		if err != nil {
			_ = cc.reply(map[string]any{"ok": false, "op": op, "error": err.Error()})
			// auth errors keep the connection for retry; fatal pairing setup already replied
			continue
		}
		if paired {
			// ownership transferred to pipe; do not close in defer sense — pipe closes both
			// Prevent defer from double-closing harmlessly is fine.
			return
		}
	}
}

func (c *clientConn) reply(v map[string]any) error {
	b, err := json.Marshal(v)
	if err != nil {
		return err
	}
	_, err = c.conn.Write(append(b, '\n'))
	return err
}

// dispatch returns paired=true when this conn entered the raw pipe (caller should stop control loop).
func (c *clientConn) dispatch(op string, req map[string]any) (paired bool, err error) {
	switch op {
	case "signup":
		return false, c.opSignup(req)
	case "login":
		return false, c.opLogin(req)
	case "auth_token":
		return false, c.opAuthToken(req)
	case "device_online":
		return c.opDeviceOnline(req)
	case "device_offline":
		return false, c.opDeviceOffline(req)
	case "list_devices":
		return false, c.opListDevices(req)
	case "connect_device":
		return c.opConnectDevice(req)
	case "guest_host":
		return c.opGuestHost(req)
	case "guest_join":
		return c.opGuestJoin(req)
	default:
		return false, fmt.Errorf("unknown op %s", op)
	}
}

func (c *clientConn) opSignup(req map[string]any) error {
	username := asString(req["username"])
	password := asString(req["password"])
	id, err := c.s.store.Signup(username, password)
	if err != nil {
		return err
	}
	return c.reply(map[string]any{"ok": true, "op": "signup", "user_id": id})
}

func (c *clientConn) opLogin(req map[string]any) error {
	username := asString(req["username"])
	password := asString(req["password"])
	id, token, err := c.s.store.Login(username, password)
	if err != nil {
		return err
	}
	c.userID = id
	c.authed = true
	return c.reply(map[string]any{"ok": true, "op": "login", "token": token, "user_id": id})
}

func (c *clientConn) opAuthToken(req map[string]any) error {
	token := asString(req["token"])
	id, err := c.s.store.AuthToken(token)
	if err != nil {
		return err
	}
	c.userID = id
	c.authed = true
	return c.reply(map[string]any{"ok": true, "op": "auth_token", "user_id": id})
}

func (c *clientConn) requireAuth() error {
	if !c.authed || c.userID == 0 {
		return fmt.Errorf("unauthorized")
	}
	return nil
}

func deviceKey(userID int64, deviceID string) string {
	return fmt.Sprintf("%d|%s", userID, deviceID)
}

func (c *clientConn) opDeviceOnline(req map[string]any) (bool, error) {
	if err := c.requireAuth(); err != nil {
		return false, err
	}
	deviceID := strings.TrimSpace(asString(req["device_id"]))
	displayName := strings.TrimSpace(asString(req["display_name"]))
	if deviceID == "" {
		return false, fmt.Errorf("missing field device_id")
	}
	if err := c.s.store.UpsertDevice(c.userID, deviceID, displayName); err != nil {
		return false, err
	}

	key := deviceKey(c.userID, deviceID)
	slot, err := c.s.registerAgent("account", "account", key, c.userID, c.conn, c.br)
	if err != nil {
		return false, err
	}
	if err := c.reply(map[string]any{"ok": true, "op": "device_online", "device_id": deviceID}); err != nil {
		c.s.abandonAgent(slot, c.conn)
		return false, err
	}
	return c.waitAndPipe(slot, "account"), nil
}

func (c *clientConn) opDeviceOffline(req map[string]any) error {
	if err := c.requireAuth(); err != nil {
		return err
	}
	deviceID := strings.TrimSpace(asString(req["device_id"]))
	if deviceID == "" {
		return fmt.Errorf("missing field device_id")
	}
	key := deviceKey(c.userID, deviceID)
	c.s.mu.Lock()
	slot := c.s.devices[key]
	if slot != nil && slot.agent != nil {
		c.s.finishSlotLocked(slot)
		delete(c.s.devices, key)
	}
	c.s.mu.Unlock()
	return c.reply(map[string]any{"ok": true, "op": "device_offline", "device_id": deviceID})
}

func (c *clientConn) opListDevices(req map[string]any) error {
	if err := c.requireAuth(); err != nil {
		return err
	}
	rows, err := c.s.store.ListDevices(c.userID)
	if err != nil {
		return err
	}
	c.s.mu.Lock()
	for i := range rows {
		key := deviceKey(c.userID, rows[i].DeviceID)
		if slot, ok := c.s.devices[key]; ok && slot.agent != nil && slot.helper == nil {
			rows[i].Online = true
		}
	}
	c.s.mu.Unlock()
	return c.reply(map[string]any{"ok": true, "op": "list_devices", "devices": rows})
}

func (c *clientConn) opConnectDevice(req map[string]any) (bool, error) {
	if err := c.requireAuth(); err != nil {
		return false, err
	}
	deviceID := strings.TrimSpace(asString(req["device_id"]))
	if deviceID == "" {
		return false, fmt.Errorf("missing field device_id")
	}
	own, err := c.s.store.OwnDevice(c.userID, deviceID)
	if err != nil {
		return false, err
	}
	if !own {
		return false, fmt.Errorf("device not found")
	}
	key := deviceKey(c.userID, deviceID)
	slot, peer, err := c.s.registerHelper("account", key, c.conn)
	if err != nil {
		return false, err
	}
	if err := c.reply(map[string]any{"ok": true, "op": "paired", "mode": "account", "device_id": deviceID}); err != nil {
		c.s.abandonHelper(slot, c.conn)
		return false, err
	}
	// notify agent
	_ = writeJSONLine(peer, map[string]any{"ok": true, "op": "paired", "mode": "account", "device_id": deviceID})
	c.s.runPipe(slot, peer, c.conn, slot.agentBr, c.br)
	return true, nil
}

func (c *clientConn) opGuestHost(req map[string]any) (bool, error) {
	password := asString(req["password"])
	if len(password) < minPasswordLen {
		return false, fmt.Errorf("password too short (min %d)", minPasswordLen)
	}
	code := normalizeCode(asString(req["code"]))
	if code == "" {
		var err error
		code, err = generateCode(6)
		if err != nil {
			return false, err
		}
	}
	if len(code) < 4 || len(code) > 16 {
		return false, fmt.Errorf("invalid code")
	}
	exp := time.Now().Add(guestTTL)
	if err := c.s.store.SaveGuest(code, password, exp); err != nil {
		return false, err
	}
	c.s.safeAppendAudit(AuditInsert{Event: AuditGenerated, Code: code, OK: intPtr(1)})

	slot, err := c.s.registerAgent("guest", "guest", code, 0, c.conn, c.br)
	if err != nil {
		return false, err
	}
	if err := c.reply(map[string]any{"ok": true, "op": "guest_host", "code": code, "expires_at": exp.Unix()}); err != nil {
		c.s.abandonAgent(slot, c.conn)
		_ = c.s.store.DeleteGuest(code)
		return false, err
	}
	return c.waitAndPipe(slot, "guest"), nil
}

func (c *clientConn) opGuestJoin(req map[string]any) (bool, error) {
	code := normalizeCode(asString(req["code"]))
	password := asString(req["password"])
	fail := func(err error) (bool, error) {
		reason := classifyJoinFail(err)
		c.s.safeAppendAudit(AuditInsert{
			Event: AuditJoinFailed, Code: code, OK: intPtr(0), FailReason: reason, PeerRole: "helper",
		})
		return false, err
	}
	if code == "" {
		return fail(fmt.Errorf("missing field code"))
	}
	if len(password) < minPasswordLen {
		return fail(fmt.Errorf("password too short (min %d)", minPasswordLen))
	}
	ok, err := c.s.store.GuestPasswordOK(code, password)
	if err != nil {
		return fail(err)
	}
	if !ok {
		return fail(fmt.Errorf("invalid code or password"))
	}
	slot, peer, err := c.s.registerHelper("guest", code, c.conn)
	if err != nil {
		return fail(err)
	}
	now := time.Now().Unix()
	c.s.safeAppendAudit(AuditInsert{
		Event: AuditPaired, Code: code, OK: intPtr(1), PeerRole: "helper", PairedAt: int64Ptr(now),
	})
	if err := c.reply(map[string]any{"ok": true, "op": "paired", "mode": "guest", "code": code}); err != nil {
		c.s.abandonHelper(slot, c.conn)
		return false, err
	}
	_ = writeJSONLine(peer, map[string]any{"ok": true, "op": "paired", "mode": "guest", "code": code})
	c.s.runPipe(slot, peer, c.conn, slot.agentBr, c.br)
	return true, nil
}

func writeJSONLine(w io.Writer, v map[string]any) error {
	b, err := json.Marshal(v)
	if err != nil {
		return err
	}
	_, err = w.Write(append(b, '\n'))
	return err
}

func (c *clientConn) waitAndPipe(slot *pairSlot, mode string) bool {
	timer := time.NewTimer(c.s.cfg.IdleTimeout)
	defer timer.Stop()
	select {
	case <-slot.paired:
		// helper side owns the pipe; wait until done
		<-slot.done
		return true
	case <-slot.done:
		if mode == "guest" {
			select {
			case <-slot.paired:
			default:
				c.s.safeAppendAudit(AuditInsert{
					Event: AuditAbandoned, Code: slot.key, OK: intPtr(0), PeerRole: "agent",
				})
			}
		}
		return true
	case <-timer.C:
		c.s.cfg.Logger.Info("wait timeout", "key", slot.key, "mode", mode)
		if mode == "guest" {
			c.s.safeAppendAudit(AuditInsert{
				Event: AuditWaitTimeout, Code: slot.key, OK: intPtr(0), PeerRole: "agent",
			})
		}
		c.s.abandonAgent(slot, c.conn)
		_ = c.reply(map[string]any{"ok": false, "op": "paired", "error": "wait timeout"})
		return true
	}
}

func (s *Server) registerAgent(kind, mode, key string, userID int64, conn net.Conn, br *bufio.Reader) (*pairSlot, error) {
	s.mu.Lock()
	defer s.mu.Unlock()
	if s.slots >= s.cfg.MaxSessions {
		return nil, fmt.Errorf("too many sessions (max %d)", s.cfg.MaxSessions)
	}
	var m map[string]*pairSlot
	if kind == "guest" {
		m = s.guests
	} else {
		m = s.devices
	}
	if existing, ok := m[key]; ok && existing.agent != nil {
		return nil, fmt.Errorf("agent already online")
	}
	slot := &pairSlot{
		key:     key,
		kind:    kind,
		mode:    mode,
		agent:   conn,
		agentBr: br,
		userID:  userID,
		paired:  make(chan struct{}),
		done:    make(chan struct{}),
	}
	m[key] = slot
	s.slots++
	s.cfg.Logger.Info("agent waiting", "kind", kind, "key", key)
	return slot, nil
}

func (s *Server) registerHelper(kind, key string, conn net.Conn) (*pairSlot, net.Conn, error) {
	s.mu.Lock()
	defer s.mu.Unlock()
	var m map[string]*pairSlot
	if kind == "guest" {
		m = s.guests
	} else {
		m = s.devices
	}
	slot, ok := m[key]
	if !ok || slot.agent == nil {
		return nil, nil, fmt.Errorf("peer not online")
	}
	if slot.helper != nil {
		return nil, nil, fmt.Errorf("helper already connected")
	}
	if s.pipes >= s.cfg.MaxSessions {
		return nil, nil, fmt.Errorf("too many sessions (max %d)", s.cfg.MaxSessions)
	}
	slot.helper = conn
	peer := slot.agent
	select {
	case <-slot.paired:
	default:
		close(slot.paired)
	}
	s.pipes++
	s.cfg.Logger.Info("session paired", "kind", kind, "key", key)
	return slot, peer, nil
}

func (s *Server) abandonAgent(slot *pairSlot, conn net.Conn) {
	s.mu.Lock()
	defer s.mu.Unlock()
	if slot == nil {
		return
	}
	if slot.agent == conn {
		slot.agent = nil
	}
	s.removeIfEmptyLocked(slot)
}

func (s *Server) abandonHelper(slot *pairSlot, conn net.Conn) {
	s.mu.Lock()
	defer s.mu.Unlock()
	if slot == nil {
		return
	}
	if slot.helper == conn {
		slot.helper = nil
		if s.pipes > 0 {
			s.pipes--
		}
	}
	s.removeIfEmptyLocked(slot)
}

func (s *Server) removeIfEmptyLocked(slot *pairSlot) {
	if slot.agent != nil || slot.helper != nil {
		return
	}
	s.finishSlotLocked(slot)
	if slot.kind == "guest" {
		delete(s.guests, slot.key)
		_ = s.store.DeleteGuest(slot.key)
	} else {
		delete(s.devices, slot.key)
	}
	if s.slots > 0 {
		s.slots--
	}
}

func (s *Server) finishSlotLocked(slot *pairSlot) {
	slot.once.Do(func() { close(slot.done) })
	if slot.agent != nil {
		_ = slot.agent.Close()
		slot.agent = nil
	}
	if slot.helper != nil {
		_ = slot.helper.Close()
		slot.helper = nil
	}
}

func (s *Server) runPipe(slot *pairSlot, a, b net.Conn, aR, bR *bufio.Reader) {
	// aR/bR may hold bytes that arrived after the last control line; feed them into the pipe.
	s.pipePair(slot.key, a, b, aR, bR)

	if slot.kind == "guest" {
		now := time.Now().Unix()
		s.safeAppendAudit(AuditInsert{
			Event: AuditEnded, Code: slot.key, OK: intPtr(1), ClosedAt: int64Ptr(now),
		})
	}

	s.mu.Lock()
	if slot.kind == "guest" {
		if cur, ok := s.guests[slot.key]; ok && cur == slot {
			s.finishSlotLocked(slot)
			delete(s.guests, slot.key)
			_ = s.store.DeleteGuest(slot.key)
			if s.slots > 0 {
				s.slots--
			}
		}
	} else {
		if cur, ok := s.devices[slot.key]; ok && cur == slot {
			s.finishSlotLocked(slot)
			delete(s.devices, slot.key)
			if s.slots > 0 {
				s.slots--
			}
		}
	}
	if s.pipes > 0 {
		s.pipes--
	}
	s.mu.Unlock()
	s.cfg.Logger.Info("session closed", "key", slot.key, "kind", slot.kind)
}
