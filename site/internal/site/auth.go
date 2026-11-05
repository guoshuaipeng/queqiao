package site

import (
	"crypto/rand"
	"crypto/subtle"
	"encoding/hex"
	"net/http"
	"strings"
	"sync"
	"time"
)

const (
	sessionCookie   = "rasite_session"
	sessionTTL      = 12 * time.Hour
	defaultDevPass  = "dev"
)

// SessionAuth holds the admin password and in-memory sessions.
type SessionAuth struct {
	password string
	mu       sync.Mutex
	sessions map[string]time.Time
}

func NewSessionAuth(password string) *SessionAuth {
	return &SessionAuth{
		password: password,
		sessions: make(map[string]time.Time),
	}
}

func (a *SessionAuth) PasswordOK(got string) bool {
	want := []byte(a.password)
	have := []byte(got)
	if len(want) != len(have) {
		// still do a compare to keep timing flatter for equal-length probes
		_ = subtle.ConstantTimeCompare(want, want)
		return false
	}
	return subtle.ConstantTimeCompare(want, have) == 1
}

func (a *SessionAuth) CreateSession() (token string, err error) {
	b := make([]byte, 32)
	if _, err = rand.Read(b); err != nil {
		return "", err
	}
	token = hex.EncodeToString(b)
	a.mu.Lock()
	a.sessions[token] = time.Now().Add(sessionTTL)
	a.mu.Unlock()
	return token, nil
}

func (a *SessionAuth) ValidSession(token string) bool {
	token = strings.TrimSpace(token)
	if token == "" {
		return false
	}
	a.mu.Lock()
	defer a.mu.Unlock()
	exp, ok := a.sessions[token]
	if !ok {
		return false
	}
	if time.Now().After(exp) {
		delete(a.sessions, token)
		return false
	}
	// sliding renew lightly
	a.sessions[token] = time.Now().Add(sessionTTL)
	return true
}

func (a *SessionAuth) Revoke(token string) {
	a.mu.Lock()
	delete(a.sessions, token)
	a.mu.Unlock()
}

func (a *SessionAuth) SetSessionCookie(w http.ResponseWriter, token string) {
	http.SetCookie(w, &http.Cookie{
		Name:     sessionCookie,
		Value:    token,
		Path:     "/admin",
		HttpOnly: true,
		SameSite: http.SameSiteLaxMode,
		MaxAge:   int(sessionTTL.Seconds()),
	})
}

func (a *SessionAuth) ClearSessionCookie(w http.ResponseWriter) {
	http.SetCookie(w, &http.Cookie{
		Name:     sessionCookie,
		Value:    "",
		Path:     "/admin",
		HttpOnly: true,
		SameSite: http.SameSiteLaxMode,
		MaxAge:   -1,
	})
}

func (a *SessionAuth) SessionFromRequest(r *http.Request) string {
	c, err := r.Cookie(sessionCookie)
	if err != nil || c == nil {
		return ""
	}
	return c.Value
}

// ResolveAdminPassword picks SITE_ADMIN_PASSWORD.
// Production (non-loopback listen) requires an explicit password.
// Local/dev: when password is empty and addr looks local, default to "dev".
func ResolveAdminPassword(addr, password string) (string, error) {
	password = strings.TrimSpace(password)
	if password != "" {
		return password, nil
	}
	if isLocalAddr(addr) {
		return defaultDevPass, nil
	}
	return "", errAdminPasswordRequired
}

var errAdminPasswordRequired = errString("SITE_ADMIN_PASSWORD is required when SITE_ADDR is not loopback (use e.g. :8088 locally to allow default \"dev\")")

type errString string

func (e errString) Error() string { return string(e) }

func isLocalAddr(addr string) bool {
	addr = strings.TrimSpace(strings.ToLower(addr))
	if addr == "" {
		return true
	}
	if strings.HasPrefix(addr, "127.") || strings.HasPrefix(addr, "[::1]") {
		return true
	}
	if strings.Contains(addr, "localhost") {
		return true
	}
	// bare :8088 / :port — treat as local-dev friendly (bind all interfaces but allow default for convenience)
	// Production deploy uses :80 with explicit SITE_ADMIN_PASSWORD in env file.
	if strings.HasPrefix(addr, ":") && addr != ":80" && addr != ":443" {
		return true
	}
	return false
}
