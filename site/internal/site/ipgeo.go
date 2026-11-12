package site

import (
	"encoding/json"
	"fmt"
	"net"
	"net/http"
	"strings"
	"sync"
	"time"
)

const ipGeoCacheTTL = 24 * time.Hour
const ipGeoCacheMax = 2000
const ipGeoFetchWorkers = 4

type ipGeoEntry struct {
	loc string
	at  time.Time
}

type ipGeoResolver struct {
	mu    sync.Mutex
	items map[string]ipGeoEntry
	cli   *http.Client
	// fetch is overridden in tests.
	fetch func(ip string) string
}

func newIPGeoResolver() *ipGeoResolver {
	r := &ipGeoResolver{
		items: map[string]ipGeoEntry{},
		cli:   &http.Client{Timeout: 800 * time.Millisecond},
	}
	r.fetch = r.fetchIPAPI
	return r
}

var defaultIPGeo = newIPGeoResolver()

func lookupIPLocation(ip string) string {
	return defaultIPGeo.lookup(ip)
}

func (r *ipGeoResolver) cachedOrLocal(ip string) (string, bool) {
	ip = strings.TrimSpace(ip)
	if ip == "" {
		return "", true
	}
	parsed := net.ParseIP(ip)
	if parsed == nil {
		return "", true
	}
	if !isPublicIP(parsed) {
		return "内网", true
	}
	r.mu.Lock()
	defer r.mu.Unlock()
	if e, ok := r.items[ip]; ok && time.Since(e.at) < ipGeoCacheTTL {
		return e.loc, true
	}
	return "", false
}

func (r *ipGeoResolver) store(ip, loc string) {
	r.mu.Lock()
	defer r.mu.Unlock()
	r.items[ip] = ipGeoEntry{loc: loc, at: time.Now()}
	if len(r.items) > ipGeoCacheMax {
		r.items = map[string]ipGeoEntry{ip: {loc: loc, at: time.Now()}}
	}
}

func (r *ipGeoResolver) lookup(ip string) string {
	if loc, ok := r.cachedOrLocal(ip); ok {
		return loc
	}
	loc := ""
	if r.fetch != nil {
		loc = r.fetch(ip)
	}
	r.store(ip, loc)
	return loc
}

func isPublicIP(ip net.IP) bool {
	if ip == nil {
		return false
	}
	if ip.IsLoopback() || ip.IsPrivate() || ip.IsLinkLocalUnicast() || ip.IsLinkLocalMulticast() || ip.IsUnspecified() || ip.IsMulticast() {
		return false
	}
	return ip.IsGlobalUnicast()
}

func (r *ipGeoResolver) fetchIPAPI(ip string) string {
	url := fmt.Sprintf("http://ip-api.com/json/%s?lang=zh-CN&fields=status,message,country,regionName,city", ip)
	req, err := http.NewRequest(http.MethodGet, url, nil)
	if err != nil {
		return ""
	}
	resp, err := r.cli.Do(req)
	if err != nil {
		return ""
	}
	defer resp.Body.Close()
	if resp.StatusCode != http.StatusOK {
		return ""
	}
	var body struct {
		Status     string `json:"status"`
		Country    string `json:"country"`
		RegionName string `json:"regionName"`
		City       string `json:"city"`
	}
	if err := json.NewDecoder(resp.Body).Decode(&body); err != nil {
		return ""
	}
	if body.Status != "success" {
		return ""
	}
	return formatIPLocation(body.Country, body.RegionName, body.City)
}

func formatIPLocation(parts ...string) string {
	out := make([]string, 0, len(parts))
	for _, p := range parts {
		p = strings.TrimSpace(p)
		if p == "" {
			continue
		}
		if len(out) > 0 && out[len(out)-1] == p {
			continue
		}
		out = append(out, p)
	}
	return strings.Join(out, " ")
}

func enrichAccessIPLocations(events []AccessEvent) {
	if len(events) == 0 {
		return
	}
	needFetch := make([]string, 0)
	seen := make(map[string]string, len(events))
	for i := range events {
		ip := strings.TrimSpace(events[i].IP)
		if ip == "" {
			continue
		}
		if loc, ok := seen[ip]; ok {
			events[i].IPLocation = loc
			continue
		}
		if loc, ok := defaultIPGeo.cachedOrLocal(ip); ok {
			seen[ip] = loc
			events[i].IPLocation = loc
			continue
		}
		needFetch = append(needFetch, ip)
		seen[ip] = ""
	}
	if len(needFetch) == 0 {
		return
	}

	type result struct {
		ip  string
		loc string
	}
	jobs := make(chan string, len(needFetch))
	out := make(chan result, len(needFetch))
	workers := ipGeoFetchWorkers
	if workers > len(needFetch) {
		workers = len(needFetch)
	}
	var wg sync.WaitGroup
	for w := 0; w < workers; w++ {
		wg.Add(1)
		go func() {
			defer wg.Done()
			for ip := range jobs {
				loc := ""
				if defaultIPGeo.fetch != nil {
					loc = defaultIPGeo.fetch(ip)
				}
				defaultIPGeo.store(ip, loc)
				out <- result{ip: ip, loc: loc}
			}
		}()
	}
	for _, ip := range needFetch {
		jobs <- ip
	}
	close(jobs)
	wg.Wait()
	close(out)
	for r := range out {
		seen[r.ip] = r.loc
	}
	for i := range events {
		ip := strings.TrimSpace(events[i].IP)
		if ip == "" {
			continue
		}
		events[i].IPLocation = seen[ip]
	}
}
