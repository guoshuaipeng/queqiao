package relay

import (
	"bufio"
	"encoding/json"
	"io"
	"net"
	"os"
	"path/filepath"
	"strings"
	"testing"
	"time"
)

func startTestServer(t *testing.T, maxSessions int) (*Server, string) {
	t.Helper()
	dir := t.TempDir()
	dbPath := filepath.Join(dir, "relay.db")
	ln, err := net.Listen("tcp", "127.0.0.1:0")
	if err != nil {
		t.Fatal(err)
	}
	srv, err := NewServer(Config{
		IdleTimeout: 5 * time.Second,
		MaxSessions: maxSessions,
		DataPath:    dbPath,
	})
	if err != nil {
		t.Fatal(err)
	}
	go func() { _ = srv.Serve(ln) }()
	t.Cleanup(func() { _ = srv.Close() })
	return srv, ln.Addr().String()
}

func dial(t *testing.T, addr string) net.Conn {
	t.Helper()
	c, err := net.DialTimeout("tcp", addr, 2*time.Second)
	if err != nil {
		t.Fatal(err)
	}
	return c
}

func writeJSON(t *testing.T, c net.Conn, v map[string]any) {
	t.Helper()
	b, err := json.Marshal(v)
	if err != nil {
		t.Fatal(err)
	}
	if _, err := c.Write(append(b, '\n')); err != nil {
		t.Fatal(err)
	}
}

func readJSON(t *testing.T, c net.Conn) map[string]any {
	t.Helper()
	_ = c.SetReadDeadline(time.Now().Add(3 * time.Second))
	line, err := bufio.NewReader(c).ReadString('\n')
	if err != nil {
		t.Fatal(err)
	}
	var m map[string]any
	if err := json.Unmarshal([]byte(strings.TrimSpace(line)), &m); err != nil {
		t.Fatalf("json %q: %v", line, err)
	}
	return m
}

func assertOK(t *testing.T, m map[string]any, op string) {
	t.Helper()
	if m["ok"] != true {
		t.Fatalf("want ok for %s, got %#v", op, m)
	}
	if asString(m["op"]) != op {
		t.Fatalf("want op %s, got %#v", op, m)
	}
}

func TestSignupLoginList(t *testing.T) {
	_, addr := startTestServer(t, 8)
	c := dial(t, addr)
	defer c.Close()

	writeJSON(t, c, map[string]any{"op": "signup", "username": "alice", "password": "secret1"})
	m := readJSON(t, c)
	assertOK(t, m, "signup")

	writeJSON(t, c, map[string]any{"op": "login", "username": "alice", "password": "secret1"})
	m = readJSON(t, c)
	assertOK(t, m, "login")
	token := asString(m["token"])
	if token == "" {
		t.Fatal("empty token")
	}

	writeJSON(t, c, map[string]any{"op": "list_devices"})
	m = readJSON(t, c)
	assertOK(t, m, "list_devices")
	devs, _ := m["devices"].([]any)
	if len(devs) != 0 {
		t.Fatalf("want empty devices, got %#v", devs)
	}

	// bad password
	c2 := dial(t, addr)
	defer c2.Close()
	writeJSON(t, c2, map[string]any{"op": "login", "username": "alice", "password": "wrong"})
	m = readJSON(t, c2)
	if m["ok"] != false {
		t.Fatalf("want fail, got %#v", m)
	}
}

func TestGuestPairAndPipe(t *testing.T) {
	_, addr := startTestServer(t, 8)

	agent := dial(t, addr)
	defer agent.Close()
	writeJSON(t, agent, map[string]any{"op": "guest_host", "password": "test"})
	am := readJSON(t, agent)
	assertOK(t, am, "guest_host")
	code := asString(am["code"])
	if len(code) != 6 {
		t.Fatalf("code %q", code)
	}

	helper := dial(t, addr)
	defer helper.Close()
	writeJSON(t, helper, map[string]any{"op": "guest_join", "code": code, "password": "test"})
	hm := readJSON(t, helper)
	assertOK(t, hm, "paired")
	if asString(hm["mode"]) != "guest" {
		t.Fatalf("%#v", hm)
	}

	// agent also gets paired
	am2 := readJSON(t, agent)
	assertOK(t, am2, "paired")

	payload := []byte("hello-relay-bytes")
	if _, err := helper.Write(payload); err != nil {
		t.Fatal(err)
	}
	buf := make([]byte, len(payload))
	_ = agent.SetReadDeadline(time.Now().Add(2 * time.Second))
	if _, err := io.ReadFull(agent, buf); err != nil {
		t.Fatal(err)
	}
	if string(buf) != string(payload) {
		t.Fatalf("got %q", buf)
	}

	back := []byte("ack")
	if _, err := agent.Write(back); err != nil {
		t.Fatal(err)
	}
	buf2 := make([]byte, len(back))
	_ = helper.SetReadDeadline(time.Now().Add(2 * time.Second))
	if _, err := io.ReadFull(helper, buf2); err != nil {
		t.Fatal(err)
	}
	if string(buf2) != string(back) {
		t.Fatalf("got %q", buf2)
	}
}

func TestGuestBadPassword(t *testing.T) {
	_, addr := startTestServer(t, 8)
	agent := dial(t, addr)
	defer agent.Close()
	writeJSON(t, agent, map[string]any{"op": "guest_host", "code": "ABC123", "password": "test"})
	assertOK(t, readJSON(t, agent), "guest_host")

	helper := dial(t, addr)
	defer helper.Close()
	writeJSON(t, helper, map[string]any{"op": "guest_join", "code": "ABC123", "password": "nope"})
	m := readJSON(t, helper)
	if m["ok"] != false {
		t.Fatalf("%#v", m)
	}
}

func TestAccountPairAndPipe(t *testing.T) {
	_, addr := startTestServer(t, 8)

	// signup+login on agent conn
	agent := dial(t, addr)
	defer agent.Close()
	writeJSON(t, agent, map[string]any{"op": "signup", "username": "bob", "password": "passw0rd"})
	assertOK(t, readJSON(t, agent), "signup")
	writeJSON(t, agent, map[string]any{"op": "login", "username": "bob", "password": "passw0rd"})
	lm := readJSON(t, agent)
	assertOK(t, lm, "login")
	token := asString(lm["token"])

	writeJSON(t, agent, map[string]any{"op": "device_online", "device_id": "dev-1", "display_name": "客厅 PC"})
	assertOK(t, readJSON(t, agent), "device_online")

	helper := dial(t, addr)
	defer helper.Close()
	writeJSON(t, helper, map[string]any{"op": "auth_token", "token": token})
	assertOK(t, readJSON(t, helper), "auth_token")
	writeJSON(t, helper, map[string]any{"op": "list_devices"})
	list := readJSON(t, helper)
	assertOK(t, list, "list_devices")
	devs := list["devices"].([]any)
	if len(devs) != 1 {
		t.Fatalf("%#v", list)
	}
	d0 := devs[0].(map[string]any)
	if d0["online"] != true {
		t.Fatalf("want online, %#v", d0)
	}

	writeJSON(t, helper, map[string]any{"op": "connect_device", "device_id": "dev-1"})
	hm := readJSON(t, helper)
	assertOK(t, hm, "paired")
	if asString(hm["mode"]) != "account" {
		t.Fatalf("%#v", hm)
	}
	am := readJSON(t, agent)
	assertOK(t, am, "paired")

	msg := []byte("acct-ping")
	if _, err := helper.Write(msg); err != nil {
		t.Fatal(err)
	}
	buf := make([]byte, len(msg))
	_ = agent.SetReadDeadline(time.Now().Add(2 * time.Second))
	if _, err := io.ReadFull(agent, buf); err != nil {
		t.Fatal(err)
	}
	if string(buf) != string(msg) {
		t.Fatalf("%q", buf)
	}
}

func TestAuthTokenReuse(t *testing.T) {
	_, addr := startTestServer(t, 8)
	c := dial(t, addr)
	defer c.Close()
	writeJSON(t, c, map[string]any{"op": "signup", "username": "carol", "password": "passw0rd"})
	assertOK(t, readJSON(t, c), "signup")
	writeJSON(t, c, map[string]any{"op": "login", "username": "carol", "password": "passw0rd"})
	token := asString(readJSON(t, c)["token"])

	c2 := dial(t, addr)
	defer c2.Close()
	writeJSON(t, c2, map[string]any{"op": "auth_token", "token": token})
	assertOK(t, readJSON(t, c2), "auth_token")
	writeJSON(t, c2, map[string]any{"op": "list_devices"})
	assertOK(t, readJSON(t, c2), "list_devices")
}

func TestMaxSessions(t *testing.T) {
	_, addr := startTestServer(t, 1)
	a1 := dial(t, addr)
	defer a1.Close()
	writeJSON(t, a1, map[string]any{"op": "guest_host", "password": "test"})
	assertOK(t, readJSON(t, a1), "guest_host")

	a2 := dial(t, addr)
	defer a2.Close()
	writeJSON(t, a2, map[string]any{"op": "guest_host", "password": "test"})
	m := readJSON(t, a2)
	if m["ok"] != false || !strings.Contains(asString(m["error"]), "too many") {
		t.Fatalf("%#v", m)
	}
}

func TestUnknownOp(t *testing.T) {
	_, addr := startTestServer(t, 8)
	c := dial(t, addr)
	defer c.Close()
	writeJSON(t, c, map[string]any{"op": "nope"})
	m := readJSON(t, c)
	if m["ok"] != false {
		t.Fatalf("%#v", m)
	}
}

func TestStoreOpenCreatesDir(t *testing.T) {
	dir := t.TempDir()
	path := filepath.Join(dir, "sub", "relay.db")
	st, err := OpenStore(path)
	if err != nil {
		t.Fatal(err)
	}
	defer st.Close()
	if _, err := os.Stat(path); err != nil {
		t.Fatal(err)
	}
}
