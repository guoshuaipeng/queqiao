package site

import (
	"net"
	"testing"
)

func TestFormatIPLocation(t *testing.T) {
	got := formatIPLocation("中国", "北京市", "北京")
	if got != "中国 北京市 北京" {
		t.Fatalf("got %q", got)
	}
	got = formatIPLocation("中国", "北京", "北京")
	if got != "中国 北京" {
		t.Fatalf("dedupe city got %q", got)
	}
	got = formatIPLocation("", "  ", "上海")
	if got != "上海" {
		t.Fatalf("skip empty got %q", got)
	}
}

func TestLookupIPLocationPrivate(t *testing.T) {
	r := newIPGeoResolver()
	r.fetch = func(ip string) string {
		t.Fatalf("should not fetch private ip %s", ip)
		return ""
	}
	defaultIPGeo = r
	t.Cleanup(func() { defaultIPGeo = newIPGeoResolver() })

	cases := []string{"127.0.0.1", "::1", "10.0.0.8", "192.168.1.1", "172.16.5.5"}
	for _, ip := range cases {
		if got := lookupIPLocation(ip); got != "内网" {
			t.Fatalf("%s => %q want 内网", ip, got)
		}
	}
}

func TestLookupIPLocationCaches(t *testing.T) {
	r := newIPGeoResolver()
	calls := 0
	r.fetch = func(ip string) string {
		calls++
		return "中国 广东 深圳"
	}
	defaultIPGeo = r
	t.Cleanup(func() { defaultIPGeo = newIPGeoResolver() })

	ip := "8.8.8.8"
	if !isPublicIP(net.ParseIP(ip)) {
		t.Fatal("expected public")
	}
	if got := lookupIPLocation(ip); got != "中国 广东 深圳" {
		t.Fatalf("got %q", got)
	}
	if got := lookupIPLocation(ip); got != "中国 广东 深圳" {
		t.Fatalf("cache got %q", got)
	}
	if calls != 1 {
		t.Fatalf("calls=%d want 1", calls)
	}
}

func TestEnrichAccessIPLocations(t *testing.T) {
	r := newIPGeoResolver()
	r.fetch = func(ip string) string { return "公网-" + ip }
	defaultIPGeo = r
	t.Cleanup(func() { defaultIPGeo = newIPGeoResolver() })

	events := []AccessEvent{
		{IP: "8.8.8.8"},
		{IP: "8.8.8.8"},
		{IP: "10.1.1.1"},
		{IP: ""},
	}
	enrichAccessIPLocations(events)
	if events[0].IPLocation != "公网-8.8.8.8" || events[1].IPLocation != "公网-8.8.8.8" {
		t.Fatalf("public enrich=%+v", events)
	}
	if events[2].IPLocation != "内网" {
		t.Fatalf("private enrich=%q", events[2].IPLocation)
	}
	if events[3].IPLocation != "" {
		t.Fatalf("empty ip location=%q", events[3].IPLocation)
	}
}
