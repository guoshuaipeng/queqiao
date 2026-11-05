package site

import (
	"encoding/json"
	"io"
	"io/fs"
	"log/slog"
	"net/http"
	"strings"
	"time"
)

type Config struct {
	Addr     string
	DataPath string
	Password string
	Logger   *slog.Logger
	Static   fs.FS
}

type Server struct {
	cfg  Config
	store *Store
	auth *SessionAuth
	mux  *http.ServeMux
	httpServer *http.Server
	log  *slog.Logger
}

func NewServer(cfg Config) (*Server, error) {
	if cfg.Logger == nil {
		cfg.Logger = slog.Default()
	}
	pass, err := ResolveAdminPassword(cfg.Addr, cfg.Password)
	if err != nil {
		return nil, err
	}
	// never log password
	store, err := OpenStore(cfg.DataPath)
	if err != nil {
		return nil, err
	}
	s := &Server{
		cfg:   cfg,
		store: store,
		auth:  NewSessionAuth(pass),
		mux:   http.NewServeMux(),
		log:   cfg.Logger,
	}
	s.routes()
	s.httpServer = &http.Server{
		Addr:              cfg.Addr,
		Handler:           s.mux,
		ReadHeaderTimeout: 10 * time.Second,
	}
	return s, nil
}

func (s *Server) Store() *Store { return s.store }
func (s *Server) Auth() *SessionAuth { return s.auth }
func (s *Server) Handler() http.Handler { return s.mux }

func (s *Server) Close() error {
	if s.httpServer != nil {
		_ = s.httpServer.Close()
	}
	return s.store.Close()
}

func (s *Server) ListenAndServe() error {
	return s.httpServer.ListenAndServe()
}

func (s *Server) routes() {
	s.mux.HandleFunc("GET /{$}", s.serveFile("index.html"))
	s.mux.HandleFunc("GET /index.html", s.serveFile("index.html"))
	s.mux.HandleFunc("GET /ask", s.serveFile("ask.html"))
	s.mux.HandleFunc("GET /ask.html", s.serveFile("ask.html"))
	s.mux.HandleFunc("GET /thanks", s.serveFile("thanks.html"))
	s.mux.HandleFunc("GET /thanks.html", s.serveFile("thanks.html"))
	s.mux.HandleFunc("GET /styles.css", s.serveFile("styles.css"))

	s.mux.HandleFunc("POST /api/ask", s.handleAsk)

	s.mux.HandleFunc("GET /admin", s.handleAdminPage)
	s.mux.HandleFunc("GET /admin/", s.handleAdminPage)
	s.mux.HandleFunc("POST /admin/login", s.handleAdminLogin)
	s.mux.HandleFunc("POST /admin/logout", s.handleAdminLogout)
	s.mux.HandleFunc("GET /admin/api/messages", s.requireAdmin(s.handleListMessages))
	s.mux.HandleFunc("GET /admin/api/customers", s.requireAdmin(s.handleListCustomers))
	s.mux.HandleFunc("POST /admin/api/status", s.requireAdmin(s.handleSetStatus))
	s.mux.HandleFunc("POST /admin/api/note", s.requireAdmin(s.handleSetNote))
}

func (s *Server) serveFile(name string) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		if s.cfg.Static == nil {
			http.Error(w, "static not configured", http.StatusInternalServerError)
			return
		}
		b, err := fs.ReadFile(s.cfg.Static, name)
		if err != nil {
			http.NotFound(w, r)
			return
		}
		ctype := "text/html; charset=utf-8"
		switch {
		case strings.HasSuffix(name, ".css"):
			ctype = "text/css; charset=utf-8"
		case strings.HasSuffix(name, ".js"):
			ctype = "application/javascript; charset=utf-8"
		}
		w.Header().Set("Content-Type", ctype)
		w.Header().Set("X-Content-Type-Options", "nosniff")
		_, _ = w.Write(b)
	}
}

func (s *Server) handleAsk(w http.ResponseWriter, r *http.Request) {
	in, err := parseAsk(r)
	if err != nil {
		s.askError(w, r, err.Error(), http.StatusBadRequest)
		return
	}
	_, _, err = s.store.CreateMessage(in)
	if err != nil {
		// validation errors from NormalizeAsk
		code := http.StatusBadRequest
		if !strings.Contains(err.Error(), "请") {
			s.log.Error("create message", "err", err)
			code = http.StatusInternalServerError
			s.askError(w, r, "提交失败，请稍后再试。", code)
			return
		}
		s.askError(w, r, err.Error(), code)
		return
	}
	// never return order/ticket id to visitor
	if wantsJSON(r) {
		writeJSON(w, http.StatusOK, map[string]any{"ok": true})
		return
	}
	http.Redirect(w, r, "/thanks", http.StatusSeeOther)
}

func parseAsk(r *http.Request) (AskInput, error) {
	ctype := r.Header.Get("Content-Type")
	if strings.Contains(ctype, "application/json") {
		defer r.Body.Close()
		body, err := io.ReadAll(io.LimitReader(r.Body, 1<<20))
		if err != nil {
			return AskInput{}, err
		}
		var raw struct {
			Name    string `json:"name"`
			Wechat  string `json:"wechat"`
			Phone   string `json:"phone"`
			Email   string `json:"email"`
			OS      string `json:"os"`
			Problem string `json:"problem"`
		}
		if err := json.Unmarshal(body, &raw); err != nil {
			return AskInput{}, errString("请填完后再发送。")
		}
		return AskInput{
			Name: raw.Name, Wechat: raw.Wechat, Phone: raw.Phone,
			Email: raw.Email, OS: raw.OS, Problem: raw.Problem,
		}, nil
	}
	if err := r.ParseForm(); err != nil {
		return AskInput{}, errString("请填完后再发送。")
	}
	return AskInput{
		Name:    r.FormValue("name"),
		Wechat:  r.FormValue("wechat"),
		Phone:   r.FormValue("phone"),
		Email:   r.FormValue("email"),
		OS:      r.FormValue("os"),
		Problem: r.FormValue("problem"),
	}, nil
}

func (s *Server) askError(w http.ResponseWriter, r *http.Request, msg string, code int) {
	if wantsJSON(r) {
		writeJSON(w, code, map[string]any{"ok": false, "error": msg})
		return
	}
	http.Error(w, msg, code)
}

func wantsJSON(r *http.Request) bool {
	accept := r.Header.Get("Accept")
	if strings.Contains(accept, "application/json") {
		return true
	}
	return strings.Contains(r.Header.Get("Content-Type"), "application/json")
}

func (s *Server) handleAdminPage(w http.ResponseWriter, r *http.Request) {
	s.serveFile("admin.html")(w, r)
}

func (s *Server) handleAdminLogin(w http.ResponseWriter, r *http.Request) {
	if err := r.ParseForm(); err != nil {
		http.Error(w, "bad request", http.StatusBadRequest)
		return
	}
	pass := r.FormValue("password")
	if pass == "" && strings.Contains(r.Header.Get("Content-Type"), "application/json") {
		var raw struct {
			Password string `json:"password"`
		}
		_ = json.NewDecoder(io.LimitReader(r.Body, 4096)).Decode(&raw)
		pass = raw.Password
	}
	if !s.auth.PasswordOK(pass) {
		if wantsJSON(r) {
			writeJSON(w, http.StatusUnauthorized, map[string]any{"ok": false, "error": "口令不正确"})
			return
		}
		http.Error(w, "口令不正确", http.StatusUnauthorized)
		return
	}
	tok, err := s.auth.CreateSession()
	if err != nil {
		http.Error(w, "server error", http.StatusInternalServerError)
		return
	}
	s.auth.SetSessionCookie(w, tok)
	if wantsJSON(r) {
		writeJSON(w, http.StatusOK, map[string]any{"ok": true})
		return
	}
	http.Redirect(w, r, "/admin", http.StatusSeeOther)
}

func (s *Server) handleAdminLogout(w http.ResponseWriter, r *http.Request) {
	tok := s.auth.SessionFromRequest(r)
	s.auth.Revoke(tok)
	s.auth.ClearSessionCookie(w)
	if wantsJSON(r) {
		writeJSON(w, http.StatusOK, map[string]any{"ok": true})
		return
	}
	http.Redirect(w, r, "/admin", http.StatusSeeOther)
}

func (s *Server) requireAdmin(next http.HandlerFunc) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		tok := s.auth.SessionFromRequest(r)
		if !s.auth.ValidSession(tok) {
			writeJSON(w, http.StatusUnauthorized, map[string]any{"ok": false, "error": "未登录"})
			return
		}
		next(w, r)
	}
}

func (s *Server) handleListMessages(w http.ResponseWriter, r *http.Request) {
	list, err := s.store.ListMessages()
	if err != nil {
		s.log.Error("list messages", "err", err)
		writeJSON(w, http.StatusInternalServerError, map[string]any{"ok": false})
		return
	}
	writeJSON(w, http.StatusOK, map[string]any{"ok": true, "messages": list})
}

func (s *Server) handleListCustomers(w http.ResponseWriter, r *http.Request) {
	list, err := s.store.ListCustomers()
	if err != nil {
		s.log.Error("list customers", "err", err)
		writeJSON(w, http.StatusInternalServerError, map[string]any{"ok": false})
		return
	}
	writeJSON(w, http.StatusOK, map[string]any{"ok": true, "customers": list})
}

func (s *Server) handleSetStatus(w http.ResponseWriter, r *http.Request) {
	var raw struct {
		ID     int64  `json:"id"`
		Status string `json:"status"`
	}
	if err := json.NewDecoder(io.LimitReader(r.Body, 1<<20)).Decode(&raw); err != nil {
		writeJSON(w, http.StatusBadRequest, map[string]any{"ok": false, "error": "bad json"})
		return
	}
	if err := s.store.SetStatus(raw.ID, raw.Status); err != nil {
		writeJSON(w, http.StatusBadRequest, map[string]any{"ok": false, "error": err.Error()})
		return
	}
	writeJSON(w, http.StatusOK, map[string]any{"ok": true})
}

func (s *Server) handleSetNote(w http.ResponseWriter, r *http.Request) {
	var raw struct {
		ID   int64  `json:"id"`
		Note string `json:"note"`
	}
	if err := json.NewDecoder(io.LimitReader(r.Body, 1<<20)).Decode(&raw); err != nil {
		writeJSON(w, http.StatusBadRequest, map[string]any{"ok": false, "error": "bad json"})
		return
	}
	if err := s.store.SetNote(raw.ID, raw.Note); err != nil {
		writeJSON(w, http.StatusBadRequest, map[string]any{"ok": false, "error": err.Error()})
		return
	}
	writeJSON(w, http.StatusOK, map[string]any{"ok": true})
}

func writeJSON(w http.ResponseWriter, code int, v any) {
	w.Header().Set("Content-Type", "application/json; charset=utf-8")
	w.WriteHeader(code)
	_ = json.NewEncoder(w).Encode(v)
}
