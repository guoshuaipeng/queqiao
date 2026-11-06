package relay

import (
	"encoding/json"
	"io"
	"net"
	"net/http"
	"path/filepath"
	"strings"
	"testing"
	"time"
)

func TestGuestAuditLifecycleAndHTTP(t *testing.T) {
	dir := t.TempDir()
	dbPath := filepath.Join(dir, "relay.db")
	ln, err := net.Listen("tcp", "127.0.0.1:0")
	if err != nil {
		t.Fatal(err)
	}
	auditLn, err := net.Listen("tcp", "127.0.0.1:0")
	if err != nil {
		t.Fatal(err)
	}
	auditAddr := auditLn.Addr().String()
	_ = auditLn.Close()

	token := "test-audit-token-xyz"
	srv, err := NewServer(Config{
		IdleTimeout:        5 * time.Second,
		MaxSessions:        8,
		DataPath:           dbPath,
		AuditAddr:          auditAddr,
		AuditToken:         token,
		AuditRetentionDays: 90,
	})
	if err != nil {
		t.Fatal(err)
	}
	go func() { _ = srv.Serve(ln) }()
	t.Cleanup(func() { _ = srv.Close() })
	addr := ln.Addr().String()

	agent := dial(t, addr)
	defer agent.Close()
	writeJSON(t, agent, map[string]any{"op": "guest_host", "code": "AUD001", "password": "test"})
	assertOK(t, readJSON(t, agent), "guest_host")

	bad := dial(t, addr)
	defer bad.Close()
	writeJSON(t, bad, map[string]any{"op": "guest_join", "code": "AUD001", "password": "nope"})
	if m := readJSON(t, bad); m["ok"] != false {
		t.Fatalf("want fail %#v", m)
	}

	helper := dial(t, addr)
	defer helper.Close()
	writeJSON(t, helper, map[string]any{"op": "guest_join", "code": "AUD001", "password": "test"})
	assertOK(t, readJSON(t, helper), "paired")
	assertOK(t, readJSON(t, agent), "paired")
	_, _ = helper.Write([]byte("x"))
	_ = helper.Close()
	_ = agent.Close()
	time.Sleep(300 * time.Millisecond)

	_ = srv.store.DeleteGuest("AUD001")

	events, err := srv.store.ListGuestAudit("AUD001", 50)
	if err != nil {
		t.Fatal(err)
	}
	kinds := map[string]int{}
	for _, e := range events {
		kinds[e.Event]++
		raw, _ := json.Marshal(e)
		s := string(raw)
		if strings.Contains(s, "password_hash") || strings.Contains(s, `"pwd"`) {
			t.Fatalf("sensitive: %s", s)
		}
	}
	for _, need := range []string{AuditGenerated, AuditJoinFailed, AuditPaired, AuditEnded} {
		if kinds[need] < 1 {
			t.Fatalf("missing event %s in %#v", need, kinds)
		}
	}

	resp, err := http.Get("http://" + auditAddr + "/audit/events")
	if err != nil {
		t.Fatal(err)
	}
	body, _ := io.ReadAll(resp.Body)
	resp.Body.Close()
	if resp.StatusCode != http.StatusUnauthorized {
		t.Fatalf("unauth code=%d body=%s", resp.StatusCode, body)
	}

	req, _ := http.NewRequest(http.MethodGet, "http://"+auditAddr+"/audit/events?code=AUD001", nil)
	req.Header.Set("Authorization", "Bearer "+token)
	resp, err = http.DefaultClient.Do(req)
	if err != nil {
		t.Fatal(err)
	}
	body, _ = io.ReadAll(resp.Body)
	resp.Body.Close()
	if resp.StatusCode != http.StatusOK {
		t.Fatalf("auth code=%d body=%s", resp.StatusCode, body)
	}
	lower := strings.ToLower(string(body))
	for _, forbid := range []string{"password_hash", "remoteaddr"} {
		if strings.Contains(lower, forbid) {
			t.Fatalf("response contains %s: %s", forbid, body)
		}
	}
	var parsed map[string]any
	if err := json.Unmarshal(body, &parsed); err != nil {
		t.Fatal(err)
	}
	if parsed["ok"] != true {
		t.Fatalf("%s", body)
	}
	ev, _ := parsed["events"].([]any)
	if len(ev) < 3 {
		t.Fatalf("want events, got %s", body)
	}
}

func TestAuditHTTPDisabledWithoutToken(t *testing.T) {
	dir := t.TempDir()
	srv, err := NewServer(Config{
		DataPath: filepath.Join(dir, "relay.db"),
	})
	if err != nil {
		t.Fatal(err)
	}
	t.Cleanup(func() { _ = srv.Close() })
	if srv.auditHTTP != nil {
		t.Fatal("audit HTTP should be off without token")
	}
}

func TestClassifyJoinFail(t *testing.T) {
	cases := map[string]string{
		"guest session expired":      FailExpired,
		"invalid code or password":   FailBadCredentials,
		"peer not online":            FailPeerOffline,
		"helper already connected":   FailBusy,
		"too many sessions (max 1)":  FailTooManySessions,
		"missing field code":         FailBadRequest,
		"password too short (min 4)": FailBadRequest,
		"something else":             FailOther,
	}
	for msg, want := range cases {
		if got := classifyJoinFail(errString(msg)); got != want {
			t.Fatalf("%q: got %s want %s", msg, got, want)
		}
	}
}
