package site

import (
	"net"
	"net/http"
	"strings"
	"time"
	"unicode/utf8"
)

const (
	AccessVisit    = "visit"
	AccessDownload = "download"
)

const maxUserAgentRunes = 512

type AccessEvent struct {
	ID         int64  `json:"id"`
	Kind       string `json:"kind"`
	Path       string `json:"path"`
	FileName   string `json:"file_name"`
	IP         string `json:"ip"`
	IPLocation string `json:"ip_location,omitempty"`
	UserAgent  string `json:"user_agent"`
	CreatedAt  int64  `json:"created_at"`
}

type FileDownloadStat struct {
	FileName string `json:"file_name"`
	Count    int64  `json:"count"`
}

type AccessStats struct {
	VisitCount        int64              `json:"visit_count"`
	VisitUniqueIPs    int64              `json:"visit_unique_ips"`
	DownloadCount     int64              `json:"download_count"`
	DownloadUniqueIPs int64              `json:"download_unique_ips"`
	DownloadsByFile   []FileDownloadStat `json:"downloads_by_file"`
	RecentVisits      []AccessEvent      `json:"recent_visits"`
	VisitPage         int                `json:"visit_page"`
	VisitPageSize     int                `json:"visit_page_size"`
	RecentDownloads   []AccessEvent      `json:"recent_downloads"`
	DownloadPage      int                `json:"download_page"`
	DownloadPageSize  int                `json:"download_page_size"`
}

type AccessStatsQuery struct {
	Section      string // overview | visits | downloads | all
	VisitPage    int
	DownloadPage int
	PageSize     int
}

func clientIP(r *http.Request) string {
	if r == nil {
		return ""
	}
	if xff := strings.TrimSpace(r.Header.Get("X-Forwarded-For")); xff != "" {
		parts := strings.Split(xff, ",")
		if ip := strings.TrimSpace(parts[0]); ip != "" {
			return truncateRunes(ip, 64)
		}
	}
	if xri := strings.TrimSpace(r.Header.Get("X-Real-IP")); xri != "" {
		return truncateRunes(xri, 64)
	}
	host, _, err := net.SplitHostPort(r.RemoteAddr)
	if err != nil {
		return truncateRunes(r.RemoteAddr, 64)
	}
	return truncateRunes(host, 64)
}

func truncateRunes(s string, max int) string {
	if max <= 0 || s == "" {
		return ""
	}
	if utf8.RuneCountInString(s) <= max {
		return s
	}
	runes := []rune(s)
	return string(runes[:max])
}

func normalizeVisitPath(p string) string {
	p = strings.TrimSpace(p)
	if p == "" {
		return "/"
	}
	switch p {
	case "/index.html":
		return "/"
	case "/ask.html":
		return "/ask"
	case "/thanks.html":
		return "/thanks"
	default:
		return p
	}
}

func (s *Store) RecordAccess(kind, path, fileName, ip, ua string) error {
	if s == nil || s.db == nil {
		return nil
	}
	kind = strings.TrimSpace(kind)
	if kind != AccessVisit && kind != AccessDownload {
		return nil
	}
	path = strings.TrimSpace(path)
	fileName = strings.TrimSpace(fileName)
	ip = strings.TrimSpace(ip)
	ua = truncateRunes(strings.TrimSpace(ua), maxUserAgentRunes)
	now := time.Now().Unix()
	_, err := s.db.Exec(`
INSERT INTO access_events(kind, path, file_name, ip, user_agent, created_at)
VALUES(?,?,?,?,?,?)`, kind, path, fileName, ip, ua, now)
	return err
}

func (s *Store) AccessStats(q AccessStatsQuery) (AccessStats, error) {
	var out AccessStats
	out.DownloadsByFile = []FileDownloadStat{}
	out.RecentVisits = []AccessEvent{}
	out.RecentDownloads = []AccessEvent{}
	if s == nil || s.db == nil {
		return out, nil
	}

	visitPage, pageSize := normalizePage(q.VisitPage, q.PageSize)
	dlPage, _ := normalizePage(q.DownloadPage, pageSize)
	out.VisitPage = visitPage
	out.VisitPageSize = pageSize
	out.DownloadPage = dlPage
	out.DownloadPageSize = pageSize
	section := strings.TrimSpace(q.Section)
	if section == "" {
		section = "all"
	}

	var err error
	out.VisitCount, err = s.countAccess(AccessVisit)
	if err != nil {
		return out, err
	}
	out.VisitUniqueIPs, err = s.countUniqueIPs(AccessVisit)
	if err != nil {
		return out, err
	}
	out.DownloadCount, err = s.countAccess(AccessDownload)
	if err != nil {
		return out, err
	}
	out.DownloadUniqueIPs, err = s.countUniqueIPs(AccessDownload)
	if err != nil {
		return out, err
	}
	if section == "overview" || section == "all" {
		out.DownloadsByFile, err = s.downloadsByFile()
		if err != nil {
			return out, err
		}
	}
	if section == "visits" || section == "all" {
		out.RecentVisits, err = s.listAccessPage(AccessVisit, visitPage, pageSize)
		if err != nil {
			return out, err
		}
	}
	if section == "downloads" || section == "all" {
		out.RecentDownloads, err = s.listAccessPage(AccessDownload, dlPage, pageSize)
		if err != nil {
			return out, err
		}
	}
	return out, nil
}

func (s *Store) countAccess(kind string) (int64, error) {
	var n int64
	err := s.db.QueryRow(`SELECT COUNT(1) FROM access_events WHERE kind=?`, kind).Scan(&n)
	return n, err
}

func (s *Store) countUniqueIPs(kind string) (int64, error) {
	var n int64
	err := s.db.QueryRow(`
SELECT COUNT(1) FROM (
  SELECT DISTINCT ip FROM access_events WHERE kind=? AND ip != ''
)`, kind).Scan(&n)
	return n, err
}

func (s *Store) downloadsByFile() ([]FileDownloadStat, error) {
	rows, err := s.db.Query(`
SELECT file_name, COUNT(1) AS c
FROM access_events
WHERE kind=? AND file_name != ''
GROUP BY file_name
ORDER BY c DESC, file_name ASC`, AccessDownload)
	if err != nil {
		return nil, err
	}
	defer rows.Close()
	out := make([]FileDownloadStat, 0)
	for rows.Next() {
		var st FileDownloadStat
		if err := rows.Scan(&st.FileName, &st.Count); err != nil {
			return nil, err
		}
		out = append(out, st)
	}
	return out, rows.Err()
}

func (s *Store) listAccessPage(kind string, page, pageSize int) ([]AccessEvent, error) {
	page, pageSize = normalizePage(page, pageSize)
	offset := (page - 1) * pageSize
	rows, err := s.db.Query(`
SELECT id, kind, path, file_name, ip, user_agent, created_at
FROM access_events
WHERE kind=?
ORDER BY created_at DESC, id DESC
LIMIT ? OFFSET ?`, kind, pageSize, offset)
	if err != nil {
		return nil, err
	}
	defer rows.Close()
	out := make([]AccessEvent, 0, pageSize)
	for rows.Next() {
		var e AccessEvent
		if err := rows.Scan(&e.ID, &e.Kind, &e.Path, &e.FileName, &e.IP, &e.UserAgent, &e.CreatedAt); err != nil {
			return nil, err
		}
		out = append(out, e)
	}
	return out, rows.Err()
}
