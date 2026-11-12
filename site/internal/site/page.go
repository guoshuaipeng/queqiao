package site

import (
	"net/http"
	"strconv"
	"strings"
)

const (
	defaultPageSize = 20
	maxPageSize     = 100
	maxPageNumber   = 10000
)

func normalizePage(page, pageSize int) (int, int) {
	if pageSize <= 0 {
		pageSize = defaultPageSize
	}
	if pageSize > maxPageSize {
		pageSize = maxPageSize
	}
	if page <= 0 {
		page = 1
	}
	if page > maxPageNumber {
		page = maxPageNumber
	}
	return page, pageSize
}

func parsePageQuery(r *http.Request) (page, pageSize int) {
	page, pageSize = 1, defaultPageSize
	if r == nil {
		return page, pageSize
	}
	if v := strings.TrimSpace(r.URL.Query().Get("page")); v != "" {
		if n, err := strconv.Atoi(v); err == nil {
			page = n
		}
	}
	if v := strings.TrimSpace(r.URL.Query().Get("page_size")); v != "" {
		if n, err := strconv.Atoi(v); err == nil {
			pageSize = n
		}
	}
	return normalizePage(page, pageSize)
}
