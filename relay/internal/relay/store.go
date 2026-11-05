package relay

import (
	"database/sql"
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"time"

	"golang.org/x/crypto/bcrypt"
	_ "modernc.org/sqlite"
)

const (
	tokenTTL       = 7 * 24 * time.Hour
	guestTTL       = 2 * time.Hour
	bcryptCost     = bcrypt.DefaultCost
	minPasswordLen = 4
)

// Store is SQLite-backed persistence for users, devices, tokens, guest rows.
type Store struct {
	db *sql.DB
}

func OpenStore(path string) (*Store, error) {
	if path == "" {
		path = filepath.Join("data", "relay.db")
	}
	if err := os.MkdirAll(filepath.Dir(path), 0o755); err != nil {
		return nil, err
	}
	db, err := sql.Open("sqlite", path)
	if err != nil {
		return nil, err
	}
	db.SetMaxOpenConns(1)
	db.SetMaxIdleConns(1)
	s := &Store{db: db}
	if err := s.migrate(); err != nil {
		_ = db.Close()
		return nil, err
	}
	return s, nil
}

func (s *Store) Close() error {
	if s == nil || s.db == nil {
		return nil
	}
	return s.db.Close()
}

func (s *Store) migrate() error {
	schema := `
CREATE TABLE IF NOT EXISTS users (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  username TEXT NOT NULL UNIQUE COLLATE NOCASE,
  password_hash TEXT NOT NULL,
  created_at INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS devices (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  user_id INTEGER NOT NULL,
  device_id TEXT NOT NULL,
  display_name TEXT NOT NULL,
  last_seen INTEGER NOT NULL,
  UNIQUE(user_id, device_id),
  FOREIGN KEY(user_id) REFERENCES users(id)
);
CREATE TABLE IF NOT EXISTS tokens (
  token TEXT PRIMARY KEY,
  user_id INTEGER NOT NULL,
  expires_at INTEGER NOT NULL,
  FOREIGN KEY(user_id) REFERENCES users(id)
);
CREATE TABLE IF NOT EXISTS guest_sessions (
  code TEXT PRIMARY KEY,
  password_hash TEXT NOT NULL,
  created_at INTEGER NOT NULL,
  expires_at INTEGER NOT NULL,
  agent_present INTEGER NOT NULL DEFAULT 0
);
CREATE INDEX IF NOT EXISTS idx_tokens_user ON tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_devices_user ON devices(user_id);
`
	_, err := s.db.Exec(schema)
	return err
}

func hashPassword(pw string) (string, error) {
	b, err := bcrypt.GenerateFromPassword([]byte(pw), bcryptCost)
	if err != nil {
		return "", err
	}
	return string(b), nil
}

func checkPassword(hash, pw string) bool {
	return bcrypt.CompareHashAndPassword([]byte(hash), []byte(pw)) == nil
}

func (s *Store) Signup(username, password string) (userID int64, err error) {
	username = strings.TrimSpace(username)
	if username == "" || len(username) > 64 {
		return 0, fmt.Errorf("invalid username")
	}
	if len(password) < minPasswordLen {
		return 0, fmt.Errorf("password too short (min %d)", minPasswordLen)
	}
	h, err := hashPassword(password)
	if err != nil {
		return 0, err
	}
	now := time.Now().Unix()
	res, err := s.db.Exec(`INSERT INTO users(username, password_hash, created_at) VALUES(?,?,?)`, username, h, now)
	if err != nil {
		if strings.Contains(strings.ToLower(err.Error()), "unique") {
			return 0, fmt.Errorf("username taken")
		}
		return 0, err
	}
	return res.LastInsertId()
}

func (s *Store) Login(username, password string) (userID int64, token string, err error) {
	username = strings.TrimSpace(username)
	var hash string
	err = s.db.QueryRow(`SELECT id, password_hash FROM users WHERE username = ? COLLATE NOCASE`, username).Scan(&userID, &hash)
	if err == sql.ErrNoRows {
		return 0, "", fmt.Errorf("invalid credentials")
	}
	if err != nil {
		return 0, "", err
	}
	if !checkPassword(hash, password) {
		return 0, "", fmt.Errorf("invalid credentials")
	}
	token, err = s.issueToken(userID)
	return userID, token, err
}

func (s *Store) issueToken(userID int64) (string, error) {
	tok, err := randomToken(32)
	if err != nil {
		return "", err
	}
	exp := time.Now().Add(tokenTTL).Unix()
	_, err = s.db.Exec(`INSERT INTO tokens(token, user_id, expires_at) VALUES(?,?,?)`, tok, userID, exp)
	if err != nil {
		return "", err
	}
	return tok, nil
}

func (s *Store) AuthToken(token string) (userID int64, err error) {
	token = strings.TrimSpace(token)
	if token == "" {
		return 0, fmt.Errorf("missing token")
	}
	var exp int64
	err = s.db.QueryRow(`SELECT user_id, expires_at FROM tokens WHERE token = ?`, token).Scan(&userID, &exp)
	if err == sql.ErrNoRows {
		return 0, fmt.Errorf("invalid token")
	}
	if err != nil {
		return 0, err
	}
	if time.Now().Unix() > exp {
		_, _ = s.db.Exec(`DELETE FROM tokens WHERE token = ?`, token)
		return 0, fmt.Errorf("token expired")
	}
	return userID, nil
}

// DeviceRow is returned by list_devices (online filled by server memory).
type DeviceRow struct {
	DeviceID    string `json:"device_id"`
	DisplayName string `json:"display_name"`
	Online      bool   `json:"online"`
	LastSeen    int64  `json:"last_seen"`
}

func (s *Store) UpsertDevice(userID int64, deviceID, displayName string) error {
	deviceID = strings.TrimSpace(deviceID)
	displayName = strings.TrimSpace(displayName)
	if deviceID == "" || len(deviceID) > 128 {
		return fmt.Errorf("invalid device_id")
	}
	if displayName == "" {
		displayName = deviceID
	}
	if len(displayName) > 128 {
		displayName = displayName[:128]
	}
	now := time.Now().Unix()
	_, err := s.db.Exec(`
INSERT INTO devices(user_id, device_id, display_name, last_seen) VALUES(?,?,?,?)
ON CONFLICT(user_id, device_id) DO UPDATE SET
  display_name=excluded.display_name,
  last_seen=excluded.last_seen
`, userID, deviceID, displayName, now)
	return err
}

func (s *Store) TouchDevice(userID int64, deviceID string) error {
	now := time.Now().Unix()
	_, err := s.db.Exec(`UPDATE devices SET last_seen=? WHERE user_id=? AND device_id=?`, now, userID, deviceID)
	return err
}

func (s *Store) ListDevices(userID int64) ([]DeviceRow, error) {
	rows, err := s.db.Query(`SELECT device_id, display_name, last_seen FROM devices WHERE user_id=? ORDER BY display_name COLLATE NOCASE`, userID)
	if err != nil {
		return nil, err
	}
	defer rows.Close()
	out := make([]DeviceRow, 0)
	for rows.Next() {
		var d DeviceRow
		if err := rows.Scan(&d.DeviceID, &d.DisplayName, &d.LastSeen); err != nil {
			return nil, err
		}
		out = append(out, d)
	}
	return out, rows.Err()
}

func (s *Store) OwnDevice(userID int64, deviceID string) (bool, error) {
	var n int
	err := s.db.QueryRow(`SELECT COUNT(1) FROM devices WHERE user_id=? AND device_id=?`, userID, deviceID).Scan(&n)
	return n > 0, err
}

func (s *Store) SaveGuest(code, password string, expiresAt time.Time) error {
	h, err := hashPassword(password)
	if err != nil {
		return err
	}
	now := time.Now().Unix()
	_, err = s.db.Exec(`
INSERT INTO guest_sessions(code, password_hash, created_at, expires_at, agent_present)
VALUES(?,?,?,?,1)
ON CONFLICT(code) DO UPDATE SET
  password_hash=excluded.password_hash,
  created_at=excluded.created_at,
  expires_at=excluded.expires_at,
  agent_present=1
`, code, h, now, expiresAt.Unix())
	return err
}

func (s *Store) GuestPasswordOK(code, password string) (bool, error) {
	var hash string
	var exp int64
	err := s.db.QueryRow(`SELECT password_hash, expires_at FROM guest_sessions WHERE code=?`, code).Scan(&hash, &exp)
	if err == sql.ErrNoRows {
		return false, nil
	}
	if err != nil {
		return false, err
	}
	if time.Now().Unix() > exp {
		return false, fmt.Errorf("guest session expired")
	}
	return checkPassword(hash, password), nil
}

func (s *Store) SetGuestAgentPresent(code string, present bool) error {
	v := 0
	if present {
		v = 1
	}
	_, err := s.db.Exec(`UPDATE guest_sessions SET agent_present=? WHERE code=?`, v, code)
	return err
}

func (s *Store) DeleteGuest(code string) error {
	_, err := s.db.Exec(`DELETE FROM guest_sessions WHERE code=?`, code)
	return err
}
