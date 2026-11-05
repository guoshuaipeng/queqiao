package sitefs

import "embed"

//go:embed index.html ask.html thanks.html styles.css admin.html
var FS embed.FS
