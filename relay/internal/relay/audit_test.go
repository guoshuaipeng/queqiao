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
	var endedDur *int64
	for _, e := range events {
		if e.Event == AuditEnded {
			endedDur = e.DurationSec
		}
	}
	if endedDur == nil {
		t.Fatal("ended event missing duration_sec")
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

func TestGuestCodesAggregation(t *testing.T) {
	dir := t.TempDir()
	auditLn, err := net.Listen("tcp", "127.0.0.1:0")
	if err != nil {
		t.Fatal(err)
	}
	auditAddr := auditLn.Addr().String()
	_ = auditLn.Close()
	token := "codes-token-xyz"
	srv, err := NewServer(Config{
		DataPath:   filepath.Join(dir, "relay.db"),
		AuditAddr:  auditAddr,
		AuditToken: token,
	})
	if err != nil {
		t.Fatal(err)
	}
	t.Cleanup(func() { _ = srv.Close() })
	put := func(in AuditInsert) {
		t.Helper()
		if err := srv.store.AppendGuestAudit(in); err != nil {
			t.Fatal(err)
		}
	}
	now := time.Now().Unix()
	put(AuditInsert{Event: AuditGenerated, Code: "AGA", OK: intPtr(1)})
	put(AuditInsert{Event: AuditJoinFailed, Code: "AGA", OK: intPtr(0), FailReason: FailBadCredentials, PeerRole: "helper"})
	put(AuditInsert{Event: AuditPaired, Code: "AGA", OK: intPtr(1), PeerRole: "helper", PairedAt: int64Ptr(now)})
	put(AuditInsert{Event: AuditEnded, Code: "AGA", OK: intPtr(1), ClosedAt: int64Ptr(now), DurationSec: int64Ptr(42)})
	put(AuditInsert{Event: AuditGenerated, Code: "AGA", OK: intPtr(1)})
	put(AuditInsert{Event: AuditWaitTimeout, Code: "AGA", OK: intPtr(0), PeerRole: "agent"})
	put(AuditInsert{Event: AuditGenerated, Code: "AGB", OK: intPtr(1)})
	put(AuditInsert{Event: AuditPaired, Code: "AGB", OK: intPtr(1), PeerRole: "helper", PairedAt: int64Ptr(now)})
	put(AuditInsert{Event: AuditGenerated, Code: "AGC", OK: intPtr(1)})

	codes, total, err := srv.store.ListGuestCodesPage("", 1, 20)
	if err != nil {
		t.Fatal(err)
	}
	if total != 3 || len(codes) != 3 {
		t.Fatalf("want 3 codes, got total=%d rows=%d", total, len(codes))
	}
	byCode := map[string]GuestCodeSummary{}
	for _, c := range codes {
		byCode[c.Code] = c
	}
	a := byCode["AGA"]
	if a.Status != CodeTimeout || a.Pairs != 1 || a.Fails != 1 || a.DurationSec != 42 || a.Events != 6 {
		t.Fatalf("AGA summary wrong: %#v", a)
	}
	if a.LastFailReason != FailBadCredentials || a.EndedAt == nil || a.FirstTS > a.LastTS {
		t.Fatalf("AGA fail/end wrong: %#v", a)
	}
	b := byCode["AGB"]
	if b.Status != CodeAssisting || b.EndedAt != nil || b.Pairs != 1 || b.Fails != 0 {
		t.Fatalf("AGB summary wrong: %#v", b)
	}
	c := byCode["AGC"]
	if c.Status != CodeWaiting || c.EndedAt != nil || c.Events != 1 {
		t.Fatalf("AGC summary wrong: %#v", c)
	}

	// join_failed 只累计失败，不把协助中的码改状态。
	put(AuditInsert{Event: AuditJoinFailed, Code: "AGB", OK: intPtr(0), FailReason: FailExpired, PeerRole: "helper"})
	codes, _, err = srv.store.ListGuestCodesPage("", 1, 20)
	if err != nil {
		t.Fatal(err)
	}
	for _, row := range codes {
		if row.Code == "AGB" {
			if row.Status != CodeAssisting || row.Fails != 1 || row.LastFailReason != FailExpired {
				t.Fatalf("AGB after join_failed: %#v", row)
			}
		}
	}

	one, totalOne, err := srv.store.ListGuestCodesPage("AGA", 1, 20)
	if err != nil || totalOne != 1 || len(one) != 1 || one[0].Code != "AGA" {
		t.Fatalf("filter: total=%d rows=%d err=%v", totalOne, len(one), err)
	}
	p1, t1, err := srv.store.ListGuestCodesPage("", 1, 2)
	if err != nil || t1 != 3 || len(p1) != 2 {
		t.Fatalf("page1: total=%d rows=%d err=%v", t1, len(p1), err)
	}
	p2, _, err := srv.store.ListGuestCodesPage("", 2, 2)
	if err != nil || len(p2) != 1 {
		t.Fatalf("page2: rows=%d err=%v", len(p2), err)
	}

	resp, err := http.Get("http://" + auditAddr + "/audit/codes")
	if err != nil {
		t.Fatal(err)
	}
	resp.Body.Close()
	if resp.StatusCode != http.StatusUnauthorized {
		t.Fatalf("unauth codes=%d", resp.StatusCode)
	}
	req, _ := http.NewRequest(http.MethodGet, "http://"+auditAddr+"/audit/codes?page=1&page_size=20", nil)
	req.Header.Set("Authorization", "Bearer "+token)
	resp, err = http.DefaultClient.Do(req)
	if err != nil {
		t.Fatal(err)
	}
	body, _ := io.ReadAll(resp.Body)
	resp.Body.Close()
	if resp.StatusCode != http.StatusOK {
		t.Fatalf("codes code=%d body=%s", resp.StatusCode, body)
	}
	var parsed struct {
		OK    bool               `json:"ok"`
		Codes []GuestCodeSummary `json:"codes"`
		Total int64              `json:"total"`
	}
	if err := json.Unmarshal(body, &parsed); err != nil {
		t.Fatal(err)
	}
	if !parsed.OK || parsed.Total != 3 || len(parsed.Codes) != 3 {
		t.Fatalf("codes payload: %s", body)
	}
	if strings.Contains(strings.ToLower(string(body)), "password") {
		t.Fatalf("sensitive: %s", body)
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
