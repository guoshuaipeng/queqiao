package site

import (
	"database/sql"
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"time"
	"unicode"

	_ "modernc.org/sqlite"
)

const (
	StatusNew     = "新留言"
	StatusContact = "已联系"
	StatusDone    = "已结束"
)

var allowedStatuses = map[string]bool{
	StatusNew:     true,
	StatusContact: true,
	StatusDone:    true,
}

var allowedOS = map[string]string{
	"Windows": "Windows",
	"macOS":   "macOS",
	"Linux":   "Linux",
	"不确定":     "不确定",
	"不清楚":     "不确定",
}

// Store is SQLite-backed persistence for customers and messages.
type Store struct {
	db *sql.DB
}

type Message struct {
	ID         int64  `json:"id"`
	CustomerID int64  `json:"customer_id"`
	Name       string `json:"name"`
	Wechat     string `json:"wechat"`
	Phone      string `json:"phone"`
	Email      string `json:"email"`
	OS         string `json:"os"`
	Problem    string `json:"problem"`
	Status     string `json:"status"`
	Note       string `json:"note"`
	CreatedAt  int64  `json:"created_at"`
	UpdatedAt  int64  `json:"updated_at"`
}

type Customer struct {
	ID           int64  `json:"id"`
	Name         string `json:"name"`
	Wechat       string `json:"wechat"`
	Phone        string `json:"phone"`
	Email        string `json:"email"`
	OS           string `json:"os"`
	LastMsgAt    int64  `json:"last_message_at"`
	Status       string `json:"status"`
	HistoryCount int    `json:"history_count"`
	CreatedAt    int64  `json:"created_at"`
	UpdatedAt    int64  `json:"updated_at"`
}

type AskInput struct {
	Name    string
	Wechat  string
	Phone   string
	Email   string
	OS      string
	Problem string
}

func OpenStore(path string) (*Store, error) {
	if path == "" {
		path = filepath.Join("data", "site.db")
	}
	dir := filepath.Dir(path)
	if dir != "." && dir != "" {
		if err := os.MkdirAll(dir, 0o755); err != nil {
			return nil, err
		}
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
CREATE TABLE IF NOT EXISTS customers (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL DEFAULT '',
  wechat TEXT NOT NULL DEFAULT '',
  phone TEXT NOT NULL DEFAULT '',
  email TEXT NOT NULL DEFAULT '',
  os TEXT NOT NULL DEFAULT '',
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS messages (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  customer_id INTEGER NOT NULL,
  name TEXT NOT NULL,
  wechat TEXT NOT NULL DEFAULT '',
  phone TEXT NOT NULL DEFAULT '',
  email TEXT NOT NULL DEFAULT '',
  os TEXT NOT NULL,
  problem TEXT NOT NULL,
  status TEXT NOT NULL,
  note TEXT NOT NULL DEFAULT '',
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL,
  FOREIGN KEY(customer_id) REFERENCES customers(id)
);
CREATE INDEX IF NOT EXISTS idx_messages_created ON messages(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_messages_customer ON messages(customer_id);
CREATE INDEX IF NOT EXISTS idx_customers_wechat ON customers(wechat);
CREATE INDEX IF NOT EXISTS idx_customers_phone ON customers(phone);
CREATE INDEX IF NOT EXISTS idx_customers_email ON customers(email);
`
	_, err := s.db.Exec(schema)
	return err
}

func NormalizeAsk(in AskInput) (AskInput, error) {
	out := AskInput{
		Name:    strings.TrimSpace(in.Name),
		Wechat:  strings.TrimSpace(in.Wechat),
		Phone:   normalizePhone(strings.TrimSpace(in.Phone)),
		Email:   strings.ToLower(strings.TrimSpace(in.Email)),
		OS:      strings.TrimSpace(in.OS),
		Problem: strings.TrimSpace(in.Problem),
	}
	if out.Name == "" || out.Problem == "" {
		return out, fmt.Errorf("请填完后再发送。")
	}
	if len([]rune(out.Name)) > 40 {
		return out, fmt.Errorf("请填完后再发送。")
	}
	if len([]rune(out.Problem)) > 2000 {
		return out, fmt.Errorf("请填完后再发送。")
	}
	if out.Wechat == "" && out.Phone == "" && out.Email == "" {
		return out, fmt.Errorf("请留下能联系到你的微信、电话或邮箱。")
	}
	if out.Wechat != "" && len([]rune(out.Wechat)) < 2 {
		return out, fmt.Errorf("请留下能联系到你的微信、电话或邮箱。")
	}
	if out.Phone != "" && len(out.Phone) < 5 {
		return out, fmt.Errorf("请留下能联系到你的微信、电话或邮箱。")
	}
	if out.Email != "" && !strings.Contains(out.Email, "@") {
		return out, fmt.Errorf("请留下能联系到你的微信、电话或邮箱。")
	}
	osNorm, ok := allowedOS[out.OS]
	if !ok {
		return out, fmt.Errorf("请填完后再发送。")
	}
	out.OS = osNorm
	return out, nil
}

func normalizePhone(p string) string {
	var b strings.Builder
	for _, r := range p {
		if unicode.IsDigit(r) || r == '+' {
			b.WriteRune(r)
		}
	}
	return b.String()
}

func ValidStatus(st string) bool {
	return allowedStatuses[st]
}

// CreateMessage inserts a message, merging into an existing customer when any
// non-empty contact field matches.
func (s *Store) CreateMessage(in AskInput) (msgID, customerID int64, err error) {
	in, err = NormalizeAsk(in)
	if err != nil {
		return 0, 0, err
	}
	tx, err := s.db.Begin()
	if err != nil {
		return 0, 0, err
	}
	defer func() { _ = tx.Rollback() }()

	customerID, err = findCustomerTx(tx, in.Wechat, in.Phone, in.Email)
	if err != nil {
		return 0, 0, err
	}
	now := time.Now().Unix()
	if customerID == 0 {
		res, err := tx.Exec(`
INSERT INTO customers(name, wechat, phone, email, os, created_at, updated_at)
VALUES(?,?,?,?,?,?,?)`, in.Name, in.Wechat, in.Phone, in.Email, in.OS, now, now)
		if err != nil {
			return 0, 0, err
		}
		customerID, err = res.LastInsertId()
		if err != nil {
			return 0, 0, err
		}
	} else {
		if err := mergeCustomerContactsTx(tx, customerID, in, now); err != nil {
			return 0, 0, err
		}
	}

	res, err := tx.Exec(`
INSERT INTO messages(customer_id, name, wechat, phone, email, os, problem, status, note, created_at, updated_at)
VALUES(?,?,?,?,?,?,?,?,?,?,?)`,
		customerID, in.Name, in.Wechat, in.Phone, in.Email, in.OS, in.Problem, StatusNew, "", now, now)
	if err != nil {
		return 0, 0, err
	}
	msgID, err = res.LastInsertId()
	if err != nil {
		return 0, 0, err
	}
	if err := tx.Commit(); err != nil {
		return 0, 0, err
	}
	return msgID, customerID, nil
}

func findCustomerTx(tx *sql.Tx, wechat, phone, email string) (int64, error) {
	conds := make([]string, 0, 3)
	args := make([]any, 0, 3)
	if wechat != "" {
		conds = append(conds, "wechat = ?")
		args = append(args, wechat)
	}
	if phone != "" {
		conds = append(conds, "phone = ?")
		args = append(args, phone)
	}
	if email != "" {
		conds = append(conds, "email = ? COLLATE NOCASE")
		args = append(args, email)
	}
	if len(conds) == 0 {
		return 0, nil
	}
	q := `SELECT id FROM customers WHERE (` + strings.Join(conds, " OR ") + `) ORDER BY id ASC LIMIT 1`
	var id int64
	err := tx.QueryRow(q, args...).Scan(&id)
	if err == sql.ErrNoRows {
		return 0, nil
	}
	return id, err
}

func mergeCustomerContactsTx(tx *sql.Tx, id int64, in AskInput, now int64) error {
	var name, wechat, phone, email, osVal string
	err := tx.QueryRow(`SELECT name, wechat, phone, email, os FROM customers WHERE id=?`, id).
		Scan(&name, &wechat, &phone, &email, &osVal)
	if err != nil {
		return err
	}
	if in.Name != "" {
		name = in.Name
	}
	if in.Wechat != "" {
		wechat = in.Wechat
	}
	if in.Phone != "" {
		phone = in.Phone
	}
	if in.Email != "" {
		email = in.Email
	}
	if in.OS != "" {
		osVal = in.OS
	}
	_, err = tx.Exec(`UPDATE customers SET name=?, wechat=?, phone=?, email=?, os=?, updated_at=? WHERE id=?`,
		name, wechat, phone, email, osVal, now, id)
	return err
}

func (s *Store) ListMessages() ([]Message, error) {
	rows, err := s.db.Query(`
SELECT id, customer_id, name, wechat, phone, email, os, problem, status, note, created_at, updated_at
FROM messages ORDER BY created_at DESC, id DESC`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()
	out := make([]Message, 0)
	for rows.Next() {
		var m Message
		if err := rows.Scan(&m.ID, &m.CustomerID, &m.Name, &m.Wechat, &m.Phone, &m.Email, &m.OS, &m.Problem, &m.Status, &m.Note, &m.CreatedAt, &m.UpdatedAt); err != nil {
			return nil, err
		}
		out = append(out, m)
	}
	return out, rows.Err()
}

func (s *Store) SetStatus(id int64, status string) error {
	if !ValidStatus(status) {
		return fmt.Errorf("invalid status")
	}
	now := time.Now().Unix()
	res, err := s.db.Exec(`UPDATE messages SET status=?, updated_at=? WHERE id=?`, status, now, id)
	if err != nil {
		return err
	}
	n, err := res.RowsAffected()
	if err != nil {
		return err
	}
	if n == 0 {
		return fmt.Errorf("not found")
	}
	return nil
}

func (s *Store) SetNote(id int64, note string) error {
	note = strings.TrimSpace(note)
	if len([]rune(note)) > 4000 {
		return fmt.Errorf("note too long")
	}
	now := time.Now().Unix()
	res, err := s.db.Exec(`UPDATE messages SET note=?, updated_at=? WHERE id=?`, note, now, id)
	if err != nil {
		return err
	}
	n, err := res.RowsAffected()
	if err != nil {
		return err
	}
	if n == 0 {
		return fmt.Errorf("not found")
	}
	return nil
}

func (s *Store) ListCustomers() ([]Customer, error) {
	rows, err := s.db.Query(`
SELECT c.id, c.name, c.wechat, c.phone, c.email, c.os, c.created_at, c.updated_at,
  (SELECT COUNT(1) FROM messages m WHERE m.customer_id = c.id) AS history_count,
  (SELECT m.created_at FROM messages m WHERE m.customer_id = c.id ORDER BY m.created_at DESC, m.id DESC LIMIT 1) AS last_msg_at,
  (SELECT m.status FROM messages m WHERE m.customer_id = c.id ORDER BY m.created_at DESC, m.id DESC LIMIT 1) AS status
FROM customers c
ORDER BY last_msg_at DESC, c.id DESC`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()
	out := make([]Customer, 0)
	for rows.Next() {
		var c Customer
		var last sql.NullInt64
		var st sql.NullString
		if err := rows.Scan(&c.ID, &c.Name, &c.Wechat, &c.Phone, &c.Email, &c.OS, &c.CreatedAt, &c.UpdatedAt, &c.HistoryCount, &last, &st); err != nil {
			return nil, err
		}
		if last.Valid {
			c.LastMsgAt = last.Int64
		}
		if st.Valid {
			c.Status = st.String
		}
		out = append(out, c)
	}
	return out, rows.Err()
}
