package main

import (
	"bufio"
	"encoding/json"
	"flag"
	"fmt"
	"io"
	"net"
	"os"
	"strings"
	"time"
)

func main() {
	addr := flag.String("addr", "127.0.0.1:17891", "relay address")
	flag.Parse()

	agent, err := net.DialTimeout("tcp", *addr, 3*time.Second)
	if err != nil {
		fail("dial agent", err)
	}
	defer agent.Close()

	if err := writeJSON(agent, map[string]any{"op": "guest_host", "password": "test"}); err != nil {
		fail("guest_host write", err)
	}
	am, err := readJSON(agent)
	if err != nil {
		fail("guest_host reply", err)
	}
	if am["ok"] != true {
		fail("guest_host", fmt.Errorf("%v", am))
	}
	code := fmt.Sprint(am["code"])
	fmt.Println("agent: guest_host", code)

	helper, err := net.DialTimeout("tcp", *addr, 3*time.Second)
	if err != nil {
		fail("dial helper", err)
	}
	defer helper.Close()
	if err := writeJSON(helper, map[string]any{"op": "guest_join", "code": code, "password": "test"}); err != nil {
		fail("guest_join write", err)
	}
	hm, err := readJSON(helper)
	if err != nil {
		fail("guest_join reply", err)
	}
	if hm["ok"] != true || fmt.Sprint(hm["op"]) != "paired" {
		fail("guest_join", fmt.Errorf("%v", hm))
	}
	fmt.Println("helper: paired")

	am2, err := readJSON(agent)
	if err != nil {
		fail("agent paired", err)
	}
	if am2["ok"] != true || fmt.Sprint(am2["op"]) != "paired" {
		fail("agent paired", fmt.Errorf("%v", am2))
	}
	fmt.Println("agent: paired")

	msg := []byte("smoke-ping")
	if _, err := helper.Write(msg); err != nil {
		fail("helper write", err)
	}
	buf := make([]byte, len(msg))
	_ = agent.SetReadDeadline(time.Now().Add(3 * time.Second))
	if _, err := io.ReadFull(agent, buf); err != nil {
		fail("agent read", err)
	}
	if string(buf) != string(msg) {
		fail("payload", fmt.Errorf("got %q", buf))
	}

	back := []byte("smoke-pong")
	if _, err := agent.Write(back); err != nil {
		fail("agent write", err)
	}
	buf2 := make([]byte, len(back))
	_ = helper.SetReadDeadline(time.Now().Add(3 * time.Second))
	if _, err := io.ReadFull(helper, buf2); err != nil {
		fail("helper read", err)
	}
	if string(buf2) != string(back) {
		fail("payload back", fmt.Errorf("got %q", buf2))
	}

	fmt.Println("SMOKE OK", code)
}

func writeJSON(c net.Conn, v map[string]any) error {
	b, err := json.Marshal(v)
	if err != nil {
		return err
	}
	_, err = c.Write(append(b, '\n'))
	return err
}

func readJSON(c net.Conn) (map[string]any, error) {
	_ = c.SetReadDeadline(time.Now().Add(5 * time.Second))
	line, err := bufio.NewReader(c).ReadString('\n')
	if err != nil {
		return nil, err
	}
	var m map[string]any
	if err := json.Unmarshal([]byte(strings.TrimSpace(line)), &m); err != nil {
		return nil, err
	}
	return m, nil
}

func fail(where string, err error) {
	fmt.Fprintf(os.Stderr, "SMOKE FAIL %s: %v\n", where, err)
	os.Exit(1)
}
