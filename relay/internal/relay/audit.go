package relay

import (
	"database/sql"
	"fmt"
	"log/slog"
	"strings"
	"time"
)

// Guest audit events (R046).
const (
	AuditGenerated   = "generated"
	AuditPaired      = "paired"
	AuditJoinFailed  = "join_failed"
	AuditWaitTimeout = "wait_timeout"
	AuditEnded       = "ended"
	AuditAbandoned   = "abandoned"
)

// join_failed fail_reason short enums (never store password).
const (
	FailBadCredentials  = "bad_credentials"
	FailExpired         = "expired"
	FailPeerOffline     = "peer_offline"
	FailBusy            = "busy"
	FailTooManySessions = "too_many_sessions"
	FailBadRequest      = "bad_request"
	FailOther           = "other"
)

const defaultAuditRetentionDays = 90

// GuestAuditEvent is a safe list row (no password / hash / fp / payload / RemoteAddr).
type GuestAuditEvent struct {
	ID          int64  `json:"id"`
	TS          int64  `json:"ts"`
	Event       string `json:"event"`
	Code        string `json:"code"`
	OK          *int   `json:"ok,omitempty"`
	FailReason  string `json:"fail_reason,omitempty"`
	PeerRole    string `json:"peer_role,omitempty"`
	PairedAt    *int64 `json:"paired_at,omitempty"`
	ClosedAt    *int64 `json:"closed_at,omitempty"`
	DurationSec *int64 `json:"duration_sec,omitempty"`
}

// AuditInsert holds fields for an append-only guest_audit row.
type AuditInsert struct {
	Event       string
	Code        string
	OK          *int
	FailReason  string
	PeerRole    string
	PairedAt    *int64
	ClosedAt    *int64
	DurationSec *int64
}

func (s *Store) ensureGuestAuditSchema() error {
	schema := `
CREATE TABLE IF NOT EXISTS guest_audit (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  ts INTEGER NOT NULL,
  event TEXT NOT NULL,
  code TEXT NOT NULL,
  ok INTEGER,
  fail_reason TEXT,
  peer_role TEXT,
  paired_at INTEGER,
  closed_at INTEGER,
  duration_sec INTEGER
);
CREATE INDEX IF NOT EXISTS idx_guest_audit_ts ON guest_audit(ts DESC);
CREATE INDEX IF NOT EXISTS idx_guest_audit_code_ts ON guest_audit(code, ts DESC);
`
	_, err := s.db.Exec(schema)
	return err
}

// AppendGuestAudit inserts one audit event. Caller should treat errors as non-fatal for pairing.
func (s *Store) AppendGuestAudit(in AuditInsert) error {
	in.Code = normalizeCode(in.Code)
	in.Event = strings.TrimSpace(in.Event)
	if in.Event == "" {
		return fmt.Errorf("missing audit event")
	}
	in.FailReason = strings.TrimSpace(in.FailReason)
	in.PeerRole = strings.TrimSpace(in.PeerRole)
	ts := time.Now().Unix()
	_, err := s.db.Exec(`
INSERT INTO guest_audit(ts, event, code, ok, fail_reason, peer_role, paired_at, closed_at, duration_sec)
VALUES(?,?,?,?,?,?,?,?,?)
`, ts, in.Event, in.Code, in.OK, nullIfEmpty(in.FailReason), nullIfEmpty(in.PeerRole), in.PairedAt, in.ClosedAt, in.DurationSec)
	return err
}

func nullIfEmpty(s string) any {
	if s == "" {
		return nil
	}
	return s
}

const (
	defaultAuditPageSize = 20
	maxAuditPageSize     = 100
	maxAuditPageNumber   = 10000
)

func normalizeAuditPage(page, pageSize int) (int, int) {
	if pageSize <= 0 {
		pageSize = defaultAuditPageSize
	}
	if pageSize > maxAuditPageSize {
		pageSize = maxAuditPageSize
	}
	if page <= 0 {
		page = 1
	}
	if page > maxAuditPageNumber {
		page = maxAuditPageNumber
	}
	return page, pageSize
}

// ListGuestAudit returns newest-first events; empty code means no filter.
func (s *Store) ListGuestAudit(code string, limit int) ([]GuestAuditEvent, error) {
	if limit <= 0 || limit > maxAuditPageSize {
		limit = defaultAuditPageSize
	}
	list, _, err := s.ListGuestAuditPage(code, 1, limit)
	return list, err
}

// ListGuestAuditPage returns newest-first events with total count.
func (s *Store) ListGuestAuditPage(code string, page, pageSize int) ([]GuestAuditEvent, int64, error) {
	page, pageSize = normalizeAuditPage(page, pageSize)
	code = normalizeCode(code)
	var total int64
	var err error
	if code == "" {
		err = s.db.QueryRow(`SELECT COUNT(1) FROM guest_audit`).Scan(&total)
	} else {
		err = s.db.QueryRow(`SELECT COUNT(1) FROM guest_audit WHERE code = ?`, code).Scan(&total)
	}
	if err != nil {
		return nil, 0, err
	}
	offset := (page - 1) * pageSize
	var rows *sql.Rows
	if code == "" {
		rows, err = s.db.Query(`
SELECT id, ts, event, code, ok, fail_reason, peer_role, paired_at, closed_at, duration_sec
FROM guest_audit ORDER BY ts DESC, id DESC LIMIT ? OFFSET ?
`, pageSize, offset)
	} else {
		rows, err = s.db.Query(`
SELECT id, ts, event, code, ok, fail_reason, peer_role, paired_at, closed_at, duration_sec
FROM guest_audit WHERE code = ? ORDER BY ts DESC, id DESC LIMIT ? OFFSET ?
`, code, pageSize, offset)
	}
	if err != nil {
		return nil, 0, err
	}
	defer rows.Close()
	out := make([]GuestAuditEvent, 0, pageSize)
	for rows.Next() {
		var e GuestAuditEvent
		var ok sql.NullInt64
		var fail, peer sql.NullString
		var paired, closed, dur sql.NullInt64
		if err := rows.Scan(&e.ID, &e.TS, &e.Event, &e.Code, &ok, &fail, &peer, &paired, &closed, &dur); err != nil {
			return nil, 0, err
		}
		if ok.Valid {
			v := int(ok.Int64)
			e.OK = &v
		}
		if fail.Valid {
			e.FailReason = fail.String
		}
		if peer.Valid {
			e.PeerRole = peer.String
		}
		if paired.Valid {
			e.PairedAt = &paired.Int64
		}
		if closed.Valid {
			e.ClosedAt = &closed.Int64
		}
		if dur.Valid {
			e.DurationSec = &dur.Int64
		}
		out = append(out, e)
	}
	return out, total, rows.Err()
}

// GuestCodeSummary collapses every audit event of one session code into a single row.
type GuestCodeSummary struct {
	Code           string `json:"code"`
	Status         string `json:"status"`
	FirstTS        int64  `json:"first_ts"`
	LastTS         int64  `json:"last_ts"`
	EndedAt        *int64 `json:"ended_at,omitempty"`
	Pairs          int64  `json:"pairs"`
	DurationSec    int64  `json:"duration_sec"`
	Fails          int64  `json:"fails"`
	LastFailReason string `json:"last_fail_reason,omitempty"`
	Events         int64  `json:"events"`
}

// Code statuses come from the last lifecycle event; join_failed only counts as a failure.
const (
	CodeWaiting   = "waiting"
	CodeAssisting = "assisting"
	CodeEnded     = "ended"
	CodeTimeout   = "timeout"
	CodeAbandoned = "abandoned"
)

func codeStatus(lastEvent string) string {
	switch lastEvent {
	case AuditPaired:
		return CodeAssisting
	case AuditEnded:
		return CodeEnded
	case AuditWaitTimeout:
		return CodeTimeout
	case AuditAbandoned:
		return CodeAbandoned
	default:
		return CodeWaiting
	}
}

// ListGuestCodesPage returns one row per code, newest activity first.
func (s *Store) ListGuestCodesPage(code string, page, pageSize int) ([]GuestCodeSummary, int64, error) {
	page, pageSize = normalizeAuditPage(page, pageSize)
	code = normalizeCode(code)
	where, args := "", []any{}
	if code != "" {
		where, args = " WHERE code = ?", []any{code}
	}
	var total int64
	if err := s.db.QueryRow(`SELECT COUNT(DISTINCT code) FROM guest_audit`+where, args...).Scan(&total); err != nil {
		return nil, 0, err
	}
	rows, err := s.db.Query(`
SELECT code, MIN(ts), MAX(ts),
       SUM(event = 'paired'), SUM(COALESCE(duration_sec, 0)), SUM(event = 'join_failed'), COUNT(1),
       MAX(CASE WHEN event IN ('ended','abandoned','wait_timeout') THEN ts END)
FROM guest_audit`+where+`
GROUP BY code ORDER BY MAX(ts) DESC, code DESC LIMIT ? OFFSET ?
`, append(args, pageSize, (page-1)*pageSize)...)
	if err != nil {
		return nil, 0, err
	}
	defer rows.Close()
	out := make([]GuestCodeSummary, 0, pageSize)
	for rows.Next() {
		var c GuestCodeSummary
		var ended sql.NullInt64
		if err := rows.Scan(&c.Code, &c.FirstTS, &c.LastTS, &c.Pairs, &c.DurationSec, &c.Fails, &c.Events, &ended); err != nil {
			return nil, 0, err
		}
		if ended.Valid {
			c.EndedAt = &ended.Int64
		}
		c.Status = CodeWaiting
		out = append(out, c)
	}
	if err := rows.Err(); err != nil {
		return nil, 0, err
	}
	if len(out) == 0 {
		return out, total, nil
	}
	byCode := make(map[string]*GuestCodeSummary, len(out))
	placeholders := make([]any, 0, len(out))
	marks := ""
	for i := range out {
		byCode[out[i].Code] = &out[i]
		placeholders = append(placeholders, out[i].Code)
		if i > 0 {
			marks += ","
		}
		marks += "?"
	}
	// 状态取每码最后一条生命周期事件；失败原因取最后一条 join_failed。
	last, err := s.db.Query(`
SELECT code, event FROM guest_audit
WHERE id IN (SELECT MAX(id) FROM guest_audit WHERE event <> 'join_failed' AND code IN (`+marks+`) GROUP BY code)
`, placeholders...)
	if err != nil {
		return nil, 0, err
	}
	for last.Next() {
		var code, event string
		if err := last.Scan(&code, &event); err != nil {
			last.Close()
			return nil, 0, err
		}
		if c, ok := byCode[code]; ok {
			c.Status = codeStatus(event)
		}
	}
	if err := last.Err(); err != nil {
		last.Close()
		return nil, 0, err
	}
	last.Close()
	fails, err := s.db.Query(`
SELECT code, fail_reason FROM guest_audit
WHERE id IN (SELECT MAX(id) FROM guest_audit WHERE event = 'join_failed' AND code IN (`+marks+`) GROUP BY code)
`, placeholders...)
	if err != nil {
		return nil, 0, err
	}
	defer fails.Close()
	for fails.Next() {
		var code string
		var reason sql.NullString
		if err := fails.Scan(&code, &reason); err != nil {
			return nil, 0, err
		}
		if c, ok := byCode[code]; ok && reason.Valid {
			c.LastFailReason = reason.String
		}
	}
	return out, total, fails.Err()
}

// PurgeGuestAuditOlderThan deletes audit rows older than retentionDays. retentionDays<=0 uses 90.
func (s *Store) PurgeGuestAuditOlderThan(retentionDays int) (int64, error) {
	if retentionDays <= 0 {
		retentionDays = defaultAuditRetentionDays
	}
	cutoff := time.Now().Add(-time.Duration(retentionDays) * 24 * time.Hour).Unix()
	res, err := s.db.Exec(`DELETE FROM guest_audit WHERE ts < ?`, cutoff)
	if err != nil {
		return 0, err
	}
	return res.RowsAffected()
}

// safeAppendAudit never blocks the guest pairing path.
func (s *Server) safeAppendAudit(in AuditInsert) {
	if s == nil || s.store == nil {
		return
	}
	if err := s.store.AppendGuestAudit(in); err != nil {
		log := s.cfg.Logger
		if log == nil {
			log = slog.Default()
		}
		log.Error("guest_audit insert failed", "event", in.Event, "code", in.Code, "err", err)
	}
}

func classifyJoinFail(err error) string {
	if err == nil {
		return FailOther
	}
	msg := strings.ToLower(err.Error())
	switch {
	case strings.Contains(msg, "expired"):
		return FailExpired
	case strings.Contains(msg, "invalid code or password"):
		return FailBadCredentials
	case strings.Contains(msg, "peer not online"):
		return FailPeerOffline
	case strings.Contains(msg, "helper already"):
		return FailBusy
	case strings.Contains(msg, "too many attempts"):
		return FailBadCredentials
	case strings.Contains(msg, "too many"):
		return FailTooManySessions
	case strings.Contains(msg, "missing"), strings.Contains(msg, "too short"), strings.Contains(msg, "invalid"):
		return FailBadRequest
	default:
		return FailOther
	}
}

func intPtr(v int) *int       { return &v }
func int64Ptr(v int64) *int64 { return &v }
