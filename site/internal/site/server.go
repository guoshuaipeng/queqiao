package site

import (
	"encoding/json"
	"io"
	"io/fs"
	"log/slog"
	"net/http"
	"net/url"
	"os"
	"path/filepath"
	"strconv"
	"strings"
	"time"
)

type Config struct {
	Addr            string
	DataPath        string
	Password        string
	DownloadsDir    string
	Logger          *slog.Logger
	Static          fs.FS
	RelayAuditURL   string // SITE_RELAY_AUDIT_URL e.g. http://127.0.0.1:17992
	RelayAuditToken string // SITE_RELAY_AUDIT_TOKEN (same secret as RELAY_AUDIT_TOKEN)
}

type Server struct {
	cfg        Config
	store      *Store
	auth       *SessionAuth
	mux        *http.ServeMux
	httpServer *http.Server
	log        *slog.Logger
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

func (s *Server) Store() *Store         { return s.store }
func (s *Server) Auth() *SessionAuth    { return s.auth }
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
	s.mux.HandleFunc("GET /admin/api/session-codes", s.requireAdmin(s.handleSessionCodes))
	s.mux.HandleFunc("GET /admin/api/session-code-events", s.requireAdmin(s.handleSessionCodeEvents))
	s.mux.HandleFunc("GET /admin/api/stats", s.requireAdmin(s.handleAccessStats))

	s.mux.HandleFunc("GET /downloads/{name}", s.handleDownload)
}

func (s *Server) handleDownload(w http.ResponseWriter, r *http.Request) {
	dir := strings.TrimSpace(s.cfg.DownloadsDir)
	if dir == "" {
		http.NotFound(w, r)
		return
	}
	name := r.PathValue("name")
	if name == "" || name == "." || name == ".." || strings.Contains(name, "/") || strings.Contains(name, "\\") || strings.Contains(name, "..") {
		http.NotFound(w, r)
		return
	}
	if filepath.Base(name) != name {
		http.NotFound(w, r)
		return
	}
	absDir, err := filepath.Abs(dir)
	if err != nil {
		http.NotFound(w, r)
		return
	}
	full := filepath.Join(absDir, name)
	absFile, err := filepath.Abs(full)
	if err != nil {
		http.NotFound(w, r)
		return
	}
	sep := string(os.PathSeparator)
	if absFile != absDir && !strings.HasPrefix(absFile, absDir+sep) {
		http.NotFound(w, r)
		return
	}
	fi, err := os.Stat(absFile)
	if err != nil || !fi.Mode().IsRegular() {
		http.NotFound(w, r)
		return
	}
	ctype := "application/octet-stream"
	lower := strings.ToLower(name)
	switch {
	case strings.HasSuffix(lower, ".tar.gz") || strings.HasSuffix(lower, ".tgz"):
		ctype = "application/gzip"
	case strings.HasSuffix(lower, ".exe"):
		ctype = "application/octet-stream"
	case strings.HasSuffix(lower, ".dmg"):
		ctype = "application/x-apple-diskimage"
	case strings.HasSuffix(lower, ".zip"):
		ctype = "application/zip"
	}
	w.Header().Set("Content-Type", ctype)
	w.Header().Set("X-Content-Type-Options", "nosniff")
	w.Header().Set("Content-Disposition", "attachment; filename=\""+name+"\"")
	http.ServeFile(w, r, absFile)
	s.recordAccess(AccessDownload, "", name, r)
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
		if shouldRecordVisit(name) {
			s.recordAccess(AccessVisit, normalizeVisitPath(r.URL.Path), "", r)
		}
	}
}

func shouldRecordVisit(name string) bool {
	switch name {
	case "index.html", "ask.html", "thanks.html":
		return true
	default:
		return false
	}
}

func (s *Server) recordAccess(kind, path, fileName string, r *http.Request) {
	if err := s.store.RecordAccess(kind, path, fileName, clientIP(r), r.UserAgent()); err != nil {
		s.log.Error("record access", "kind", kind, "err", err)
	}
}

func (s *Server) handleAccessStats(w http.ResponseWriter, r *http.Request) {
	_, pageSize := parsePageQuery(r)
	q := AccessStatsQuery{
		Section:      strings.TrimSpace(r.URL.Query().Get("section")),
		VisitPage:    1,
		DownloadPage: 1,
		PageSize:     pageSize,
	}
	if v := strings.TrimSpace(r.URL.Query().Get("visit_page")); v != "" {
		if n, err := strconv.Atoi(v); err == nil {
			q.VisitPage = n
		}
	}
	if v := strings.TrimSpace(r.URL.Query().Get("download_page")); v != "" {
		if n, err := strconv.Atoi(v); err == nil {
			q.DownloadPage = n
		}
	}
	if v := strings.TrimSpace(r.URL.Query().Get("page")); v != "" {
		if n, err := strconv.Atoi(v); err == nil {
			if q.Section == "downloads" {
				q.DownloadPage = n
			} else {
				q.VisitPage = n
			}
		}
	}
	// 兼容旧参数 limit：当作 page_size
	if v := strings.TrimSpace(r.URL.Query().Get("limit")); v != "" {
		if n, err := strconv.Atoi(v); err == nil && n > 0 {
			q.PageSize = n
		}
	}
	stats, err := s.store.AccessStats(q)
	if err != nil {
		s.log.Error("access stats", "err", err)
		writeJSON(w, http.StatusInternalServerError, map[string]any{"ok": false})
		return
	}
	enrichAccessIPLocations(stats.RecentVisits)
	enrichAccessIPLocations(stats.RecentDownloads)
	writeJSON(w, http.StatusOK, map[string]any{"ok": true, "stats": stats})
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
	page, pageSize := parsePageQuery(r)
	list, total, err := s.store.ListMessagesPage(page, pageSize)
	if err != nil {
		s.log.Error("list messages", "err", err)
		writeJSON(w, http.StatusInternalServerError, map[string]any{"ok": false})
		return
	}
	writeJSON(w, http.StatusOK, map[string]any{
		"ok": true, "messages": list, "page": page, "page_size": pageSize, "total": total,
	})
}

func (s *Server) handleListCustomers(w http.ResponseWriter, r *http.Request) {
	page, pageSize := parsePageQuery(r)
	list, total, err := s.store.ListCustomersPage(page, pageSize)
	if err != nil {
		s.log.Error("list customers", "err", err)
		writeJSON(w, http.StatusInternalServerError, map[string]any{"ok": false})
		return
	}
	writeJSON(w, http.StatusOK, map[string]any{
		"ok": true, "customers": list, "page": page, "page_size": pageSize, "total": total,
	})
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

func (s *Server) handleSessionCodes(w http.ResponseWriter, r *http.Request) {
	s.proxyRelayAudit(w, r, "/audit/codes", "codes")
}

func (s *Server) handleSessionCodeEvents(w http.ResponseWriter, r *http.Request) {
	s.proxyRelayAudit(w, r, "/audit/events", "events")
}

func (s *Server) proxyRelayAudit(w http.ResponseWriter, r *http.Request, path, listKey string) {
	base := strings.TrimSpace(s.cfg.RelayAuditURL)
	token := strings.TrimSpace(s.cfg.RelayAuditToken)
	if base == "" || token == "" {
		writeJSON(w, http.StatusOK, map[string]any{
			"ok": true, listKey: []any{}, "configured": false,
			"hint": "未配置 SITE_RELAY_AUDIT_URL / SITE_RELAY_AUDIT_TOKEN",
		})
		return
	}
	base = strings.TrimRight(base, "/")
	u, err := url.Parse(base + path)
	if err != nil {
		writeJSON(w, http.StatusBadGateway, map[string]any{"ok": false, "error": "bad relay audit url"})
		return
	}
	q := u.Query()
	if code := strings.TrimSpace(r.URL.Query().Get("code")); code != "" {
		q.Set("code", code)
	}
	page, pageSize := parsePageQuery(r)
	q.Set("page", strconv.Itoa(page))
	q.Set("page_size", strconv.Itoa(pageSize))
	if lim := strings.TrimSpace(r.URL.Query().Get("limit")); lim != "" {
		if n, err := strconv.Atoi(lim); err == nil && n > 0 {
			q.Set("limit", strconv.Itoa(n))
		}
	}
	u.RawQuery = q.Encode()

	req, err := http.NewRequestWithContext(r.Context(), http.MethodGet, u.String(), nil)
	if err != nil {
		writeJSON(w, http.StatusBadGateway, map[string]any{"ok": false, "error": "proxy build failed"})
		return
	}
	req.Header.Set("Authorization", "Bearer "+token)
	req.Header.Set("Accept", "application/json")

	client := &http.Client{Timeout: 8 * time.Second}
	resp, err := client.Do(req)
	if err != nil {
		s.log.Error("relay audit proxy", "err", err)
		writeJSON(w, http.StatusBadGateway, map[string]any{"ok": false, "error": "中继审计不可达"})
		return
	}
	defer resp.Body.Close()
	body, err := io.ReadAll(io.LimitReader(resp.Body, 1<<20))
	if err != nil {
		writeJSON(w, http.StatusBadGateway, map[string]any{"ok": false, "error": "proxy read failed"})
		return
	}
	if resp.StatusCode != http.StatusOK {
		writeJSON(w, http.StatusBadGateway, map[string]any{
			"ok": false, "error": "中继审计返回 " + strconv.Itoa(resp.StatusCode),
		})
		return
	}
	var parsed map[string]any
	if err := json.Unmarshal(body, &parsed); err != nil {
		writeJSON(w, http.StatusBadGateway, map[string]any{"ok": false, "error": "bad relay json"})
		return
	}
	events, _ := parsed[listKey].([]any)
	safe := make([]map[string]any, 0, len(events))
	forbid := map[string]bool{
		"password": true, "pwd": true, "password_hash": true, "fp": true,
		"payload": true, "remote_addr": true, "remoteaddr": true, "pair": true,
	}
	for _, raw := range events {
		row, ok := raw.(map[string]any)
		if !ok {
			continue
		}
		out := map[string]any{}
		for k, v := range row {
			lk := strings.ToLower(k)
			if forbid[lk] || strings.Contains(lk, "password") || lk == "fp" {
				continue
			}
			out[k] = v
		}
		safe = append(safe, out)
	}
	respPage := page
	respSize := pageSize
	if v, ok := parsed["page"].(float64); ok && v > 0 {
		respPage = int(v)
	}
	if v, ok := parsed["page_size"].(float64); ok && v > 0 {
		respSize = int(v)
	}
	total := int64(len(safe))
	switch v := parsed["total"].(type) {
	case float64:
		total = int64(v)
	case int64:
		total = v
	case int:
		total = int64(v)
	}
	writeJSON(w, http.StatusOK, map[string]any{
		"ok": true, listKey: safe, "configured": true,
		"page": respPage, "page_size": respSize, "total": total,
	})
}
