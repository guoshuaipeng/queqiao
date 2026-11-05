package main

import (
	"log/slog"
	"os"
	"os/signal"
	"path/filepath"
	"syscall"

	sitefs "gitee.com/CodeYG/remote-assist/site"
	"gitee.com/CodeYG/remote-assist/site/internal/site"
)

func main() {
	logger := slog.New(slog.NewTextHandler(os.Stdout, &slog.HandlerOptions{Level: slog.LevelInfo}))
	slog.SetDefault(logger)

	addr := os.Getenv("SITE_ADDR")
	if addr == "" {
		addr = ":8088"
	}
	data := os.Getenv("SITE_DATA")
	if data == "" {
		data = filepath.Join("data", "site.db")
	}
	pass := os.Getenv("SITE_ADMIN_PASSWORD")
	// ResolveAdminPassword inside NewServer: empty + local addr => "dev";
	// production :80 requires explicit password. Never log the password.
	downloads := os.Getenv("SITE_DOWNLOADS")
	if downloads == "" {
		downloads = filepath.Join(".", "downloads")
	}

	srv, err := site.NewServer(site.Config{
		Addr:         addr,
		DataPath:     data,
		Password:     pass,
		DownloadsDir: downloads,
		Logger:       logger,
		Static:       sitefs.FS,
	})
	if err != nil {
		logger.Error("init failed", "err", err)
		os.Exit(1)
	}

	go func() {
		logger.Info("site listening", "addr", addr, "data", data, "downloads", downloads)
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
