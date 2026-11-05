package relay

import (
	"bufio"
	"io"
	"net"
	"sync"
	"time"
)

func (s *Server) pipePair(key string, a, b net.Conn, aR, bR *bufio.Reader) {
	idle := s.cfg.IdleTimeout

	setIdle := func(c net.Conn) {
		_ = c.SetDeadline(time.Now().Add(idle))
	}
	setIdle(a)
	setIdle(b)

	readerOf := func(c net.Conn, br *bufio.Reader) io.Reader {
		if br != nil && br.Buffered() > 0 {
			return io.MultiReader(br, c)
		}
		return c
	}
	ra := readerOf(a, aR)
	rb := readerOf(b, bR)

	var once sync.Once
	done := func() {
		once.Do(func() {
			_ = a.Close()
			_ = b.Close()
		})
	}

	copySide := func(dst net.Conn, src io.Reader, srcConn net.Conn, label string) {
		defer done()
		buf := make([]byte, 32*1024)
		for {
			setIdle(srcConn)
			n, err := src.Read(buf)
			if n > 0 {
				setIdle(dst)
				if _, werr := dst.Write(buf[:n]); werr != nil {
					s.cfg.Logger.Debug("pipe write end", "key", key, "from", label, "err", werr)
					return
				}
			}
			if err != nil {
				return
			}
		}
	}

	go copySide(b, ra, a, "a")
	copySide(a, rb, b, "b")
}
