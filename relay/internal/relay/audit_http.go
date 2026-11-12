package relay

import (
	"crypto/subtle"
	"encoding/json"
	"log/slog"
	"net"
	"net/http"
	"strconv"
	"strings"
	"time"
)

const defaultAuditAddr = "127.0.0.1:17992"

// AuditHTTP serves loopback-only read-only guest audit events.
type AuditHTTP struct {
	store  *Store
	token  string
	logger *slog.Logger
	srv    *http.Server
}

// StartAuditHTTP starts the audit management HTTP if token is non-empty.
// addr empty => default 127.0.0.1:17992. Returns nil,nil when token unset (feature off).
func StartAuditHTTP(store *Store, addr, token string, logger *slog.Logger) (*AuditHTTP, error) {
	token = strings.TrimSpace(token)
	if token == "" {
		return nil, nil
	}
	if store == nil {
		return nil, errString("audit store required")
	}
	if logger == nil {
		logger = slog.Default()
	}
	addr = strings.TrimSpace(addr)
	if addr == "" {
		addr = defaultAuditAddr
	}
	a := &AuditHTTP{store: store, token: token, logger: logger}
	mux := http.NewServeMux()
	mux.HandleFunc("GET /audit/events", a.handleListEvents)
	mux.HandleFunc("GET /healthz", func(w http.ResponseWriter, _ *http.Request) {
		w.WriteHeader(http.StatusOK)
		_, _ = w.Write([]byte("ok"))
	})
	a.srv = &http.Server{
		Addr:              addr,
		Handler:           mux,
		ReadHeaderTimeout: 5 * time.Second,
	}
	ln, err := net.Listen("tcp", addr)
	if err != nil {
		return nil, err
	}
	go func() {
		logger.Info("relay audit http listening", "addr", ln.Addr().String())
		if err := a.srv.Serve(ln); err != nil && err != http.ErrServerClosed {
			logger.Error("relay audit http stopped", "err", err)
		}
	}()
	return a, nil
}

func (a *AuditHTTP) Close() error {
	if a == nil || a.srv == nil {
		return nil
	}
	return a.srv.Close()
}

func (a *AuditHTTP) Addr() string {
	if a == nil || a.srv == nil {
		return ""
	}
	return a.srv.Addr
}

func (a *AuditHTTP) authorized(r *http.Request) bool {
	got := ""
	if h := r.Header.Get("Authorization"); strings.HasPrefix(strings.ToLower(h), "bearer ") {
		got = strings.TrimSpace(h[7:])
	}
	if got == "" {
		got = strings.TrimSpace(r.Header.Get("X-Relay-Audit-Token"))
	}
	if got == "" {
		return false
	}
	want := []byte(a.token)
	have := []byte(got)
	if len(want) != len(have) {
		_ = subtle.ConstantTimeCompare(want, want)
		return false
	}
	return subtle.ConstantTimeCompare(want, have) == 1
}

func (a *AuditHTTP) handleListEvents(w http.ResponseWriter, r *http.Request) {
	if !a.authorized(r) {
		writeAuditJSON(w, http.StatusUnauthorized, map[string]any{"ok": false, "error": "unauthorized"})
		return
	}
	code := r.URL.Query().Get("code")
	page, pageSize := 1, defaultAuditPageSize
	if v := strings.TrimSpace(r.URL.Query().Get("page")); v != "" {
		if n, err := strconv.Atoi(v); err == nil {
			page = n
		}
	}
	if v := strings.TrimSpace(r.URL.Query().Get("page_size")); v != "" {
		if n, err := strconv.Atoi(v); err == nil {
			pageSize = n
		}
	}
	// 兼容旧 limit：当作第一页 page_size
	if v := strings.TrimSpace(r.URL.Query().Get("limit")); v != "" {
		if n, err := strconv.Atoi(v); err == nil && n > 0 {
			pageSize = n
		}
	}
	page, pageSize = normalizeAuditPage(page, pageSize)
	events, total, err := a.store.ListGuestAuditPage(code, page, pageSize)
	if err != nil {
		a.logger.Error("list guest_audit", "err", err)
		writeAuditJSON(w, http.StatusInternalServerError, map[string]any{"ok": false, "error": "list failed"})
		return
	}
	// Defense in depth: never emit forbidden keys even if a future column leaks.
	safe := make([]map[string]any, 0, len(events))
	for _, e := range events {
		row := map[string]any{
			"id":    e.ID,
			"ts":    e.TS,
			"event": e.Event,
			"code":  e.Code,
		}
		if e.OK != nil {
			row["ok"] = *e.OK
		}
		if e.FailReason != "" {
			row["fail_reason"] = e.FailReason
		}
		if e.PeerRole != "" {
			row["peer_role"] = e.PeerRole
		}
		if e.PairedAt != nil {
			row["paired_at"] = *e.PairedAt
		}
		if e.ClosedAt != nil {
			row["closed_at"] = *e.ClosedAt
		}
		if e.DurationSec != nil {
			row["duration_sec"] = *e.DurationSec
		}
		safe = append(safe, row)
	}
	writeAuditJSON(w, http.StatusOK, map[string]any{
		"ok": true, "events": safe,
		"page": page, "page_size": pageSize, "total": total,
	})
}

func writeAuditJSON(w http.ResponseWriter, code int, v any) {
	w.Header().Set("Content-Type", "application/json; charset=utf-8")
	w.WriteHeader(code)
	_ = json.NewEncoder(w).Encode(v)
}

type errString string

func (e errString) Error() string { return string(e) }
