package main

import (
	"log/slog"
	"os"
	"os/signal"
	"path/filepath"
	"strconv"
	"syscall"

	"gitee.com/CodeYG/remote-assist/relay/internal/relay"
)

func main() {
	logger := slog.New(slog.NewTextHandler(os.Stdout, &slog.HandlerOptions{Level: slog.LevelInfo}))
	slog.SetDefault(logger)

	addr := os.Getenv("RELAY_ADDR")
	if addr == "" {
		addr = ":17891"
	}
	data := os.Getenv("RELAY_DATA")
	if data == "" {
		data = filepath.Join("data", "relay.db")
	}
	auditAddr := os.Getenv("RELAY_AUDIT_ADDR")
	auditToken := os.Getenv("RELAY_AUDIT_TOKEN")
	auditRetention := 90
	if v := os.Getenv("RELAY_AUDIT_RETENTION_DAYS"); v != "" {
		if n, err := strconv.Atoi(v); err == nil && n > 0 {
			auditRetention = n
		}
	}

	srv, err := relay.NewServer(relay.Config{
		Addr:               addr,
		DataPath:           data,
		IdleTimeout:        relay.DefaultIdleTimeout,
		MaxSessions:        relay.DefaultMaxSessions,
		Logger:             logger,
		AuditAddr:          auditAddr,
		AuditToken:         auditToken,
		AuditRetentionDays: auditRetention,
	})
	if err != nil {
		logger.Error("init failed", "err", err)
		os.Exit(1)
	}

	go func() {
		logger.Info("relay listening",
			"addr", addr,
			"data", data,
			"idle", relay.DefaultIdleTimeout.String(),
			"maxSessions", relay.DefaultMaxSessions,
		)
		if err := srv.ListenAndServe(); err != nil {
			logger.Error("server stopped", "err", err)
			os.Exit(1)
		}
	}()

	ch := make(chan os.Signal, 1)
	signal.Notify(ch, os.Interrupt, syscall.SIGTERM)
	sig := <-ch
	logger.Info("shutting down", "signal", sig.String())
	_ = srv.Close()
}
