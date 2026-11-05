package site

import (
	"bytes"
	"encoding/json"
	"io"
	"net/http"
	"net/http/httptest"
	"net/url"
	"os"
	"path/filepath"
	"strings"
	"testing"
	"testing/fstest"
)

func testStatic() fstest.MapFS {
	return fstest.MapFS{
		"index.html":  &fstest.MapFile{Data: []byte("<html>index</html>")},
		"ask.html":    &fstest.MapFile{Data: []byte("<html>ask</html>")},
		"thanks.html": &fstest.MapFile{Data: []byte("<html>thanks</html>")},
		"styles.css":  &fstest.MapFile{Data: []byte("body{}")},
		"admin.html":  &fstest.MapFile{Data: []byte("<html>admin</html>")},
	}
}

func newTestServer(t *testing.T) *Server {
	t.Helper()
	dir := t.TempDir()
	db := filepath.Join(dir, "site.db")
	srv, err := NewServer(Config{
		Addr:     ":8088",
		DataPath: db,
		Password: "dev",
		Static:   testStatic(),
	})
	if err != nil {
		t.Fatal(err)
	}
	t.Cleanup(func() { _ = srv.Close() })
	return srv
}

func TestNormalizeAskValidation(t *testing.T) {
	_, err := NormalizeAsk(AskInput{Name: "", Problem: "x", Wechat: "wx", OS: "Windows"})
	if err == nil || !strings.Contains(err.Error(), "请填完后再发送") {
		t.Fatalf("expected fill error, got %v", err)
	}
	_, err = NormalizeAsk(AskInput{Name: "小王", Problem: "卡顿", OS: "Windows"})
	if err == nil || !strings.Contains(err.Error(), "请留下能联系到你的") {
		t.Fatalf("expected contact error, got %v", err)
	}
	_, err = NormalizeAsk(AskInput{Name: "小王", Problem: "卡顿", Wechat: "a", OS: "Windows"})
	if err == nil || !strings.Contains(err.Error(), "请留下能联系到你的") {
		t.Fatalf("expected short wechat error, got %v", err)
	}
	out, err := NormalizeAsk(AskInput{Name: "小王", Problem: "卡顿", Wechat: "wx_ok", OS: "不清楚"})
	if err != nil {
		t.Fatal(err)
	}
	if out.OS != "不确定" {
		t.Fatalf("os map: got %q", out.OS)
	}
}

func TestSubmitMergesCustomerByWechat(t *testing.T) {
	srv := newTestServer(t)
	id1, cust1, err := srv.store.CreateMessage(AskInput{
		Name: "小王", Wechat: "wang88", OS: "Windows", Problem: "第一次",
	})
	if err != nil {
		t.Fatal(err)
	}
	id2, cust2, err := srv.store.CreateMessage(AskInput{
		Name: "王哥", Wechat: "wang88", Phone: "13800138000", OS: "macOS", Problem: "第二次",
	})
	if err != nil {
		t.Fatal(err)
	}
	if id1 == id2 {
		t.Fatal("expected two messages")
	}
	if cust1 != cust2 {
		t.Fatalf("expected same customer, got %d vs %d", cust1, cust2)
	}
	msgs, err := srv.store.ListMessages()
	if err != nil {
		t.Fatal(err)
	}
	if len(msgs) != 2 {
		t.Fatalf("want 2 messages, got %d", len(msgs))
	}
	if msgs[0].Status != StatusNew || msgs[1].Status != StatusNew {
		t.Fatalf("default status: %+v", msgs)
	}
	customers, err := srv.store.ListCustomers()
	if err != nil {
		t.Fatal(err)
	}
	if len(customers) != 1 {
		t.Fatalf("want 1 customer, got %d", len(customers))
	}
	if customers[0].HistoryCount != 2 {
		t.Fatalf("history_count=%d", customers[0].HistoryCount)
	}
	if customers[0].Phone != "13800138000" {
		t.Fatalf("phone merge: %q", customers[0].Phone)
	}
}

func TestStatusTransitions(t *testing.T) {
	srv := newTestServer(t)
	id, _, err := srv.store.CreateMessage(AskInput{
		Name: "李", Email: "a@b.com", OS: "Linux", Problem: "日志",
	})
	if err != nil {
		t.Fatal(err)
	}
	if err := srv.store.SetStatus(id, StatusContact); err != nil {
		t.Fatal(err)
	}
	if err := srv.store.SetStatus(id, StatusDone); err != nil {
		t.Fatal(err)
	}
	if err := srv.store.SetStatus(id, "处理中"); err == nil {
		t.Fatal("expected invalid status")
	}
	if err := srv.store.SetNote(id, "已电话沟通"); err != nil {
		t.Fatal(err)
	}
	msgs, _ := srv.store.ListMessages()
	if msgs[0].Status != StatusDone || msgs[0].Note != "已电话沟通" {
		t.Fatalf("%+v", msgs[0])
	}
}

func TestAdminRejectsBadPassword(t *testing.T) {
	srv := newTestServer(t)
	form := url.Values{"password": {"wrong"}}
	req := httptest.NewRequest(http.MethodPost, "/admin/login", strings.NewReader(form.Encode()))
	req.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	req.Header.Set("Accept", "application/json")
	rr := httptest.NewRecorder()
	srv.Handler().ServeHTTP(rr, req)
	if rr.Code != http.StatusUnauthorized {
		t.Fatalf("code=%d body=%s", rr.Code, rr.Body.String())
	}

	form = url.Values{"password": {"dev"}}
	req = httptest.NewRequest(http.MethodPost, "/admin/login", strings.NewReader(form.Encode()))
	req.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	req.Header.Set("Accept", "application/json")
	rr = httptest.NewRecorder()
	srv.Handler().ServeHTTP(rr, req)
	if rr.Code != http.StatusOK {
		t.Fatalf("login code=%d body=%s", rr.Code, rr.Body.String())
	}
	cookies := rr.Result().Cookies()
	var session string
	for _, c := range cookies {
		if c.Name == sessionCookie {
			session = c.Value
		}
	}
	if session == "" {
		t.Fatal("missing session cookie")
	}

	// unauthenticated API
	req = httptest.NewRequest(http.MethodGet, "/admin/api/messages", nil)
	rr = httptest.NewRecorder()
	srv.Handler().ServeHTTP(rr, req)
	if rr.Code != http.StatusUnauthorized {
		t.Fatalf("unauth code=%d", rr.Code)
	}

	req = httptest.NewRequest(http.MethodGet, "/admin/api/messages", nil)
	req.AddCookie(&http.Cookie{Name: sessionCookie, Value: session})
	rr = httptest.NewRecorder()
	srv.Handler().ServeHTTP(rr, req)
	if rr.Code != http.StatusOK {
		t.Fatalf("auth messages code=%d body=%s", rr.Code, rr.Body.String())
	}
}

func TestAskHTTPFormAndJSON(t *testing.T) {
	srv := newTestServer(t)
	form := url.Values{
		"name": {"小王"}, "wechat": {"wx1"}, "os": {"Windows"}, "problem": {"蓝屏"},
	}
	req := httptest.NewRequest(http.MethodPost, "/api/ask", strings.NewReader(form.Encode()))
	req.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	rr := httptest.NewRecorder()
	srv.Handler().ServeHTTP(rr, req)
	if rr.Code != http.StatusSeeOther {
		t.Fatalf("form code=%d", rr.Code)
	}
	if loc := rr.Header().Get("Location"); loc != "/thanks" {
		t.Fatalf("location=%q", loc)
	}

	body, _ := json.Marshal(map[string]string{
		"name": "小王", "wechat": "wx1", "os": "Windows", "problem": "又卡了",
	})
	req = httptest.NewRequest(http.MethodPost, "/api/ask", bytes.NewReader(body))
	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("Accept", "application/json")
	rr = httptest.NewRecorder()
	srv.Handler().ServeHTTP(rr, req)
	if rr.Code != http.StatusOK {
		t.Fatalf("json code=%d body=%s", rr.Code, rr.Body.String())
	}
	var resp map[string]any
	_ = json.Unmarshal(rr.Body.Bytes(), &resp)
	if resp["ok"] != true {
		t.Fatalf("resp=%v", resp)
	}
	if _, has := resp["id"]; has {
		t.Fatal("must not return ticket id to visitor")
	}
	raw, _ := io.ReadAll(rr.Body)
	_ = raw

	customers, _ := srv.store.ListCustomers()
	if len(customers) != 1 || customers[0].HistoryCount != 2 {
		t.Fatalf("merge via http: %+v", customers)
	}
}

func TestResolveAdminPassword(t *testing.T) {
	p, err := ResolveAdminPassword(":8088", "")
	if err != nil || p != "dev" {
		t.Fatalf("local default: %q %v", p, err)
	}
	_, err = ResolveAdminPassword(":80", "")
	if err == nil {
		t.Fatal("prod :80 must require password")
	}
	p, err = ResolveAdminPassword(":80", "secret")
	if err != nil || p != "secret" {
		t.Fatalf("explicit: %q %v", p, err)
	}
	_ = os.DevNull
}
