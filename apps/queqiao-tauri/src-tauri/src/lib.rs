mod copy;
mod clipboard;
mod credential;
mod link;
mod policy;
mod settings;
mod tlsutil;
mod tools;
mod wire;

use std::net::TcpStream;
use std::path::PathBuf;
use std::sync::atomic::{AtomicBool, AtomicU64, Ordering};
use std::sync::mpsc::{self, Sender};
use std::sync::Mutex;
use std::thread;
use std::time::{Duration, Instant, SystemTime, UNIX_EPOCH};

use serde::Serialize;
use tauri::{AppHandle, Emitter, Manager, State, WindowEvent};

use copy::{self as text};
use credential::{CredClass, Via};
use link::{AskKind, Ev, Hooks, Job};
use policy::{Decision, Tool, Trust};

struct PromptSlot {
    id: u64,
    tx: Sender<bool>,
}

struct Secrets {
    password: String,
    payload: String,
    code: String,
    pending_via: Option<Via>,
}

#[derive(Clone)]
struct Row {
    t: String,
    text: String,
    kind: String,
}

struct Dialog {
    id: u64,
    kind: String,
    title: String,
    body: String,
    detail: String,
    primary: String,
    secondary: String,
    seconds: u64,
}

struct Model {
    phase: String,
    view: String,
    code: String,
    password_len: usize,
    left_note: String,
    helper_status: String,
    helper_error: String,
    copy_hint: String,
    manual_copy: String,
    dialog: Option<Dialog>,
    trust_saved: String,
    relay: String,
    relay_hint: String,
    audit: Vec<Row>,
    result: String,
    past: Vec<String>,
    op_busy: bool,
    op_status: String,
    peer_ended: bool,
    history: Vec<Row>,
    show_history: bool,
    settings_hint: String,
    full_gen: u64,
    path_open: String,
}

struct Shared {
    model: Mutex<Model>,
    secrets: Mutex<Secrets>,
    prompt: Mutex<Option<PromptSlot>>,
    killer: Mutex<Option<TcpStream>>,
    jobs: Mutex<Option<Sender<Job>>>,
    gen: AtomicU64,
    seq: AtomicU64,
    prompt_ids: AtomicU64,
    worker: AtomicBool,
    app: Mutex<Option<AppHandle>>,
    settings_path: PathBuf,
    history_path: PathBuf,
}

impl Shared {
    fn new() -> Self {
        let loaded = settings::load(&settings::settings_file());
        let history = load_history(&settings::history_file());
        Self {
            model: Mutex::new(Model {
                phase: "idle".into(),
                view: "home".into(),
                code: String::new(),
                password_len: 0,
                left_note: String::new(),
                helper_status: String::new(),
                helper_error: String::new(),
                copy_hint: String::new(),
                manual_copy: String::new(),
                dialog: None,
                trust_saved: policy::trust_token(loaded.saved_trust()).into(),
                relay: loaded.relay,
                relay_hint: String::new(),
                audit: Vec::new(),
                result: String::new(),
                past: Vec::new(),
                op_busy: false,
                op_status: String::new(),
                peer_ended: false,
                history,
                show_history: false,
                settings_hint: String::new(),
                full_gen: 0,
                path_open: String::new(),
            }),
            secrets: Mutex::new(Secrets {
                password: String::new(),
                payload: String::new(),
                code: String::new(),
                pending_via: None,
            }),
            prompt: Mutex::new(None),
            killer: Mutex::new(None),
            jobs: Mutex::new(None),
            gen: AtomicU64::new(1),
            seq: AtomicU64::new(1),
            prompt_ids: AtomicU64::new(1),
            worker: AtomicBool::new(false),
            app: Mutex::new(None),
            settings_path: settings::settings_file(),
            history_path: settings::history_file(),
        }
    }
}

struct AppHooks {
    shared: std::sync::Arc<Shared>,
    my: u64,
}

impl Hooks for AppHooks {
    fn alive(&self) -> bool {
        self.shared.gen.load(Ordering::SeqCst) == self.my
    }
    fn generation(&self) -> u64 {
        self.shared.gen.load(Ordering::SeqCst)
    }
    fn trust(&self) -> Trust {
        let model = self.shared.model.lock().expect("model");
        policy::effective_trust(
            policy::parse_saved_trust(&model.trust_saved),
            model.full_gen,
            self.my,
            model.phase == "host_session",
        )
    }
    fn bind_io(&self, stream: TcpStream) {
        *self.shared.killer.lock().expect("killer") = Some(stream);
    }
    fn peer_gone(&self) -> bool {
        let guard = self.shared.killer.lock().expect("killer");
        let Some(stream) = guard.as_ref() else {
            return false;
        };
        let _ = stream.set_read_timeout(Some(Duration::from_millis(1)));
        let mut buf = [0u8; 1];
        let gone = match stream.peek(&mut buf) {
            Ok(0) => true,
            Ok(_) => false,
            Err(err) => peer_io_closed(&err),
        };
        let _ = stream.set_read_timeout(Some(Duration::from_secs(1)));
        gone
    }
    fn ask(&self, kind: AskKind) -> Decision {
        if !self.alive() {
            return Decision::Cancelled;
        }
        let id = self.shared.prompt_ids.fetch_add(1, Ordering::SeqCst);
        let pair = matches!(kind, AskKind::Pair);
        let (tx, rx) = mpsc::channel();
        {
            let mut slot = self.shared.prompt.lock().expect("prompt");
            *slot = Some(PromptSlot { id, tx });
        }
        {
            let mut model = self.shared.model.lock().expect("model");
            model.dialog = Some(match kind {
                AskKind::Pair => Dialog {
                    id,
                    kind: "pair".into(),
                    title: text::PAIR_TITLE.into(),
                    body: text::PAIR_BODY.into(),
                    detail: String::new(),
                    primary: "允许协助".into(),
                    secondary: "拒绝".into(),
                    seconds: text::CONFIRM_SECS,
                },
                AskKind::Write { headline, detail } => Dialog {
                    id,
                    kind: "write".into(),
                    title: text::WRITE_TITLE.into(),
                    body: headline,
                    detail,
                    primary: "允许".into(),
                    secondary: "拒绝".into(),
                    seconds: text::CONFIRM_SECS,
                },
            });
        }
        self.shared.publish();
        let start = Instant::now();
        loop {
            if !self.alive() {
                self.clear_prompt(id);
                return Decision::Cancelled;
            }
            if pair && self.peer_gone() {
                self.clear_prompt(id);
                return Decision::Deny;
            }
            match rx.recv_timeout(Duration::from_millis(200)) {
                Ok(true) => {
                    self.clear_prompt(id);
                    return if self.alive() { Decision::Allow } else { Decision::Cancelled };
                }
                Ok(false) => {
                    self.clear_prompt(id);
                    return Decision::Deny;
                }
                Err(mpsc::RecvTimeoutError::Timeout) => {
                    if start.elapsed() >= Duration::from_secs(text::CONFIRM_SECS) {
                        self.clear_prompt(id);
                        return Decision::Timeout;
                    }
                }
                Err(mpsc::RecvTimeoutError::Disconnected) => {
                    self.clear_prompt(id);
                    return if self.alive() { Decision::Deny } else { Decision::Cancelled };
                }
            }
        }
    }
    fn emit(&self, ev: Ev) {
        if !self.alive() {
            return;
        }
        let mut model = self.shared.model.lock().expect("model");
        if !self.alive() {
            return;
        }
        let mut clear_secrets = false;
        match ev {
            Ev::Waiting { code, payload } => {
                let mut secrets = self.shared.secrets.lock().expect("secrets");
                secrets.code = code.clone();
                secrets.payload = payload;
                model.phase = "waiting".into();
                model.view = "home".into();
                model.code = credential::display_code(&code);
                model.password_len = secrets.password.chars().count();
                model.left_note = text::WAIT_NOTE.into();
                model.peer_ended = false;
                model.helper_error.clear();
            }
            Ev::HostSession => {
                model.phase = "host_session".into();
                model.view = "host".into();
                model.dialog = None;
                model.peer_ended = false;
                model.audit.clear();
            }
            Ev::HelperSession => {
                model.phase = "helper_session".into();
                model.view = "helper".into();
                model.helper_status.clear();
                model.helper_error.clear();
                model.result.clear();
                model.past.clear();
                model.op_status.clear();
                model.peer_ended = false;
            }
            Ev::Audit { text: line, kind } => {
                let clean = self.clean(&line);
                model.audit.push(Row {
                    t: clock(),
                    text: clean,
                    kind,
                });
            }
            Ev::Status(s) => {
                if s == text::WAIT_ALLOW {
                    model.phase = "wait_allow".into();
                    model.helper_status = s.clone();
                } else if s == text::CONNECTING {
                    model.phase = "connecting".into();
                    model.helper_status = s.clone();
                }
                model.op_status = s;
            }
            Ev::Result(s) => {
                let previous = model.result.clone();
                if !previous.is_empty() {
                    model.past.insert(0, previous);
                }
                model.result = s;
                model.op_busy = false;
            }
            Ev::Fail(s) => {
                if model.phase == "opening" || model.phase == "waiting" {
                    model.phase = "open_fail".into();
                    model.view = "home".into();
                    model.left_note = s;
                    model.dialog = None;
                    model.code.clear();
                    model.password_len = 0;
                } else {
                    model.phase = "helper_fail".into();
                    model.view = "home".into();
                    model.helper_error = s;
                    model.helper_status.clear();
                }
                model.op_busy = false;
            }
            Ev::PeerEnded => {
                clear_secrets = true;
                let helper = model.view == "helper" || model.phase == "helper_session";
                self.shared.remember_locked(&mut model);
                model.phase = "idle".into();
                model.view = "home".into();
                model.dialog = None;
                model.peer_ended = false;
                model.code.clear();
                model.password_len = 0;
                model.full_gen = 0;
                model.op_busy = false;
                model.op_status.clear();
                model.audit.clear();
                if helper {
                    model.helper_error = text::PEER_ENDED.into();
                    model.helper_status.clear();
                    model.left_note.clear();
                } else {
                    model.left_note = text::PEER_ENDED.into();
                    model.helper_error.clear();
                }
            }
            Ev::Busy(b) => model.op_busy = b,
        }
        drop(model);
        if clear_secrets {
            let mut secrets = self.shared.secrets.lock().expect("secrets");
            secrets.password.clear();
            secrets.payload.clear();
            secrets.code.clear();
            secrets.pending_via = None;
        }
        if self.alive() {
            self.shared.publish();
        }
    }
}

impl AppHooks {
    fn clear_prompt(&self, id: u64) {
        let mut slot = self.shared.prompt.lock().expect("prompt");
        if slot.as_ref().is_some_and(|s| s.id == id) {
            *slot = None;
        }
        drop(slot);
        let mut model = self.shared.model.lock().expect("model");
        if model.dialog.as_ref().is_some_and(|d| d.id == id) {
            model.dialog = None;
        }
        drop(model);
        self.shared.publish();
    }
    fn clean(&self, line: &str) -> String {
        let secrets = self.shared.secrets.lock().expect("secrets");
        let list = vec![
            secrets.password.clone(),
            secrets.payload.clone(),
            secrets.code.clone(),
        ];
        wire::redact(line, &list)
    }
}

impl Shared {
    fn publish(&self) {
        let snap = self.snapshot();
        if let Some(app) = self.app.lock().expect("app").clone() {
            let _ = app.emit("ui", snap);
        }
    }

    fn snapshot(&self) -> Snapshot {
        let model = self.model.lock().expect("model");
        let effective_trust = policy::effective_trust(
            policy::parse_saved_trust(&model.trust_saved),
            model.full_gen,
            self.gen.load(Ordering::SeqCst),
            model.phase == "host_session",
        );
        let effective = match effective_trust {
            Trust::Full => "full",
            Trust::Relaxed => "relaxed",
            Trust::Ask => "ask",
        };
        Snapshot {
            phase: model.phase.clone(),
            view: model.view.clone(),
            code: model.code.clone(),
            password_len: model.password_len,
            left_note: model.left_note.clone(),
            helper_status: model.helper_status.clone(),
            helper_error: model.helper_error.clone(),
            copy_hint: model.copy_hint.clone(),
            manual_copy: model.manual_copy.clone(),
            dialog: model.dialog.as_ref().map(|d| DialogSnap {
                id: d.id,
                kind: d.kind.clone(),
                title: d.title.clone(),
                body: d.body.clone(),
                detail: d.detail.clone(),
                primary: d.primary.clone(),
                secondary: d.secondary.clone(),
                seconds: d.seconds,
            }),
            trust_saved: model.trust_saved.clone(),
            trust_effective: effective.into(),
            relay: model.relay.clone(),
            relay_hint: model.relay_hint.clone(),
            audit: model.audit.iter().map(row_snap).collect(),
            result: model.result.clone(),
            past: model.past.clone(),
            op_busy: model.op_busy,
            op_status: model.op_status.clone(),
            peer_ended: model.peer_ended,
            history: model.history.iter().map(row_snap).collect(),
            show_history: model.show_history,
            settings_hint: model.settings_hint.clone(),
            version: env!("CARGO_PKG_VERSION").into(),
            path_open: model.path_open.clone(),
            active: is_active(&model.phase),
            generation: self.gen.load(Ordering::SeqCst),
            seq: self.seq.fetch_add(1, Ordering::SeqCst) + 1,
        }
    }

    fn bump(&self) -> u64 {
        // 先作废这一代，读循环下一次超时就会退出。关套接字放在后台，避免 shutdown 卡住界面。
        let next = self.gen.fetch_add(1, Ordering::SeqCst) + 1;
        let stream = self.killer.lock().expect("killer").take();
        *self.jobs.lock().expect("jobs") = None;
        if let Some(stream) = stream {
            thread::spawn(move || abort_io(stream));
        }
        next
    }

    fn wait_worker(&self) -> bool {
        for _ in 0..40 {
            if !self.worker.load(Ordering::SeqCst) {
                return true;
            }
            thread::sleep(Duration::from_millis(100));
        }
        !self.worker.load(Ordering::SeqCst)
    }

    fn remember_locked(&self, model: &mut Model) {
        if model.audit.is_empty() {
            return;
        }
        model.history = model.audit.clone();
        let rows: Vec<HistoryRow> = model
            .history
            .iter()
            .map(|r| HistoryRow {
                t: r.t.clone(),
                text: r.text.clone(),
                kind: r.kind.clone(),
            })
            .collect();
        if let Some(parent) = self.history_path.parent() {
            let _ = std::fs::create_dir_all(parent);
        }
        if let Ok(body) = serde_json::to_string_pretty(&rows) {
            let _ = std::fs::write(&self.history_path, body);
        }
    }

    fn go_idle(&self) {
        let mut secrets = self.secrets.lock().expect("secrets");
        *secrets = Secrets {
            password: String::new(),
            payload: String::new(),
            code: String::new(),
            pending_via: None,
        };
        drop(secrets);
        let mut model = self.model.lock().expect("model");
        model.phase = "idle".into();
        model.view = "home".into();
        model.code.clear();
        model.password_len = 0;
        model.left_note.clear();
        model.helper_status.clear();
        model.helper_error.clear();
        model.copy_hint.clear();
        model.manual_copy.clear();
        model.dialog = None;
        model.audit.clear();
        model.result.clear();
        model.past.clear();
        model.op_busy = false;
        model.op_status.clear();
        model.peer_ended = false;
        model.full_gen = 0;
        model.path_open.clear();
    }
}

fn peer_io_closed(err: &std::io::Error) -> bool {
    matches!(
        err.kind(),
        std::io::ErrorKind::ConnectionReset
            | std::io::ErrorKind::ConnectionAborted
            | std::io::ErrorKind::BrokenPipe
            | std::io::ErrorKind::UnexpectedEof
            | std::io::ErrorKind::NotConnected
    ) || matches!(err.raw_os_error(), Some(10053 | 10054 | 10057 | 10058))
}

fn is_active(phase: &str) -> bool {
    matches!(
        phase,
        "opening" | "waiting" | "stopping" | "host_session" | "connecting" | "wait_allow" | "helper_session"
    ) || phase == "peer_ended"
}

fn row_snap(r: &Row) -> RowSnap {
    RowSnap {
        t: r.t.clone(),
        text: r.text.clone(),
        kind: r.kind.clone(),
    }
}

fn clock() -> String {
    let secs = SystemTime::now().duration_since(UNIX_EPOCH).map(|d| d.as_secs()).unwrap_or(0);
    let h = (secs / 3600 + 8) % 24;
    let m = (secs / 60) % 60;
    format!("{h:02}:{m:02}")
}

#[derive(Clone, Serialize)]
struct Snapshot {
    phase: String,
    view: String,
    code: String,
    #[serde(rename = "passwordLen")]
    password_len: usize,
    #[serde(rename = "leftNote")]
    left_note: String,
    #[serde(rename = "helperStatus")]
    helper_status: String,
    #[serde(rename = "helperError")]
    helper_error: String,
    #[serde(rename = "copyHint")]
    copy_hint: String,
    #[serde(rename = "manualCopy")]
    manual_copy: String,
    dialog: Option<DialogSnap>,
    #[serde(rename = "trustSaved")]
    trust_saved: String,
    #[serde(rename = "trustEffective")]
    trust_effective: String,
    relay: String,
    #[serde(rename = "relayHint")]
    relay_hint: String,
    audit: Vec<RowSnap>,
    result: String,
    past: Vec<String>,
    #[serde(rename = "opBusy")]
    op_busy: bool,
    #[serde(rename = "opStatus")]
    op_status: String,
    #[serde(rename = "peerEnded")]
    peer_ended: bool,
    history: Vec<RowSnap>,
    #[serde(rename = "showHistory")]
    show_history: bool,
    #[serde(rename = "settingsHint")]
    settings_hint: String,
    version: String,
    #[serde(rename = "pathOpen")]
    path_open: String,
    active: bool,
    generation: u64,
    seq: u64,
}

#[derive(Clone, Serialize)]
struct DialogSnap {
    id: u64,
    kind: String,
    title: String,
    body: String,
    detail: String,
    primary: String,
    secondary: String,
    seconds: u64,
}

#[derive(Clone, Serialize)]
struct RowSnap {
    t: String,
    text: String,
    kind: String,
}

#[derive(Clone, Serialize, serde::Deserialize)]
struct HistoryRow {
    t: String,
    text: String,
    kind: String,
}

fn load_history(path: &std::path::Path) -> Vec<Row> {
    let Ok(text) = std::fs::read_to_string(path) else {
        return Vec::new();
    };
    let Ok(rows) = serde_json::from_str::<Vec<HistoryRow>>(&text) else {
        return Vec::new();
    };
    rows.into_iter()
        .map(|r| Row {
            t: r.t,
            text: r.text,
            kind: r.kind,
        })
        .collect()
}

fn copy_text(app: &AppHandle, value: &str) -> Result<(), String> {
    let value = value.to_string();
    let owner = window_hwnd(app);
    let (tx, rx) = std::sync::mpsc::channel();
    app.run_on_main_thread(move || {
        let _ = tx.send(clipboard::set(owner, &value));
    })
    .map_err(|_| text::COPY_FAIL.to_string())?;
    rx.recv_timeout(Duration::from_secs(2))
        .unwrap_or(Err(text::COPY_FAIL.into()))
}

fn paste_text(app: &AppHandle) -> Result<String, String> {
    let owner = window_hwnd(app);
    let (tx, rx) = std::sync::mpsc::channel();
    app.run_on_main_thread(move || {
        let _ = tx.send(clipboard::get(owner));
    })
    .map_err(|_| "没能读取剪贴板。".to_string())?;
    rx.recv_timeout(Duration::from_secs(2))
        .unwrap_or(Err("没能读取剪贴板。".into()))
}

fn window_hwnd(app: &AppHandle) -> isize {
    app.get_webview_window("main")
        .and_then(|window| window.hwnd().ok())
        .map(|handle| handle.0 as isize)
        .unwrap_or(0)
}

fn spawn_host(shared: std::sync::Arc<Shared>, my: u64) {
    shared.worker.store(true, Ordering::SeqCst);
    thread::spawn(move || {
        let hooks = AppHooks {
            shared: shared.clone(),
            my,
        };
        let identity = match tlsutil::create_identity() {
            Ok(id) => id,
            Err(_) => {
                hooks.emit(Ev::Fail(text::OPEN_FAIL.into()));
                shared.worker.store(false, Ordering::SeqCst);
                return;
            }
        };
        let password = wire::new_password();
        {
            let mut secrets = shared.secrets.lock().expect("secrets");
            secrets.password = password.clone();
            secrets.payload.clear();
            secrets.code.clear();
        }
        let relay = shared.model.lock().expect("model").relay.clone();
        let mut code = String::new();
        loop {
            if !hooks.alive() {
                break;
            }
            let tcp = match link::connect_endpoint(
                credential::parse_endpoint(&relay).map(|p| p.0).as_deref().unwrap_or(""),
                credential::parse_endpoint(&relay).map(|p| p.1).unwrap_or(0),
            ) {
                Ok(s) => s,
                Err(()) => {
                    if hooks.alive() {
                        hooks.emit(Ev::Fail(text::OPEN_FAIL.into()));
                    }
                    break;
                }
            };
            match link::run_host_once(tcp, &relay, &password, &code, &identity, &hooks) {
                link::HostEnd::Relisten { code: next } => {
                    code = next;
                    if hooks.alive() {
                        let mut model = shared.model.lock().expect("model");
                        if model.phase == "host_session" {
                            model.phase = "waiting".into();
                            model.view = "home".into();
                            model.full_gen = 0;
                        }
                        drop(model);
                        shared.publish();
                    }
                }
                link::HostEnd::Stop => break,
                link::HostEnd::Fail => {
                    if hooks.alive() {
                        hooks.emit(Ev::Fail(text::OPEN_FAIL.into()));
                    }
                    break;
                }
            }
        }
        shared.worker.store(false, Ordering::SeqCst);
        if shared.gen.load(Ordering::SeqCst) == my {
            shared.publish();
        }
    });
}

fn spawn_helper(shared: std::sync::Arc<Shared>, my: u64, via: Via) {
    let (tx, rx) = mpsc::channel();
    *shared.jobs.lock().expect("jobs") = Some(tx);
    shared.worker.store(true, Ordering::SeqCst);
    thread::spawn(move || {
        let hooks = AppHooks {
            shared: shared.clone(),
            my,
        };
        link::run_helper(via, &rx, &hooks);
        shared.worker.store(false, Ordering::SeqCst);
        if shared.gen.load(Ordering::SeqCst) == my {
            shared.publish();
        }
    });
}

fn abort_io(stream: TcpStream) {
    #[cfg(windows)]
    {
        use std::os::windows::io::AsRawSocket;
        #[repr(C)]
        struct Linger {
            onoff: u16,
            linger: u16,
        }
        #[link(name = "ws2_32")]
        extern "system" {
            fn setsockopt(s: usize, level: i32, opt: i32, val: *const u8, len: i32) -> i32;
        }
        let linger = Linger { onoff: 1, linger: 0 };
        let sock = stream.as_raw_socket() as usize;
        unsafe {
            setsockopt(sock, 0xffff, 0x0080, &linger as *const Linger as *const u8, 4);
        }
    }
    drop(stream);
}

#[tauri::command]
fn get_state(state: State<'_, std::sync::Arc<Shared>>) -> Snapshot {
    state.snapshot()
}

#[tauri::command]
fn start_accept(state: State<'_, std::sync::Arc<Shared>>) -> Result<Snapshot, String> {
    let phase = state.model.lock().expect("model").phase.clone();
    if matches!(phase.as_str(), "connecting" | "wait_allow" | "helper_session" | "helper_fail") {
        open_switch(&state, "to-host");
        return Ok(state.snapshot());
    }
    if is_active(&phase) && phase != "open_fail" && phase != "stop_fail" {
        return Ok(state.snapshot());
    }
    begin_host(state.inner())?;
    Ok(state.snapshot())
}

fn begin_host(state: &std::sync::Arc<Shared>) -> Result<(), String> {
    if state.worker.load(Ordering::SeqCst) && !stop_worker(state) {
        let mut model = state.model.lock().expect("model");
        model.phase = "stop_fail".into();
        model.left_note = text::STOP_FAIL.into();
        drop(model);
        state.publish();
        return Err(text::STOP_FAIL.into());
    }
    {
        let mut model = state.model.lock().expect("model");
        model.phase = "opening".into();
        model.view = "home".into();
        model.left_note = text::OPENING.into();
        model.copy_hint.clear();
        model.manual_copy.clear();
        model.peer_ended = false;
        model.full_gen = 0;
        model.dialog = None;
    }
    let my = state.bump();
    state.publish();
    spawn_host(state.clone(), my);
    Ok(())
}

fn open_switch(state: &Shared, toward: &str) {
    let id = state.prompt_ids.fetch_add(1, Ordering::SeqCst);
    let phase = state.model.lock().expect("model").phase.clone();
    let peer_ended = state.model.lock().expect("model").peer_ended;
    let (title_body, primary) = if toward == "to-host" || phase == "host_session" || peer_ended {
        (text::SWITCH_SESSION_BODY, "结束并切换")
    } else {
        (text::SWITCH_WAIT_BODY, "停止并切换")
    };
    let mut model = state.model.lock().expect("model");
    model.dialog = Some(Dialog {
        id,
        kind: format!("switch:{toward}"),
        title: "要切换吗？".into(),
        body: title_body.into(),
        detail: String::new(),
        primary: primary.into(),
        secondary: "取消".into(),
        seconds: text::CONFIRM_SECS,
    });
    drop(model);
    state.publish();
}

fn stop_worker(state: &std::sync::Arc<Shared>) -> bool {
    let _ = state.bump();
    state.wait_worker()
}

#[tauri::command]
fn stop_accept(state: State<'_, std::sync::Arc<Shared>>) -> Result<Snapshot, String> {
    {
        let mut model = state.model.lock().expect("model");
        model.phase = "stopping".into();
        model.left_note = text::STOPPING.into();
        model.dialog = None;
        model.copy_hint.clear();
        model.manual_copy.clear();
        model.code.clear();
        model.password_len = 0;
    }
    state.publish();
    let _ = state.bump();
    if !state.wait_worker() {
        let mut model = state.model.lock().expect("model");
        model.phase = "stop_fail".into();
        model.left_note = text::STOP_FAIL.into();
        drop(model);
        state.publish();
        return Err(text::STOP_FAIL.into());
    }
    state.go_idle();
    state.publish();
    Ok(state.snapshot())
}

#[tauri::command]
fn copy_credential(app: AppHandle, state: State<'_, std::sync::Arc<Shared>>) -> Result<Snapshot, String> {
    let payload = state.secrets.lock().expect("secrets").payload.clone();
    let mut model = state.model.lock().expect("model");
    if payload.is_empty() || model.phase != "waiting" {
        model.copy_hint = text::COPY_FAIL.into();
        drop(model);
        state.publish();
        return Ok(state.snapshot());
    }
    match copy_text(&app, &payload) {
        Ok(()) => match paste_text(&app) {
            Ok(got) if got == payload => {
                model.copy_hint = text::COPY_OK.into();
                model.manual_copy.clear();
            }
            _ => {
                model.copy_hint = text::COPY_FAIL.into();
                model.manual_copy = payload;
            }
        },
        Err(_) => {
            model.copy_hint = text::COPY_FAIL.into();
            model.manual_copy = payload;
        }
    }
    drop(model);
    state.publish();
    Ok(state.snapshot())
}

#[tauri::command]
fn confirm_copy(app: AppHandle, state: State<'_, std::sync::Arc<Shared>>) -> Result<Snapshot, String> {
    let payload = state.secrets.lock().expect("secrets").payload.clone();
    let mut model = state.model.lock().expect("model");
    if payload.is_empty() || model.phase != "waiting" {
        model.copy_hint = text::COPY_FAIL.into();
        drop(model);
        state.publish();
        return Ok(state.snapshot());
    }
    match paste_text(&app) {
        Ok(got) if got == payload => {
            model.copy_hint = text::COPY_OK.into();
            model.manual_copy.clear();
        }
        _ => {
            model.copy_hint = text::COPY_FAIL.into();
            model.manual_copy = payload;
        }
    }
    drop(model);
    state.publish();
    Ok(state.snapshot())
}

#[tauri::command]
fn reveal_password(state: State<'_, std::sync::Arc<Shared>>) -> Result<String, String> {
    let phase = state.model.lock().expect("model").phase.clone();
    if phase != "waiting" {
        return Err("现在没有可显示的密码。".into());
    }
    let pwd = state.secrets.lock().expect("secrets").password.clone();
    if pwd.is_empty() {
        return Err("现在没有可显示的密码。".into());
    }
    Ok(pwd)
}

#[tauri::command]
fn paste_clipboard(app: AppHandle) -> Result<String, String> {
    paste_text(&app)
}

#[tauri::command]
fn connect(state: State<'_, std::sync::Arc<Shared>>, payload: String) -> Result<Snapshot, String> {
    match credential::classify(&payload) {
        CredClass::Format => {
            let mut model = state.model.lock().expect("model");
            model.helper_error = text::FORMAT_BAD.into();
            model.phase = if is_active(&model.phase) { model.phase.clone() } else { "helper_fail".into() };
            drop(model);
            state.publish();
            return Ok(state.snapshot());
        }
        CredClass::Incomplete => {
            let mut model = state.model.lock().expect("model");
            model.helper_error = text::INCOMPLETE.into();
            if !is_active(&model.phase) {
                model.phase = "helper_fail".into();
            }
            drop(model);
            state.publish();
            return Ok(state.snapshot());
        }
        CredClass::Ok(via) => {
            let phase = state.model.lock().expect("model").phase.clone();
            if matches!(phase.as_str(), "opening" | "waiting" | "host_session" | "stopping" | "peer_ended") {
                state.secrets.lock().expect("secrets").pending_via = Some(via);
                let toward = "to-helper";
                open_switch(&state, toward);
                return Ok(state.snapshot());
            }
            if matches!(phase.as_str(), "connecting" | "wait_allow" | "helper_session") {
                return Ok(state.snapshot());
            }
            begin_helper(state.inner(), via)?;
            Ok(state.snapshot())
        }
    }
}

fn begin_helper(state: &std::sync::Arc<Shared>, via: Via) -> Result<(), String> {
    if state.worker.load(Ordering::SeqCst) && !stop_worker(state) {
        return Err(text::STOP_FAIL.into());
    }
    {
        let mut model = state.model.lock().expect("model");
        model.phase = "connecting".into();
        model.view = "home".into();
        model.helper_status = text::CONNECTING.into();
        model.helper_error.clear();
        model.dialog = None;
        model.full_gen = 0;
    }
    let my = state.bump();
    state.publish();
    spawn_helper(state.clone(), my, via);
    Ok(())
}

#[tauri::command]
fn cancel_connect(state: State<'_, std::sync::Arc<Shared>>) -> Result<Snapshot, String> {
    let _ = state.bump();
    let _ = state.wait_worker();
    let mut model = state.model.lock().expect("model");
    model.phase = "helper_fail".into();
    model.view = "home".into();
    model.helper_status.clear();
    model.helper_error = text::CANCELLED_CONNECT.into();
    model.op_busy = false;
    model.op_status.clear();
    model.dialog = None;
    model.peer_ended = false;
    drop(model);
    state.publish();
    Ok(state.snapshot())
}

#[tauri::command]
fn resolve_dialog(state: State<'_, std::sync::Arc<Shared>>, id: u64, allow: bool) -> Result<Snapshot, String> {
    let kind = {
        let model = state.model.lock().expect("model");
        match &model.dialog {
            Some(d) if d.id == id => d.kind.clone(),
            _ => return Ok(state.snapshot()),
        }
    };
    if kind == "pair" || kind == "write" {
        let slot = state.prompt.lock().expect("prompt");
        if let Some(prompt) = slot.as_ref() {
            if prompt.id == id {
                let _ = prompt.tx.send(allow);
            }
        }
        drop(slot);
        return Ok(state.snapshot());
    }
    {
        let mut model = state.model.lock().expect("model");
        model.dialog = None;
    }
    if !allow {
        state.publish();
        return Ok(state.snapshot());
    }
    if kind == "end" {
        finish_session(state.inner())?;
    } else if kind == "quit" {
        finish_session(state.inner()).ok();
        if let Some(app) = state.app.lock().expect("app").clone() {
            app.exit(0);
        }
    } else if kind == "full" {
        let phase = state.model.lock().expect("model").phase.clone();
        if phase == "host_session" {
            let gen = state.gen.load(Ordering::SeqCst);
            let mut model = state.model.lock().expect("model");
            model.full_gen = gen;
            model.settings_hint.clear();
        } else {
            let mut model = state.model.lock().expect("model");
            model.settings_hint = text::FULL_NEEDS_PEER.into();
            model.full_gen = 0;
        }
        state.publish();
    } else if kind.starts_with("switch:") {
        let toward_host = kind.ends_with("to-host");
        let pending = state.secrets.lock().expect("secrets").pending_via.clone();
        if !stop_worker(state.inner()) {
            let mut model = state.model.lock().expect("model");
            model.left_note = text::STOP_FAIL.into();
            model.phase = "stop_fail".into();
            drop(model);
            state.publish();
            return Err(text::STOP_FAIL.into());
        }
        state.go_idle();
        if toward_host {
            begin_host(state.inner())?;
        } else if let Some(via) = pending {
            begin_helper(state.inner(), via)?;
        }
    }
    Ok(state.snapshot())
}

fn finish_session(state: &std::sync::Arc<Shared>) -> Result<(), String> {
    {
        let mut model = state.model.lock().expect("model");
        state.remember_locked(&mut model);
    }
    if !stop_worker(state) {
        return Err(text::STOP_FAIL.into());
    }
    state.go_idle();
    state.publish();
    Ok(())
}

#[tauri::command]
fn request_end(state: State<'_, std::sync::Arc<Shared>>) -> Result<Snapshot, String> {
    let id = state.prompt_ids.fetch_add(1, Ordering::SeqCst);
    let mut model = state.model.lock().expect("model");
    model.dialog = Some(Dialog {
        id,
        kind: "end".into(),
        title: "结束本次协助".into(),
        body: text::END_BODY.into(),
        detail: String::new(),
        primary: "结束协助".into(),
        secondary: "取消".into(),
        seconds: text::CONFIRM_SECS,
    });
    drop(model);
    state.publish();
    Ok(state.snapshot())
}

#[tauri::command]
fn return_home(state: State<'_, std::sync::Arc<Shared>>) -> Result<Snapshot, String> {
    let _ = finish_session(state.inner());
    Ok(state.snapshot())
}

#[tauri::command]
fn run_tool(
    state: State<'_, std::sync::Arc<Shared>>,
    name: String,
    path: String,
    summary: String,
    command: String,
) -> Result<Snapshot, String> {
    let phase = state.model.lock().expect("model").phase.clone();
    if phase != "helper_session" {
        return Err(text::CONNECT_FAIL.into());
    }
    if state.model.lock().expect("model").op_busy {
        return Err(text::BUSY_OP.into());
    }
    let Some(tool) = Tool::parse(&name) else {
        return Err("没有这项操作。".into());
    };
    let mut args = serde_json::Map::new();
    if !path.trim().is_empty() {
        args.insert("path".into(), serde_json::Value::String(path));
    }
    if tool == Tool::RunCommand {
        if command.trim().is_empty() {
            return Err("请先写明要做的改动。".into());
        }
        args.insert("command".into(), serde_json::Value::String(command));
    }
    let gen = state.gen.load(Ordering::SeqCst);
    let tx = state.jobs.lock().expect("jobs").clone();
    let Some(tx) = tx else {
        return Err(text::CONNECT_FAIL.into());
    };
    tx.send(Job {
        tool,
        args,
        summary,
        gen,
    })
    .map_err(|_| text::CONNECT_FAIL.to_string())?;
    let mut model = state.model.lock().expect("model");
    model.op_busy = true;
    model.path_open.clear();
    if !tool.is_read() {
        model.op_status = text::WAIT_CONFIRM.into();
    }
    drop(model);
    state.publish();
    Ok(state.snapshot())
}

#[tauri::command]
fn set_trust(state: State<'_, std::sync::Arc<Shared>>, trust: String) -> Result<Snapshot, String> {
    if trust == "full" {
        let id = state.prompt_ids.fetch_add(1, Ordering::SeqCst);
        let mut model = state.model.lock().expect("model");
        model.dialog = Some(Dialog {
            id,
            kind: "full".into(),
            title: text::FULL_TITLE.into(),
            body: text::FULL_BODY.into(),
            detail: String::new(),
            primary: text::FULL_OK.into(),
            secondary: "取消".into(),
            seconds: text::CONFIRM_SECS,
        });
        drop(model);
        state.publish();
        return Ok(state.snapshot());
    }
    let saved = if trust == "relaxed" { Trust::Relaxed } else { Trust::Ask };
    let token = policy::trust_token(saved);
    {
        let mut model = state.model.lock().expect("model");
        model.trust_saved = token.into();
        model.full_gen = 0;
        let relay = model.relay.clone();
        let file = settings::Settings {
            relay,
            trust: token.into(),
        };
        let _ = settings::save(&state.settings_path, &file);
    }
    state.publish();
    Ok(state.snapshot())
}

#[tauri::command]
fn end_full_trust(state: State<'_, std::sync::Arc<Shared>>) -> Result<Snapshot, String> {
    state.model.lock().expect("model").full_gen = 0;
    state.publish();
    Ok(state.snapshot())
}

#[tauri::command]
fn save_relay(state: State<'_, std::sync::Arc<Shared>>, relay: String) -> Result<Snapshot, String> {
    let relay = relay.trim().to_string();
    if credential::parse_endpoint(&relay).is_none() {
        return Err("服务器地址不对。请填写 主机:端口。".into());
    }
    let busy = is_active(&state.model.lock().expect("model").phase);
    let trust = state.model.lock().expect("model").trust_saved.clone();
    settings::save(
        &state.settings_path,
        &settings::Settings {
            relay: relay.clone(),
            trust,
        },
    )?;
    let mut model = state.model.lock().expect("model");
    model.relay = relay;
    model.relay_hint = if busy { text::SAVED_RELAY_BUSY.into() } else { text::SAVED_RELAY.into() };
    drop(model);
    state.publish();
    Ok(state.snapshot())
}

#[tauri::command]
fn reset_relay(state: State<'_, std::sync::Arc<Shared>>) -> Result<Snapshot, String> {
    save_relay(state, text::DEFAULT_RELAY.into())
}

#[tauri::command]
fn copy_mcp(app: AppHandle, state: State<'_, std::sync::Arc<Shared>>, draft: String) -> Result<Snapshot, String> {
    let payload = {
        let secrets = state.secrets.lock().expect("secrets");
        if !secrets.payload.is_empty() {
            secrets.payload.clone()
        } else if let CredClass::Ok(_) = credential::classify(&draft) {
            draft.trim().to_string()
        } else {
            String::new()
        }
    };
    let mut model = state.model.lock().expect("model");
    if payload.is_empty() {
        model.settings_hint = text::MCP_NEED.into();
        drop(model);
        state.publish();
        return Ok(state.snapshot());
    }
    let exe = find_mcp();
    let exe_text = exe
        .as_ref()
        .map(|p| p.display().to_string().replace('\\', "\\\\"))
        .unwrap_or_else(|| "RemoteAssist.Mcp.exe".into());
    let body = format!(
        "{{\n  \"mcpServers\": {{\n    \"remote-assist\": {{\n      \"command\": \"{exe_text}\",\n      \"args\": [],\n      \"env\": {{\n        \"REMOTE_ASSIST_PAYLOAD\": \"{}\"\n      }}\n    }}\n  }}\n}}",
        payload.replace('\\', "\\\\").replace('"', "\\\"")
    );
    match copy_text(&app, &body) {
        Ok(()) => {
            model.settings_hint = if exe.is_some() { text::MCP_OK.into() } else { text::MCP_PLACEHOLDER.into() };
        }
        Err(_) => model.settings_hint = text::COPY_FAIL.into(),
    }
    drop(model);
    state.publish();
    Ok(state.snapshot())
}

#[tauri::command]
fn copy_usage(app: AppHandle, state: State<'_, std::sync::Arc<Shared>>) -> Result<Snapshot, String> {
    let mut model = state.model.lock().expect("model");
    model.settings_hint = match copy_text(&app, text::usage_text()) {
        Ok(()) => text::USAGE_OK.into(),
        Err(_) => text::COPY_FAIL.into(),
    };
    drop(model);
    state.publish();
    Ok(state.snapshot())
}

#[tauri::command]
fn copy_install(app: AppHandle, state: State<'_, std::sync::Arc<Shared>>) -> Result<Snapshot, String> {
    let mut model = state.model.lock().expect("model");
    model.helper_error.clear();
    model.copy_hint = match copy_text(&app, &text::install_text()) {
        Ok(()) => text::INSTALL_HINT.into(),
        Err(_) => text::COPY_FAIL.into(),
    };
    drop(model);
    state.publish();
    Ok(state.snapshot())
}

#[tauri::command]
fn copy_result(app: AppHandle, state: State<'_, std::sync::Arc<Shared>>) -> Result<Snapshot, String> {
    let result = state.model.lock().expect("model").result.clone();
    let mut model = state.model.lock().expect("model");
    if result.trim().is_empty() {
        model.op_status = text::NO_RESULT.into();
    } else {
        model.op_status = match copy_text(&app, &result) {
            Ok(()) => text::RESULT_COPIED.into(),
            Err(_) => text::COPY_FAIL.into(),
        };
    }
    drop(model);
    state.publish();
    Ok(state.snapshot())
}

#[tauri::command]
fn open_history_dir(state: State<'_, std::sync::Arc<Shared>>) -> Result<Snapshot, String> {
    let dir = settings::base_dir();
    let _ = std::fs::create_dir_all(&dir);
    let status = std::process::Command::new("explorer").arg(&dir).spawn();
    let mut model = state.model.lock().expect("model");
    model.settings_hint = if status.is_ok() {
        "已打开本机协助记录目录。".into()
    } else {
        "没能打开协助记录。".into()
    };
    drop(model);
    state.publish();
    Ok(state.snapshot())
}

#[tauri::command]
fn open_site() -> Result<(), String> {
    std::process::Command::new("cmd")
        .args(["/c", "start", "", text::SITE])
        .spawn()
        .map_err(|_| "没能打开官网。".to_string())?;
    Ok(())
}

#[tauri::command]
fn toggle_history(state: State<'_, std::sync::Arc<Shared>>) -> Result<Snapshot, String> {
    let mut model = state.model.lock().expect("model");
    model.show_history = !model.show_history;
    drop(model);
    state.publish();
    Ok(state.snapshot())
}

#[tauri::command]
fn show_settings(state: State<'_, std::sync::Arc<Shared>>, open: bool) -> Result<Snapshot, String> {
    let mut model = state.model.lock().expect("model");
    model.view = if open { "settings".into() } else { view_for(&model.phase) };
    drop(model);
    state.publish();
    Ok(state.snapshot())
}

fn view_for(phase: &str) -> String {
    match phase {
        "host_session" => "host".into(),
        "helper_session" => "helper".into(),
        _ => "home".into(),
    }
}

fn find_mcp() -> Option<PathBuf> {
    let mut cursor = std::env::current_exe().ok().and_then(|p| p.parent().map(|d| d.to_path_buf()));
    for _ in 0..8 {
        let Some(dir) = cursor else { break };
        let cand = dir.join("artifacts").join("mcp-win-x64-sc").join("RemoteAssist.Mcp.exe");
        if cand.exists() {
            return Some(cand);
        }
        let flat = dir.join("RemoteAssist.Mcp.exe");
        if flat.exists() {
            return Some(flat);
        }
        cursor = dir.parent().map(|p| p.to_path_buf());
    }
    None
}

pub fn run() {
    let shared = std::sync::Arc::new(Shared::new());
    tauri::Builder::default()
        .manage(shared)
        .setup(|app| {
            let shared = app.state::<std::sync::Arc<Shared>>();
            *shared.app.lock().expect("app") = Some(app.handle().clone());
            Ok(())
        })
        .on_window_event(|window, event| {
            if let WindowEvent::CloseRequested { api, .. } = event {
                let shared = window.state::<std::sync::Arc<Shared>>();
                let phase = shared.model.lock().expect("model").phase.clone();
                let has_dialog = shared.model.lock().expect("model").dialog.is_some();
                if is_active(&phase) {
                    api.prevent_close();
                    if !has_dialog {
                        let id = shared.prompt_ids.fetch_add(1, Ordering::SeqCst);
                        shared.model.lock().expect("model").dialog = Some(Dialog {
                            id,
                            kind: "quit".into(),
                            title: "要退出鹊桥吗？".into(),
                            body: text::QUIT_BODY.into(),
                            detail: String::new(),
                            primary: "退出".into(),
                            secondary: "取消".into(),
                            seconds: text::CONFIRM_SECS,
                        });
                        shared.publish();
                    }
                }
            }
        })
        .invoke_handler(tauri::generate_handler![
            get_state,
            start_accept,
            stop_accept,
            copy_credential,
            confirm_copy,
            reveal_password,
            paste_clipboard,
            connect,
            cancel_connect,
            resolve_dialog,
            request_end,
            return_home,
            run_tool,
            set_trust,
            end_full_trust,
            save_relay,
            reset_relay,
            copy_mcp,
            copy_usage,
            copy_install,
            copy_result,
            open_history_dir,
            open_site,
            toggle_history,
            show_settings
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
