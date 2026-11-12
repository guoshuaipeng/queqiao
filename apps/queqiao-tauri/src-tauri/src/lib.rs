mod account;
mod copy;
mod clipboard;
mod pake;
mod credential;
mod diag;
mod link;
mod policy;
mod settings;
mod tlsutil;
mod tools;
mod update;
mod wire;

use std::net::TcpStream;
use std::path::PathBuf;
use std::sync::atomic::{AtomicBool, AtomicU64, Ordering};
use std::sync::mpsc::{self, Sender};
use std::sync::Mutex;
use std::thread;
use std::time::{Duration, Instant, SystemTime, UNIX_EPOCH};

use serde::Serialize;
use tauri::menu::{Menu, MenuItem};
use tauri::tray::{MouseButton, MouseButtonState, TrayIconEvent};
use tauri::{AppHandle, Emitter, Manager, State, UserAttentionType, WindowEvent};

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
    /// 协助方粘贴的完整 pair 凭证，供会话页「复制 MCP」；切换角色时 go_idle 会清 payload，此字段保留到会话结束。
    mcp_payload: String,
    code: String,
    pending_via: Option<Via>,
    pending_device: String,
    account_token: String,
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
    /// 配对弹窗选了「完全信任」：等进入 host_session 再落档。
    pending_full_trust: bool,
    path_open: String,
    form_epoch: u64,
    just_dropped: bool,
    long_on: bool,
    long_code: String,
    long_skip: bool,
    session_long: bool,
    session_account: bool,
    account_user: String,
    account_error: String,
    account_allow: bool,
    account_online: bool,
    account_unattended: bool,
    account_skip: bool,
    account_autostart: bool,
    device_name: String,
    host_banner: String,
    /// 被协助会话开始时刻；用于界面「已连接时长」。
    host_connected_at: Option<Instant>,
    devices: Vec<DeviceItem>,
    auto_update: bool,
    update_hint: String,
    update_busy: bool,
    update_available: bool,
    update_version: String,
}

struct DeviceItem {
    id: String,
    name: String,
    online: bool,
    unattended: bool,
    fp: String,
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
    long_gen: AtomicU64,
    long_worker: AtomicBool,
    long_killer: Mutex<Option<TcpStream>>,
    long_skip: AtomicBool,
    account_gen: AtomicU64,
    account_worker: AtomicBool,
    account_killer: Mutex<Option<TcpStream>>,
    account_allow: AtomicBool,
    account_skip: AtomicBool,
    app: Mutex<Option<AppHandle>>,
    settings_path: PathBuf,
    history_path: PathBuf,
    pending_update: Mutex<Option<update::Available>>,
    update_busy: AtomicBool,
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
                pending_full_trust: false,
                path_open: String::new(),
                form_epoch: 0,
                just_dropped: false,
                long_on: !loaded.unattended_code.is_empty() && !loaded.unattended_password.is_empty(),
                long_code: credential::display_code(&loaded.unattended_code),
                // 已开启长久码时一律按「改动不再询问」生效（与 2026-11-12 口径对齐）。
                long_skip: !loaded.unattended_code.is_empty() || loaded.unattended_skip_write,
                session_long: false,
                session_account: false,
                account_user: loaded.account_username.clone(),
                account_error: String::new(),
                account_allow: loaded.account_allow,
                account_online: false,
                account_unattended: loaded.account_unattended,
                account_skip: loaded.account_skip_write,
                account_autostart: loaded.account_autostart,
                device_name: loaded.device_name.clone(),
                host_banner: String::new(),
                host_connected_at: None,
                devices: Vec::new(),
                auto_update: loaded.auto_update,
                update_hint: String::new(),
                update_busy: false,
                update_available: false,
                update_version: String::new(),
            }),
            secrets: Mutex::new(Secrets {
                password: String::new(),
                payload: String::new(),
                mcp_payload: String::new(),
                code: String::new(),
                pending_via: None,
                pending_device: String::new(),
                account_token: loaded.account_token.clone(),
            }),
            prompt: Mutex::new(None),
            killer: Mutex::new(None),
            jobs: Mutex::new(None),
            gen: AtomicU64::new(1),
            seq: AtomicU64::new(1),
            prompt_ids: AtomicU64::new(1),
            worker: AtomicBool::new(false),
            long_gen: AtomicU64::new(1),
            long_worker: AtomicBool::new(false),
            long_killer: Mutex::new(None),
            long_skip: AtomicBool::new(
                !loaded.unattended_code.is_empty() || loaded.unattended_skip_write,
            ),
            account_gen: AtomicU64::new(1),
            account_worker: AtomicBool::new(false),
            account_killer: Mutex::new(None),
            account_allow: AtomicBool::new(loaded.account_allow && !loaded.account_token.is_empty()),
            account_skip: AtomicBool::new(loaded.account_skip_write),
            app: Mutex::new(None),
            settings_path: settings::settings_file(),
            history_path: settings::history_file(),
            pending_update: Mutex::new(None),
            update_busy: AtomicBool::new(false),
        }
    }
}

struct AppHooks {
    shared: std::sync::Arc<Shared>,
    my: u64,
    long: bool,
    account: bool,
}

impl Hooks for AppHooks {
    fn alive(&self) -> bool {
        if self.long {
            self.shared.long_gen.load(Ordering::SeqCst) == self.my
        } else if self.account {
            self.shared.account_gen.load(Ordering::SeqCst) == self.my
        } else {
            self.shared.gen.load(Ordering::SeqCst) == self.my
        }
    }
    fn auto_allow_pair(&self) -> bool {
        if self.account {
            return self.shared.account_allow.load(Ordering::SeqCst);
        }
        self.long
    }
    fn auto_pair_note(&self) -> &'static str {
        if self.account {
            "你的账号已连接，这次不用会话码"
        } else {
            "已按无人值守允许这次协助"
        }
    }
    fn can_take_session(&self) -> bool {
        if !self.account {
            return true;
        }
        let model = self.shared.model.lock().expect("model");
        match model.phase.as_str() {
            "idle" | "open_fail" | "helper_fail" | "stop_fail" => true,
            _ => model.session_account,
        }
    }
    fn generation(&self) -> u64 {
        self.shared.gen.load(Ordering::SeqCst)
    }
    fn trust(&self) -> Trust {
        let model = self.shared.model.lock().expect("model");
        // 长久码无人值守：固定完全信任写操作（开启时已确认）。
        if self.long {
            return Trust::Full;
        }
        if self.account
            && model.account_unattended
            && self.shared.account_skip.load(Ordering::SeqCst)
        {
            return Trust::Full;
        }
        // 设置页「完全信任」按当前会话世代生效；账号/长久会话也要认，不能只看持久偏好。
        policy::effective_trust(
            policy::parse_saved_trust(&model.trust_saved),
            model.full_gen,
            self.my,
            model.phase == "host_session",
        )
    }
    fn bind_io(&self, stream: TcpStream) {
        if self.long {
            *self.shared.long_killer.lock().expect("long killer") = Some(stream);
        } else if self.account {
            *self.shared.account_killer.lock().expect("account killer") = Some(stream);
        } else {
            *self.shared.killer.lock().expect("killer") = Some(stream);
        }
    }
    fn peer_gone(&self) -> bool {
        let guard = if self.long {
            self.shared.long_killer.lock().expect("long killer")
        } else if self.account {
            self.shared.account_killer.lock().expect("account killer")
        } else {
            self.shared.killer.lock().expect("killer")
        };
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
        // 确认窗在主窗口遮罩里；最小化/被挡住时人看不见，超时会当拒绝。
        // unminimize + 尝试置前；抢焦点常被系统拦住，再靠任务栏闪烁提醒。
        raise_for_confirm(&self.shared);
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
        let lane = if self.long {
            "long"
        } else if self.account {
            "account"
        } else {
            "session"
        };
        match &ev {
            Ev::Fail(s) => diag::note(&format!("{lane} fail"), s),
            Ev::Dropped => diag::note(&format!("{lane} dropped"), "连接中断"),
            Ev::PeerEnded => diag::note(&format!("{lane} peer ended"), "对方结束"),
            _ => {}
        }
        if self.long {
            self.emit_long(ev);
            return;
        }
        if self.account {
            self.emit_account(ev);
            return;
        }
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
                model.left_note = if model.just_dropped {
                    text::DROPPED_HOST.into()
                } else {
                    text::WAIT_NOTE.into()
                };
                model.just_dropped = false;
                model.peer_ended = false;
                model.helper_error.clear();
            }
            Ev::HostSession => {
                model.phase = "host_session".into();
                model.view = "host".into();
                model.dialog = None;
                model.peer_ended = false;
                model.host_connected_at = Some(Instant::now());
                take_pending_full_trust(self.shared.as_ref(), &mut model);
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
                } else if matches!(model.phase.as_str(), "connecting" | "wait_allow") {
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
            Ev::Dropped => {
                let helper = model.view == "helper"
                    || matches!(model.phase.as_str(), "helper_session" | "connecting" | "wait_allow" | "helper_fail");
                model.dialog = None;
                model.op_busy = false;
                model.op_status.clear();
                if helper {
                    model.phase = "helper_fail".into();
                    model.view = "home".into();
                    model.helper_error = text::DROPPED.into();
                    model.helper_status = text::DROPPED_NEXT.into();
                } else {
                    model.just_dropped = true;
                    model.left_note = text::DROPPED_HOST.into();
                    if model.phase == "host_session" {
                        model.phase = "waiting".into();
                        model.view = "home".into();
                        model.full_gen = 0;
                        model.pending_full_trust = false;
                        model.session_long = false;
                        model.host_connected_at = None;
                    }
                }
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
                model.pending_full_trust = false;
                model.op_busy = false;
                model.op_status.clear();
                model.audit.clear();
                model.manual_copy.clear();
                model.copy_hint.clear();
                model.result.clear();
                model.past.clear();
                model.host_connected_at = None;
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
            Ev::ClearPropose => model.form_epoch = model.form_epoch.saturating_add(1),
        }
        drop(model);
        if clear_secrets {
            let mut secrets = self.shared.secrets.lock().expect("secrets");
            secrets.password.clear();
            secrets.payload.clear();
            secrets.mcp_payload.clear();
            secrets.code.clear();
            secrets.pending_via = None;
        }
        if self.alive() {
            self.shared.publish();
        }
    }
}

impl AppHooks {
    fn emit_long(&self, ev: Ev) {
        if !self.alive() {
            return;
        }
        let mut publish = true;
        {
            let mut model = self.shared.model.lock().expect("model");
            match ev {
                Ev::Waiting { code, .. } => {
                    model.long_on = true;
                    model.long_code = credential::display_code(&code);
                    model.long_skip = self.shared.long_skip.load(Ordering::SeqCst);
                    if model.session_long {
                        model.session_long = false;
                        if model.phase == "host_session" {
                            model.phase = "idle".into();
                            model.view = "home".into();
                            model.host_connected_at = None;
                            model.left_note = "连接已断开。长久码还能用，对方可以再连接。".into();
                        }
                    }
                    let _ = settings::set_unattended_code(&self.shared.settings_path, &code);
                }
                Ev::HostSession => {
                    if model.phase == "host_session" && !model.session_long {
                        model.settings_hint = "有人用长久码连上了。结束当前协助后可以在这台电脑上看到。".into();
                    } else {
                        model.session_long = true;
                        model.phase = "host_session".into();
                        model.view = "host".into();
                        model.dialog = None;
                        model.host_connected_at = Some(Instant::now());
                        take_pending_full_trust(self.shared.as_ref(), &mut model);
                    }
                }
                Ev::Dropped => {
                    if model.session_long {
                        model.session_long = false;
                        model.phase = "idle".into();
                        model.view = "home".into();
                        model.dialog = None;
                        model.host_connected_at = None;
                        model.left_note = "连接已断开。长久码还能用，对方可以再连接。".into();
                    }
                }
                Ev::Audit { text: line, kind } => {
                    if model.session_long {
                        model.audit.push(Row {
                            t: clock(),
                            text: self.clean(&line),
                            kind,
                        });
                    }
                }
                Ev::Fail(s) => {
                    model.settings_hint = s;
                }
                _ => publish = false,
            }
        }
        if publish && self.alive() {
            self.shared.publish();
        }
    }

    fn emit_account(&self, ev: Ev) {
        if !self.alive() {
            return;
        }
        let mut publish = true;
        {
            let mut model = self.shared.model.lock().expect("model");
            match ev {
                Ev::Waiting { .. } => {
                    model.account_online = true;
                    model.account_error.clear();
                    drop(model);
                    let _ = pull_devices(&self.shared);
                    if self.alive() {
                        self.shared.publish();
                    }
                    return;
                }
                Ev::HostSession => {
                    let banner = if model.account_unattended && model.account_skip {
                        "你的账号已连接。这次改动不会再问你，仍会记下。"
                    } else {
                        "你的账号已连接。改电脑前仍会问你。"
                    };
                    model.session_account = true;
                    model.phase = "host_session".into();
                    model.view = "host".into();
                    model.dialog = None;
                    model.peer_ended = false;
                    model.host_banner = banner.into();
                    model.host_connected_at = Some(Instant::now());
                    take_pending_full_trust(self.shared.as_ref(), &mut model);
                }
                Ev::Dropped | Ev::PeerEnded => {
                    if model.session_account {
                        model.session_account = false;
                        model.phase = "idle".into();
                        model.view = "home".into();
                        model.dialog = None;
                        model.host_banner.clear();
                        model.host_connected_at = None;
                        model.left_note = "连接已断开。你的账号还可以再连。".into();
                        model.account_online = true;
                    }
                }
                Ev::Audit { text: line, kind } => {
                    if model.session_account {
                        model.audit.push(Row {
                            t: clock(),
                            text: self.clean(&line),
                            kind,
                        });
                    }
                }
                Ev::Fail(s) => {
                    model.account_error = s;
                    model.account_online = false;
                }
                _ => publish = false,
            }
        }
        if publish && self.alive() {
            self.shared.publish();
        }
    }

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
            trust_generation(self, &model),
            model.phase == "host_session",
        );
        let mut effective = match effective_trust {
            Trust::Full => "full",
            Trust::Relaxed => "relaxed",
            Trust::Ask => "ask",
        };
        if model.session_account && model.account_unattended && model.account_skip {
            effective = "full";
        }
        let self_id = settings::load(&self.settings_path).device_id;
        let wait_password = if model.phase == "waiting" {
            self.secrets.lock().expect("secrets").password.clone()
        } else {
            String::new()
        };
        Snapshot {
            phase: model.phase.clone(),
            view: model.view.clone(),
            code: model.code.clone(),
            password_len: model.password_len,
            password: wait_password,
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
            form_epoch: model.form_epoch,
            long_on: model.long_on,
            long_code: model.long_code.clone(),
            long_skip: model.long_skip,
            can_autostart: cfg!(windows),
            account_user: model.account_user.clone(),
            account_error: model.account_error.clone(),
            account_allow: model.account_allow,
            account_online: model.account_online,
            account_unattended: model.account_unattended,
            account_skip: model.account_skip,
            account_autostart: model.account_autostart,
            device_name: model.device_name.clone(),
            host_banner: model.host_banner.clone(),
            connected_secs: model
                .host_connected_at
                .map(|t| t.elapsed().as_secs())
                .unwrap_or(0),
            devices: model
                .devices
                .iter()
                .map(|d| DeviceSnap {
                    id: d.id.clone(),
                    name: d.name.clone(),
                    online: d.online,
                    unattended: d.unattended,
                    self_device: !self_id.is_empty() && d.id == self_id,
                })
                .collect(),
            auto_update: model.auto_update,
            update_hint: model.update_hint.clone(),
            update_busy: model.update_busy,
            update_available: model.update_available,
            update_version: model.update_version.clone(),
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
        if let Ok(body) = serde_json::to_string_pretty(&rows) {
            let _ = settings::write_atomic(&self.history_path, &body);
        }
    }

    fn go_idle(&self) {
        let mut secrets = self.secrets.lock().expect("secrets");
        let account_token = secrets.account_token.clone();
        // 切换角色时保留 mcp_payload，供协助会话页复制 MCP；真正结束会话时另清。
        let mcp_payload = secrets.mcp_payload.clone();
        *secrets = Secrets {
            password: String::new(),
            payload: String::new(),
            mcp_payload,
            code: String::new(),
            pending_via: None,
            pending_device: String::new(),
            account_token,
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
        model.pending_full_trust = false;
        model.path_open.clear();
        model.session_long = false;
        model.session_account = false;
        model.host_banner.clear();
        model.host_connected_at = None;
        model.just_dropped = false;
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
    /// 仅等待对方时下发，供本机眼睛图标显示明文；空闲时为空。
    password: String,
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
    #[serde(rename = "formEpoch")]
    form_epoch: u64,
    #[serde(rename = "longOn")]
    long_on: bool,
    #[serde(rename = "longCode")]
    long_code: String,
    #[serde(rename = "longSkip")]
    long_skip: bool,
    /// 本机是否支持开机自启（目前只有 Windows 登记 Run 项）。
    #[serde(rename = "canAutostart")]
    can_autostart: bool,
    #[serde(rename = "accountUser")]
    account_user: String,
    #[serde(rename = "accountError")]
    account_error: String,
    #[serde(rename = "accountAllow")]
    account_allow: bool,
    #[serde(rename = "accountOnline")]
    account_online: bool,
    #[serde(rename = "accountUnattended")]
    account_unattended: bool,
    #[serde(rename = "accountSkip")]
    account_skip: bool,
    #[serde(rename = "accountAutostart")]
    account_autostart: bool,
    #[serde(rename = "deviceName")]
    device_name: String,
    #[serde(rename = "hostBanner")]
    host_banner: String,
    /// 被协助会话已连接秒数；非 host_session 时为 0。
    #[serde(rename = "connectedSecs")]
    connected_secs: u64,
    devices: Vec<DeviceSnap>,
    #[serde(rename = "autoUpdate")]
    auto_update: bool,
    #[serde(rename = "updateHint")]
    update_hint: String,
    #[serde(rename = "updateBusy")]
    update_busy: bool,
    #[serde(rename = "updateAvailable")]
    update_available: bool,
    #[serde(rename = "updateVersion")]
    update_version: String,
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

#[derive(Clone, Serialize)]
struct DeviceSnap {
    id: String,
    name: String,
    online: bool,
    unattended: bool,
    #[serde(rename = "selfDevice")]
    self_device: bool,
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

/// 完全信任绑定的是「当前这条被协助会话」的世代：临时码用 gen，长久码用 long_gen，账号用 account_gen。
fn trust_generation(state: &Shared, model: &Model) -> u64 {
    if model.session_account {
        state.account_gen.load(Ordering::SeqCst)
    } else if model.session_long {
        state.long_gen.load(Ordering::SeqCst)
    } else {
        state.gen.load(Ordering::SeqCst)
    }
}

fn take_pending_full_trust(state: &Shared, model: &mut Model) {
    if !model.pending_full_trust {
        return;
    }
    model.full_gen = trust_generation(state, model);
    model.pending_full_trust = false;
    model.settings_hint.clear();
}

/// 配对弹窗选中的安全等级：每次询问/宽松立即记偏好；完全信任等会话起来再落档。
fn apply_pair_trust_choice(state: &Shared, trust: &str) {
    if trust == "full" {
        let mut model = state.model.lock().expect("model");
        model.pending_full_trust = true;
        model.settings_hint.clear();
        return;
    }
    let saved = if trust == "relaxed" {
        Trust::Relaxed
    } else {
        Trust::Ask
    };
    let token = policy::trust_token(saved);
    let mut model = state.model.lock().expect("model");
    model.trust_saved = token.into();
    model.full_gen = 0;
    model.pending_full_trust = false;
    let relay = model.relay.clone();
    let mut file = settings::load(&state.settings_path);
    file.relay = relay;
    file.trust = token.into();
    let _ = settings::save(&state.settings_path, &file);
}

fn window_hwnd(app: &AppHandle) -> isize {
    #[cfg(windows)]
    {
        return app
            .get_webview_window("main")
            .and_then(|window| window.hwnd().ok())
            .map(|handle| handle.0 as isize)
            .unwrap_or(0);
    }
    #[cfg(not(windows))]
    {
        let _ = app;
        0
    }
}

/// 从托盘/最小化还原主窗并尽量置前。
fn show_main(app: &AppHandle) {
    let Some(win) = app.get_webview_window("main") else {
        return;
    };
    let _ = win.show();
    let _ = win.unminimize();
    let _ = win.set_focus();
}

/// 托盘「退出鹊桥」：空闲直接退出；有活动则亮窗并弹既有 quit 确认。
fn request_quit(shared: &Shared) {
    let phase = shared.model.lock().expect("model").phase.clone();
    let has_dialog = shared.model.lock().expect("model").dialog.is_some();
    if !is_active(&phase) {
        if let Some(app) = shared.app.lock().expect("app").clone() {
            app.exit(0);
        }
        return;
    }
    if let Some(app) = shared.app.lock().expect("app").clone() {
        show_main(&app);
    }
    if has_dialog {
        return;
    }
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

/// 配对/写操作确认弹出时：从托盘/最小化还原并尽量置前；失败则任务栏闪烁（Windows 常限制强制抢焦点）。
fn raise_for_confirm(shared: &Shared) {
    let Some(app) = shared.app.lock().expect("app").clone() else {
        return;
    };
    let handle = app.clone();
    let _ = app.run_on_main_thread(move || {
        show_main(&handle);
        if let Some(win) = handle.get_webview_window("main") {
            let _ = win.request_user_attention(Some(UserAttentionType::Critical));
        }
    });
}

/// 断网或中继重启时不要每 3 秒硬打一次：3s 起指数退避、封顶 60s，
/// 再加抖动，避免一堆客户端在同一秒回连把中继打满。
fn retry_delay(fails: u32) -> Duration {
    let secs = 3u64.saturating_mul(1u64 << fails.min(5)).min(60);
    let mut bytes = [0u8; 2];
    let _ = getrandom::getrandom(&mut bytes);
    let jitter = u64::from(u16::from_le_bytes(bytes)) % (secs * 200 + 1);
    Duration::from_secs(secs) + Duration::from_millis(jitter)
}

/// 退避期间也要能立刻响应「关闭无人值守」，不能一觉睡到 60 秒后才发现世代变了。
fn sleep_while_alive(delay: Duration, alive: &dyn Fn() -> bool) {
    let start = Instant::now();
    while alive() {
        let left = delay.saturating_sub(start.elapsed());
        if left.is_zero() {
            return;
        }
        thread::sleep(left.min(Duration::from_millis(200)));
    }
}

fn spawn_host(shared: std::sync::Arc<Shared>, my: u64) {
    shared.worker.store(true, Ordering::SeqCst);
    thread::spawn(move || {
        let hooks = AppHooks {
            shared: shared.clone(),
            my,
            long: false,
            account: false,
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
        let mut pake_fails = 0u8;
        loop {
            if !hooks.alive() {
                break;
            }
            let tcp = match link::connect_endpoint(
                credential::parse_endpoint(&relay).map(|p| p.0).as_deref().unwrap_or(""),
                credential::parse_endpoint(&relay).map(|p| p.1).unwrap_or(0),
                &|| hooks.alive(),
            ) {
                Ok(s) => s,
                Err(()) => {
                    if hooks.alive() {
                        hooks.emit(Ev::Fail(text::OPEN_FAIL.into()));
                    }
                    break;
                }
            };
            match link::run_host_once(tcp, &relay, &password, &code, &identity, &hooks, true) {
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
                link::HostEnd::Locked => {
                    if hooks.alive() {
                        hooks.emit(Ev::Fail(text::LOCKED_OPEN.into()));
                    }
                    break;
                }
                link::HostEnd::PakeFail => {
                    if !hooks.alive() {
                        break;
                    }
                    pake_fails = pake_fails.saturating_add(1);
                    if pake_fails >= 5 {
                        hooks.emit(Ev::Fail(text::LOCKED_OPEN.into()));
                        break;
                    }
                }
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

fn spawn_long(shared: std::sync::Arc<Shared>) {
    let my = shared.long_gen.load(Ordering::SeqCst);
    shared.long_worker.store(true, Ordering::SeqCst);
    thread::spawn(move || {
        let hooks = AppHooks {
            shared: shared.clone(),
            my,
            long: true,
            account: false,
        };
        let identity = match tlsutil::create_identity() {
            Ok(id) => id,
            Err(_) => {
                hooks.emit(Ev::Fail("没能开启无人值守。".into()));
                if shared.long_gen.load(Ordering::SeqCst) == my {
                    shared.long_worker.store(false, Ordering::SeqCst);
                }
                return;
            }
        };
        let saved = settings::load(&shared.settings_path);
        let password = saved.unattended_password;
        let mut code = saved.unattended_code;
        let mut pake_fails = 0u8;
        let mut net_fails = 0u32;
        loop {
            if !hooks.alive() || password.len() < 8 {
                break;
            }
            let relay = shared.model.lock().expect("model").relay.clone();
            let Some((host, port)) = credential::parse_endpoint(&relay) else {
                break;
            };
            let tcp = match link::connect_endpoint(&host, port, &|| hooks.alive()) {
                Ok(stream) => stream,
                Err(()) => {
                    if !hooks.alive() {
                        break;
                    }
                    net_fails = net_fails.saturating_add(1);
                    // 只在第一次失败时提示，后面安静退避，别每几秒刷一次界面。
                    if net_fails == 1 {
                        hooks.emit(Ev::Fail("没能连上服务器，无人值守会再试。".into()));
                    }
                    let delay = retry_delay(net_fails);
                    diag::note(
                        "long reconnect",
                        &format!("attempt={net_fails} wait_ms={}", delay.as_millis()),
                    );
                    sleep_while_alive(delay, &|| hooks.alive());
                    continue;
                }
            };
            match link::run_host_once(tcp, &relay, &password, &code, &identity, &hooks, true) {
                link::HostEnd::Relisten { code: next } => {
                    code = next;
                    pake_fails = 0;
                    net_fails = 0;
                }
                link::HostEnd::PakeFail => {
                    pake_fails = pake_fails.saturating_add(1);
                    if pake_fails >= 5 && hooks.alive() {
                        hooks.emit(Ev::Fail(text::LOCKED_OPEN.into()));
                        let _ = settings::clear_unattended(&shared.settings_path);
                        let mut model = shared.model.lock().expect("model");
                        model.long_on = false;
                        model.long_code.clear();
                        drop(model);
                        sync_autostart(&shared);
                        shared.publish();
                        break;
                    }
                }
                link::HostEnd::Locked | link::HostEnd::Stop => break,
                link::HostEnd::Fail => {
                    if hooks.alive() {
                        net_fails = net_fails.saturating_add(1);
                        let delay = retry_delay(net_fails);
                        diag::note(
                            "long retry",
                            &format!("attempt={net_fails} wait_ms={}", delay.as_millis()),
                        );
                        sleep_while_alive(delay, &|| hooks.alive());
                    }
                }
            }
        }
        if shared.long_gen.load(Ordering::SeqCst) == my {
            shared.long_worker.store(false, Ordering::SeqCst);
        }
    });
}

fn enable_unattended(state: &std::sync::Arc<Shared>) -> Result<(), String> {
    // 长久码无人值守 = 始终跳过写确认。
    state.long_skip.store(true, Ordering::SeqCst);
    let password = wire::new_password();
    settings::set_unattended(&state.settings_path, "", &password, true)?;
    let _ = state.long_gen.fetch_add(1, Ordering::SeqCst);
    let stream = state.long_killer.lock().expect("long killer").take();
    if let Some(stream) = stream {
        thread::spawn(move || abort_io(stream));
    }
    {
        let mut model = state.model.lock().expect("model");
        model.long_on = true;
        model.long_skip = true;
        model.long_code.clear();
        model.settings_hint = "正在开启无人值守…".into();
    }
    sync_autostart(state);
    spawn_long(state.clone());
    state.publish();
    Ok(())
}

fn revoke_unattended(state: &std::sync::Arc<Shared>) -> Result<(), String> {
    let _ = settings::clear_unattended(&state.settings_path);
    let _ = state.long_gen.fetch_add(1, Ordering::SeqCst);
    state.long_skip.store(false, Ordering::SeqCst);
    let stream = state.long_killer.lock().expect("long killer").take();
    if let Some(stream) = stream {
        thread::spawn(move || abort_io(stream));
    }
    let mut model = state.model.lock().expect("model");
    model.long_on = false;
    model.long_code.clear();
    model.long_skip = false;
    if model.session_long {
        model.session_long = false;
        model.phase = "idle".into();
        model.view = "home".into();
    }
    model.left_note = "长久码已作废，无人时不能再连。".into();
    model.settings_hint = "长久码已作废。".into();
    drop(model);
    sync_autostart(state);
    state.publish();
    Ok(())
}

fn spawn_helper(shared: std::sync::Arc<Shared>, my: u64, via: Via) {
    let (tx, rx) = mpsc::channel();
    *shared.jobs.lock().expect("jobs") = Some(tx);
    shared.worker.store(true, Ordering::SeqCst);
    thread::spawn(move || {
        let hooks = AppHooks {
            shared: shared.clone(),
            my,
            long: false,
            account: false,
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
        CredClass::Ok(mut via) => {
            if let Via::Relay(target) = &mut via {
                if target.pake {
                    let relay = state.model.lock().expect("model").relay.clone();
                    let Some((host, port)) = credential::parse_endpoint(&relay) else {
                        let mut model = state.model.lock().expect("model");
                        model.helper_error = text::CONNECT_FAIL.into();
                        model.phase = "helper_fail".into();
                        drop(model);
                        state.publish();
                        return Ok(state.snapshot());
                    };
                    target.host = host;
                    target.port = port;
                }
            }
            // 协助方连上后，会话页「复制 MCP」要用完整 pair 凭证（短码 MCP 用不了）。
            remember_mcp_payload(&state, &payload);
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

fn remember_mcp_payload(state: &std::sync::Arc<Shared>, payload: &str) {
    let raw = payload.trim();
    // 完整 pair 或短码均可供会话页「复制 MCP」。
    let ok = raw.to_ascii_lowercase().starts_with("remoteassist:pair")
        || matches!(credential::classify(raw), CredClass::Ok(_));
    if !ok {
        return;
    }
    state.secrets.lock().expect("secrets").mcp_payload = raw.to_string();
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
fn resolve_dialog(
    state: State<'_, std::sync::Arc<Shared>>,
    id: u64,
    allow: bool,
    trust: Option<String>,
) -> Result<Snapshot, String> {
    let kind = {
        let model = state.model.lock().expect("model");
        match &model.dialog {
            Some(d) if d.id == id => d.kind.clone(),
            _ => return Ok(state.snapshot()),
        }
    };
    if kind == "pair" || kind == "write" {
        if kind == "pair" && allow {
            apply_pair_trust_choice(state.inner(), trust.as_deref().unwrap_or("ask"));
        }
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
        if kind == "account-unattended" {
            let saved = settings::load(&state.settings_path).account_skip_write;
            state.account_skip.store(saved, Ordering::SeqCst);
        }
        state.publish();
        return Ok(state.snapshot());
    }
    if kind == "end" {
        finish_session(state.inner())?;
    } else if kind == "account-allow" {
        enable_account_allow(state.inner())?;
    } else if kind == "account-unattended" {
        enable_account_unattended(state.inner())?;
    } else if kind == "account-remove" {
        remove_account_device(state.inner())?;
    } else if kind == "unattended" {
        enable_unattended(state.inner())?;
    } else if kind == "unattended-revoke" {
        revoke_unattended(state.inner())?;
    } else if kind == "quit" {
        finish_session(state.inner()).ok();
        if let Some(app) = state.app.lock().expect("app").clone() {
            app.exit(0);
        }
    } else if kind == "update" {
        begin_apply_update(state.inner());
    } else if kind == "full" {
        {
            let mut model = state.model.lock().expect("model");
            if model.phase == "host_session" {
                model.full_gen = trust_generation(state.inner(), &model);
                model.settings_hint.clear();
            } else {
                model.settings_hint = text::FULL_NEEDS_PEER.into();
                model.full_gen = 0;
            }
        }
        state.publish();
    } else if kind.starts_with("switch:") {
        let toward_host = kind.ends_with("to-host");
        let pending = state.secrets.lock().expect("secrets").pending_via.clone();
        let pending_device = state.secrets.lock().expect("secrets").pending_device.clone();
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
        } else if kind.ends_with("to-account") {
            begin_account_helper(state.inner(), &pending_device)?;
        } else if let Some(via) = pending {
            begin_helper(state.inner(), via)?;
        }
    }
    Ok(state.snapshot())
}

fn finish_session(state: &std::sync::Arc<Shared>) -> Result<(), String> {
    let session_long = state.model.lock().expect("model").session_long;
    let session_account = state.model.lock().expect("model").session_account;
    {
        let mut model = state.model.lock().expect("model");
        state.remember_locked(&mut model);
    }
    if session_long {
        let stream = state.long_killer.lock().expect("long killer").take();
        if let Some(stream) = stream {
            thread::spawn(move || abort_io(stream));
        }
        return Ok(());
    }
    if session_account {
        let stream = state.account_killer.lock().expect("account killer").take();
        if let Some(stream) = stream {
            thread::spawn(move || abort_io(stream));
        }
        let mut model = state.model.lock().expect("model");
        model.session_account = false;
        model.phase = "idle".into();
        model.view = "home".into();
        model.host_banner.clear();
        model.dialog = None;
        model.left_note = "这次协助已结束。你的账号还可以再连。".into();
        drop(model);
        state.publish();
        return Ok(());
    }
    if !stop_worker(state) {
        return Err(text::STOP_FAIL.into());
    }
    state.secrets.lock().expect("secrets").mcp_payload.clear();
    state.go_idle();
    state.publish();
    Ok(())
}

#[tauri::command]
fn request_end(state: State<'_, std::sync::Arc<Shared>>) -> Result<Snapshot, String> {
    let id = state.prompt_ids.fetch_add(1, Ordering::SeqCst);
    let mut model = state.model.lock().expect("model");
    let body = if model.session_account {
        "结束后对方会断开。这台电脑仍允许你的账号连接。"
    } else if model.session_long {
        "结束后对方会断开。长久码仍然有效，对方还可以再连。"
    } else {
        text::END_BODY
    };
    model.dialog = Some(Dialog {
        id,
        kind: "end".into(),
        title: "结束本次协助".into(),
        body: body.into(),
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
        {
            let mut model = state.model.lock().expect("model");
            // 没人连着时二次确认也落不了档，直接说明原因，避免确认后选项弹回像坏了。
            if model.phase != "host_session" {
                model.settings_hint = text::FULL_NEEDS_PEER.into();
                model.full_gen = 0;
                model.pending_full_trust = false;
                model.dialog = None;
                drop(model);
                state.publish();
                return Ok(state.snapshot());
            }
            let id = state.prompt_ids.fetch_add(1, Ordering::SeqCst);
            model.settings_hint.clear();
            model.pending_full_trust = false;
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
        }
        state.publish();
        return Ok(state.snapshot());
    }
    apply_pair_trust_choice(state.inner(), &trust);
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
    let mut file = settings::load(&state.settings_path);
    file.relay = relay.clone();
    settings::save(&state.settings_path, &file)?;
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
    // 有会话/输入框凭证时一并写入配置；没有也能复制「仅安装」配置（默认中继），由 Agent 再 connect。
    let payload = {
        let secrets = state.secrets.lock().expect("secrets");
        if !secrets.payload.is_empty() {
            secrets.payload.clone()
        } else if !secrets.mcp_payload.is_empty() {
            secrets.mcp_payload.clone()
        } else if let CredClass::Ok(_) = credential::classify(&draft) {
            draft.trim().to_string()
        } else {
            String::new()
        }
    };
    let mut model = state.model.lock().expect("model");
    let exe = find_mcp(&app);
    let placeholder = if cfg!(windows) {
        "RemoteAssist.Mcp.exe"
    } else {
        "RemoteAssist.Mcp"
    };
    let command = exe
        .as_ref()
        .map(|p| p.display().to_string())
        .unwrap_or_else(|| placeholder.into());
    let relay = model.relay.clone();
    let short = matches!(
        credential::classify(&payload),
        CredClass::Ok(Via::Relay(t)) if t.pake
    );
    let include_relay = payload.is_empty() || short;
    let body = text::mcp_share_text(
        &command,
        &payload,
        if include_relay { Some(relay.as_str()) } else { None },
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
    let status = open_folder(&dir);
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

fn open_folder(dir: &std::path::Path) -> std::io::Result<std::process::Child> {
    #[cfg(windows)]
    {
        std::process::Command::new("explorer").arg(dir).spawn()
    }
    #[cfg(not(windows))]
    {
        let opener = if cfg!(target_os = "macos") { "open" } else { "xdg-open" };
        std::process::Command::new(opener).arg(dir).spawn()
    }
}

fn open_url(url: &str) -> std::io::Result<std::process::Child> {
    #[cfg(windows)]
    {
        std::process::Command::new("cmd").args(["/c", "start", "", url]).spawn()
    }
    #[cfg(not(windows))]
    {
        let opener = if cfg!(target_os = "macos") { "open" } else { "xdg-open" };
        std::process::Command::new(opener).arg(url).spawn()
    }
}

#[tauri::command]
fn open_site() -> Result<(), String> {
    open_url(text::SITE).map_err(|_| "没能打开官网。".to_string())?;
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
fn ask_unattended(
    state: State<'_, std::sync::Arc<Shared>>,
    #[allow(unused_variables)] skip_write: bool,
) -> Result<Snapshot, String> {
    // 2026-11-12：长久码无人值守固定为「连上不弹允许 + 改动不再询问」，不再单独开关。
    state.long_skip.store(true, Ordering::SeqCst);
    let id = state.prompt_ids.fetch_add(1, Ordering::SeqCst);
    let mut model = state.model.lock().expect("model");
    model.dialog = Some(Dialog {
        id,
        kind: "unattended".into(),
        title: "开启无人值守？".into(),
        body: "持有长久码的人可以在你不在时连上，并且改动不再询问。改动仍会记在这台电脑上。请只把凭证放在你信任的设备上。确定开启吗？".into(),
        detail: String::new(),
        primary: "开启".into(),
        secondary: "取消".into(),
        seconds: text::CONFIRM_SECS,
    });
    drop(model);
    state.publish();
    Ok(state.snapshot())
}

#[tauri::command]
fn ask_revoke_unattended(state: State<'_, std::sync::Arc<Shared>>) -> Result<Snapshot, String> {
    let id = state.prompt_ids.fetch_add(1, Ordering::SeqCst);
    let mut model = state.model.lock().expect("model");
    model.dialog = Some(Dialog {
        id,
        kind: "unattended-revoke".into(),
        title: "撤销长久码？".into(),
        body: "撤销后这组长久码立刻失效，已经连上的人会断开。".into(),
        detail: String::new(),
        primary: "撤销".into(),
        secondary: "取消".into(),
        seconds: text::CONFIRM_SECS,
    });
    drop(model);
    state.publish();
    Ok(state.snapshot())
}

#[tauri::command]
fn copy_long_credential(app: AppHandle, state: State<'_, std::sync::Arc<Shared>>) -> Result<Snapshot, String> {
    let saved = settings::load(&state.settings_path);
    let mut model = state.model.lock().expect("model");
    if saved.unattended_code.len() < 4 || saved.unattended_password.len() < 8 {
        model.settings_hint = "长久码还没准备好。".into();
        drop(model);
        state.publish();
        return Ok(state.snapshot());
    }
    let payload = credential::short_credential(&saved.unattended_code, &saved.unattended_password);
    match copy_text(&app, &payload) {
        Ok(()) => model.settings_hint = "已复制长久码。请只发给你自己信任的人。".into(),
        Err(_) => {
            model.settings_hint = text::COPY_FAIL.into();
            model.manual_copy = payload;
        }
    }
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

fn mcp_binary_names() -> &'static [&'static str] {
    if cfg!(windows) {
        &["RemoteAssist.Mcp.exe", "remote_assist_mcp.exe"]
    } else {
        &["RemoteAssist.Mcp", "remote_assist_mcp"]
    }
}

fn find_mcp_in_dir(dir: &std::path::Path) -> Option<PathBuf> {
    for name in mcp_binary_names() {
        let direct = dir.join(name);
        if direct.exists() {
            return Some(direct);
        }
        let nested = dir.join("mcp").join(name);
        if nested.exists() {
            return Some(nested);
        }
    }
    let win_artifacts = dir
        .join("artifacts")
        .join("mcp-win-x64-sc")
        .join("RemoteAssist.Mcp.exe");
    if win_artifacts.exists() {
        return Some(win_artifacts);
    }
    None
}

fn find_mcp(app: &AppHandle) -> Option<PathBuf> {
    if let Ok(res) = app.path().resource_dir() {
        if let Some(p) = find_mcp_in_dir(&res) {
            return Some(p);
        }
    }
    let mut cursor = std::env::current_exe()
        .ok()
        .and_then(|p| p.parent().map(|d| d.to_path_buf()));
    for _ in 0..8 {
        let Some(dir) = cursor else { break };
        if let Some(p) = find_mcp_in_dir(&dir) {
            return Some(p);
        }
        cursor = dir.parent().map(|p| p.to_path_buf());
    }
    None
}

fn computer_name() -> String {
    std::env::var("COMPUTERNAME")
        .or_else(|_| std::env::var("HOSTNAME"))
        .unwrap_or_else(|_| "这台电脑".into())
}

fn identity_path() -> PathBuf {
    settings::base_dir().join("device-identity.json")
}

fn save_settings(state: &Shared, edit: impl FnOnce(&mut settings::Settings)) -> Result<settings::Settings, String> {
    let mut current = settings::load(&state.settings_path);
    edit(&mut current);
    settings::save(&state.settings_path, &current)?;
    Ok(current)
}

fn ensure_device(state: &Shared) -> Result<settings::Settings, String> {
    save_settings(state, |current| {
        if current.device_id.trim().is_empty() {
            current.device_id = wire::new_id();
        }
        if current.device_name.trim().is_empty() {
            current.device_name = computer_name();
        }
    })
}

fn remember_login(state: &std::sync::Arc<Shared>, username: &str, token: &str) -> Result<(), String> {
    let saved = save_settings(state, |current| {
        current.account_username = username.to_string();
        current.account_token = token.to_string();
        if current.device_id.trim().is_empty() {
            current.device_id = wire::new_id();
        }
        if current.device_name.trim().is_empty() {
            current.device_name = computer_name();
        }
    })?;
    state.secrets.lock().expect("secrets").account_token = token.to_string();
    {
        let mut model = state.model.lock().expect("model");
        model.account_user = saved.account_username.clone();
        model.device_name = saved.device_name.clone();
        model.account_allow = saved.account_allow;
        model.account_unattended = saved.account_unattended;
        model.account_skip = saved.account_skip_write;
        model.account_autostart = saved.account_autostart;
        model.account_error.clear();
    }
    state.account_allow.store(saved.account_allow, Ordering::SeqCst);
    state.account_skip.store(saved.account_skip_write, Ordering::SeqCst);
    if saved.account_allow {
        restart_account(state.clone());
    }
    pull_devices(state)?;
    state.publish();
    Ok(())
}

fn pull_devices(state: &Shared) -> Result<(), String> {
    let token = state.secrets.lock().expect("secrets").account_token.clone();
    let relay = state.model.lock().expect("model").relay.clone();
    if token.is_empty() {
        state.model.lock().expect("model").devices.clear();
        return Ok(());
    }
    match link::account_list(&relay, &token) {
        Ok(list) => {
            let mut model = state.model.lock().expect("model");
            model.devices = list
                .into_iter()
                .map(|d| DeviceItem {
                    id: d.id,
                    name: d.name,
                    online: d.online,
                    unattended: d.unattended,
                    fp: d.fp,
                })
                .collect();
            model.account_error.clear();
            Ok(())
        }
        Err(err) => {
            if err.contains("登录已失效") {
                state.secrets.lock().expect("secrets").account_token.clear();
                let _ = save_settings(state, |current| current.account_token.clear());
                let mut model = state.model.lock().expect("model");
                model.account_user.clear();
                model.devices.clear();
                model.account_online = false;
            }
            state.model.lock().expect("model").account_error = err.clone();
            Err(err)
        }
    }
}

fn spawn_account(shared: std::sync::Arc<Shared>) {
    let my = shared.account_gen.load(Ordering::SeqCst);
    shared.account_worker.store(true, Ordering::SeqCst);
    thread::spawn(move || {
        let hooks = AppHooks {
            shared: shared.clone(),
            my,
            long: false,
            account: true,
        };
        let identity = match tlsutil::load_or_create_identity(&identity_path()) {
            Ok(id) => id,
            Err(err) => {
                hooks.emit(Ev::Fail(err));
                if shared.account_gen.load(Ordering::SeqCst) == my {
                    shared.account_worker.store(false, Ordering::SeqCst);
                }
                return;
            }
        };
        let mut net_fails = 0u32;
        loop {
            if !hooks.alive() || !shared.account_allow.load(Ordering::SeqCst) {
                break;
            }
            let token = shared.secrets.lock().expect("secrets").account_token.clone();
            if token.is_empty() {
                break;
            }
            let saved = settings::load(&shared.settings_path);
            let relay = shared.model.lock().expect("model").relay.clone();
            let Some((host, port)) = credential::parse_endpoint(&relay) else {
                break;
            };
            let name = if saved.device_name.trim().is_empty() {
                computer_name()
            } else {
                saved.device_name.clone()
            };
            let tcp = match link::connect_endpoint(&host, port, &|| hooks.alive()) {
                Ok(stream) => stream,
                Err(()) => {
                    if !hooks.alive() {
                        break;
                    }
                    net_fails = net_fails.saturating_add(1);
                    if net_fails == 1 {
                        hooks.emit(Ev::Fail("没能连上服务器，允许连接会再试。".into()));
                    }
                    let delay = retry_delay(net_fails);
                    diag::note(
                        "account reconnect",
                        &format!("attempt={net_fails} wait_ms={}", delay.as_millis()),
                    );
                    sleep_while_alive(delay, &|| hooks.alive());
                    continue;
                }
            };
            match link::run_account_host(
                tcp,
                &identity,
                &saved.device_id,
                &name,
                &token,
                saved.account_unattended,
                &hooks,
            ) {
                link::HostEnd::Relisten { .. } => {
                    net_fails = 0;
                    if hooks.alive() && !hooks.can_take_session() {
                        sleep_while_alive(Duration::from_secs(3), &|| hooks.alive());
                    }
                }
                link::HostEnd::Fail => {
                    if hooks.alive() {
                        net_fails = net_fails.saturating_add(1);
                        let delay = retry_delay(net_fails);
                        diag::note(
                            "account retry",
                            &format!("attempt={net_fails} wait_ms={}", delay.as_millis()),
                        );
                        sleep_while_alive(delay, &|| hooks.alive());
                    }
                }
                link::HostEnd::Stop | link::HostEnd::Locked | link::HostEnd::PakeFail => break,
            }
        }
        if shared.account_gen.load(Ordering::SeqCst) == my {
            shared.account_worker.store(false, Ordering::SeqCst);
            shared.model.lock().expect("model").account_online = false;
            shared.publish();
        }
    });
}

fn restart_account(shared: std::sync::Arc<Shared>) {
    let _ = shared.account_gen.fetch_add(1, Ordering::SeqCst);
    let stream = shared.account_killer.lock().expect("account killer").take();
    if let Some(stream) = stream {
        thread::spawn(move || abort_io(stream));
    }
    spawn_account(shared);
}

fn stop_account_listener(state: &std::sync::Arc<Shared>) {
    state.account_allow.store(false, Ordering::SeqCst);
    let _ = state.account_gen.fetch_add(1, Ordering::SeqCst);
    let stream = state.account_killer.lock().expect("account killer").take();
    if let Some(stream) = stream {
        thread::spawn(move || abort_io(stream));
    }
    state.model.lock().expect("model").account_online = false;
}

fn enable_account_allow(state: &std::sync::Arc<Shared>) -> Result<(), String> {
    let saved = ensure_device(state)?;
    let _ = save_settings(state, |current| current.account_allow = true)?;
    state.account_allow.store(true, Ordering::SeqCst);
    {
        let mut model = state.model.lock().expect("model");
        model.account_allow = true;
        model.device_name = saved.device_name;
        model.account_error.clear();
        model.settings_hint = "已允许这个账号连接这台电脑。".into();
    }
    restart_account(state.clone());
    state.publish();
    Ok(())
}

fn disable_account_allow(state: &std::sync::Arc<Shared>) -> Result<(), String> {
    let _ = save_settings(state, |current| current.account_allow = false)?;
    stop_account_listener(state);
    state.model.lock().expect("model").account_allow = false;
    state.publish();
    Ok(())
}

fn enable_account_unattended(state: &std::sync::Arc<Shared>) -> Result<(), String> {
    let skip = state.account_skip.load(Ordering::SeqCst);
    let saved = save_settings(state, |current| {
        current.account_unattended = true;
        current.account_skip_write = skip;
    })?;
    if sync_device_unattended(state, &saved, true).is_err() && state.account_allow.load(Ordering::SeqCst) {
        restart_account(state.clone());
    }
    let mut model = state.model.lock().expect("model");
    model.account_unattended = true;
    model.account_skip = skip;
    if let Some(row) = model.devices.iter_mut().find(|d| d.id == saved.device_id) {
        row.unattended = true;
    }
    model.settings_hint = if skip {
        "无人值守已开启。人不在时，改动不再询问，仍会记下。".into()
    } else {
        "无人值守已开启。人不在时，改动仍会等这台电脑点允许。".into()
    };
    drop(model);
    state.publish();
    Ok(())
}

fn stop_account_unattended_now(state: &std::sync::Arc<Shared>) -> Result<(), String> {
    let saved = save_settings(state, |current| {
        current.account_unattended = false;
        current.account_skip_write = false;
        current.account_autostart = false;
    })?;
    let _ = sync_device_unattended(state, &saved, false);
    state.account_skip.store(false, Ordering::SeqCst);
    set_windows_run(false);
    let mut model = state.model.lock().expect("model");
    model.account_unattended = false;
    model.account_skip = false;
    model.account_autostart = false;
    if let Some(row) = model.devices.iter_mut().find(|d| d.id == saved.device_id) {
        row.unattended = false;
    }
    model.settings_hint = "已关闭无人值守。账号连接还在，改动仍会询问。".into();
    drop(model);
    state.publish();
    Ok(())
}

fn sync_device_unattended(state: &Shared, saved: &settings::Settings, unattended: bool) -> Result<(), String> {
    let token = state.secrets.lock().expect("secrets").account_token.clone();
    let relay = state.model.lock().expect("model").relay.clone();
    if token.is_empty() || saved.device_id.trim().is_empty() {
        return Ok(());
    }
    link::account_set_unattended(&relay, &token, &saved.device_id, unattended)
}

fn remove_account_device(state: &std::sync::Arc<Shared>) -> Result<(), String> {
    let token = state.secrets.lock().expect("secrets").account_token.clone();
    let relay = state.model.lock().expect("model").relay.clone();
    let device_id = settings::load(&state.settings_path).device_id;
    if !token.is_empty() && !device_id.is_empty() {
        link::account_delete(&relay, &token, &device_id)?;
    }
    let _ = save_settings(state, |current| {
        current.account_allow = false;
        current.account_unattended = false;
        current.account_skip_write = false;
        current.account_autostart = false;
    })?;
    state.account_skip.store(false, Ordering::SeqCst);
    set_windows_run(false);
    stop_account_listener(state);
    let mut model = state.model.lock().expect("model");
    model.account_allow = false;
    model.account_unattended = false;
    model.account_skip = false;
    model.account_autostart = false;
    model.devices.retain(|d| d.id != device_id);
    model.left_note = "这台电脑已从账号移除。".into();
    model.settings_hint = "这台电脑已从账号移除。".into();
    drop(model);
    state.publish();
    Ok(())
}

fn begin_account_helper(state: &std::sync::Arc<Shared>, device_id: &str) -> Result<(), String> {
    let device_id = device_id.to_string();
    let (fp, online) = {
        let model = state.model.lock().expect("model");
        match model.devices.iter().find(|d| d.id == device_id) {
            Some(d) => (d.fp.clone(), d.online),
            None => return Err("列表里没有这台电脑。".into()),
        }
    };
    if !online {
        return Err("这台电脑没开着鹊桥，现在连不上。".into());
    }
    if !credential::is_sha256_hex(&credential::normalize_fp(&fp)) {
        return Err("这台电脑还没准备好加密连接。".into());
    }
    let token = state.secrets.lock().expect("secrets").account_token.clone();
    if token.is_empty() {
        return Err("请先登录。".into());
    }
    let relay = state.model.lock().expect("model").relay.clone();
    if state.worker.load(Ordering::SeqCst) && !stop_worker(state) {
        return Err(text::STOP_FAIL.into());
    }
    {
        let mut model = state.model.lock().expect("model");
        model.phase = "connecting".into();
        model.view = "home".into();
        model.helper_status = "正在连接你的电脑…".into();
        model.helper_error.clear();
        model.dialog = None;
    }
    let my = state.bump();
    state.publish();
    let (tx, rx) = mpsc::channel();
    *state.jobs.lock().expect("jobs") = Some(tx);
    state.worker.store(true, Ordering::SeqCst);
    let shared = state.clone();
    thread::spawn(move || {
        let hooks = AppHooks {
            shared: shared.clone(),
            my,
            long: false,
            account: false,
        };
        link::run_account_helper(&relay, &token, &device_id, &fp, &rx, &hooks);
        shared.worker.store(false, Ordering::SeqCst);
        if shared.gen.load(Ordering::SeqCst) == my {
            shared.publish();
        }
    });
    Ok(())
}

fn set_windows_run(on: bool) {
    #[cfg(windows)]
    {
        let key = r"HKCU\Software\Microsoft\Windows\CurrentVersion\Run";
        if on {
            if let Ok(exe) = std::env::current_exe() {
                // 带空格的路径必须加引号，否则登录后 Run 项起不来；--minimized 让它只待在托盘。
                let command = format!("\"{}\" --minimized", exe.to_string_lossy());
                let _ = std::process::Command::new("reg")
                    .args(["add", key, "/v", "Queqiao", "/t", "REG_SZ", "/d", &command, "/f"])
                    .status();
            }
        } else {
            let _ = std::process::Command::new("reg")
                .args(["delete", key, "/v", "Queqiao", "/f"])
                .status();
        }
    }
    #[cfg(not(windows))]
    {
        let _ = on;
    }
}

/// 无人值守必须能在重启后自己回来：自启跟着「长久码开着 或 账号允许连接」走。
fn sync_autostart(state: &Shared) {
    let long_on = state.model.lock().expect("model").long_on;
    let account_on = settings::load(&state.settings_path).account_autostart;
    set_windows_run(long_on || account_on);
}

fn note_account_error(state: &Shared, err: String) -> Snapshot {
    state.model.lock().expect("model").account_error = err;
    state.publish();
    state.snapshot()
}

#[tauri::command]
fn account_signup(state: State<'_, std::sync::Arc<Shared>>, username: String, password: String, confirm: String) -> Result<Snapshot, String> {
    let username = account::username(&username).map_err(|e| e.to_string())?;
    account::password(&password).map_err(|e| e.to_string())?;
    if password != confirm {
        return Ok(note_account_error(&state, "两次密码不一致。".into()));
    }
    let relay = state.model.lock().expect("model").relay.clone();
    match link::account_signup(&relay, &username, &password) {
        Ok(token) => {
            remember_login(state.inner(), &username, &token).map_err(|e| e)?;
            Ok(state.snapshot())
        }
        Err(err) => Ok(note_account_error(&state, err)),
    }
}

#[tauri::command]
fn account_login(state: State<'_, std::sync::Arc<Shared>>, username: String, password: String) -> Result<Snapshot, String> {
    let username = account::username(&username).map_err(|e| e.to_string())?;
    account::password(&password).map_err(|e| e.to_string())?;
    let relay = state.model.lock().expect("model").relay.clone();
    match link::account_login(&relay, &username, &password) {
        Ok(token) => {
            remember_login(state.inner(), &username, &token)?;
            Ok(state.snapshot())
        }
        Err(err) => Ok(note_account_error(&state, err)),
    }
}

#[tauri::command]
fn account_logout(state: State<'_, std::sync::Arc<Shared>>) -> Result<Snapshot, String> {
    let token = state.secrets.lock().expect("secrets").account_token.clone();
    let relay = state.model.lock().expect("model").relay.clone();
    if !token.is_empty() {
        let _ = link::account_logout(&relay, &token);
    }
    stop_account_listener(state.inner());
    state.secrets.lock().expect("secrets").account_token.clear();
    let _ = save_settings(&state, |current| {
        current.account_token.clear();
        current.account_username.clear();
    });
    let mut model = state.model.lock().expect("model");
    model.account_user.clear();
    model.devices.clear();
    model.account_error.clear();
    model.account_online = false;
    drop(model);
    state.publish();
    Ok(state.snapshot())
}

#[tauri::command]
fn refresh_devices(state: State<'_, std::sync::Arc<Shared>>) -> Result<Snapshot, String> {
    let _ = pull_devices(&state);
    state.publish();
    Ok(state.snapshot())
}

#[tauri::command]
fn connect_account(state: State<'_, std::sync::Arc<Shared>>, device_id: String) -> Result<Snapshot, String> {
    let phase = state.model.lock().expect("model").phase.clone();
    if matches!(phase.as_str(), "opening" | "waiting" | "host_session" | "stopping" | "peer_ended") {
        state.secrets.lock().expect("secrets").pending_device = device_id;
        open_switch(&state, "to-account");
        return Ok(state.snapshot());
    }
    if matches!(phase.as_str(), "connecting" | "wait_allow" | "helper_session") {
        return Ok(state.snapshot());
    }
    begin_account_helper(state.inner(), &device_id).map_err(|err| {
        state.model.lock().expect("model").helper_error = err.clone();
        state.publish();
        err
    })?;
    Ok(state.snapshot())
}

#[tauri::command]
fn ask_account_allow(state: State<'_, std::sync::Arc<Shared>>) -> Result<Snapshot, String> {
    if state.model.lock().expect("model").account_user.is_empty() {
        return Ok(note_account_error(&state, "请先登录。".into()));
    }
    if state.model.lock().expect("model").account_allow {
        disable_account_allow(state.inner())?;
        return Ok(state.snapshot());
    }
    let id = state.prompt_ids.fetch_add(1, Ordering::SeqCst);
    state.model.lock().expect("model").dialog = Some(Dialog {
        id,
        kind: "account-allow".into(),
        title: "允许这个账号连接这台电脑？".into(),
        body: "同一账号可以不输入会话码连上这台电脑。改动前仍会问你。".into(),
        detail: String::new(),
        primary: "允许".into(),
        secondary: "取消".into(),
        seconds: text::CONFIRM_SECS,
    });
    state.publish();
    Ok(state.snapshot())
}

#[tauri::command]
fn set_device_name(state: State<'_, std::sync::Arc<Shared>>, name: String) -> Result<Snapshot, String> {
    let name = name.trim().chars().take(32).collect::<String>();
    if name.is_empty() {
        return Ok(note_account_error(&state, "请给这台电脑起个名字。".into()));
    }
    let _ = save_settings(&state, |current| current.device_name = name.clone())?;
    state.model.lock().expect("model").device_name = name;
    if state.account_allow.load(Ordering::SeqCst) {
        restart_account((*state).clone());
    }
    state.publish();
    Ok(state.snapshot())
}

#[tauri::command]
fn ask_account_remove(state: State<'_, std::sync::Arc<Shared>>) -> Result<Snapshot, String> {
    let id = state.prompt_ids.fetch_add(1, Ordering::SeqCst);
    state.model.lock().expect("model").dialog = Some(Dialog {
        id,
        kind: "account-remove".into(),
        title: "从账号移除这台电脑？".into(),
        body: "移除后，其他电脑的列表里不再有它。已经连上的人会断开。".into(),
        detail: String::new(),
        primary: "移除".into(),
        secondary: "取消".into(),
        seconds: text::CONFIRM_SECS,
    });
    state.publish();
    Ok(state.snapshot())
}

#[tauri::command]
fn ask_account_unattended(state: State<'_, std::sync::Arc<Shared>>, skip_write: bool) -> Result<Snapshot, String> {
    if !state.model.lock().expect("model").account_allow {
        return Ok(note_account_error(&state, "请先允许这个账号连接这台电脑。".into()));
    }
    state.account_skip.store(skip_write, Ordering::SeqCst);
    let id = state.prompt_ids.fetch_add(1, Ordering::SeqCst);
    let body = if skip_write {
        "人不在旁边时，这个账号也能连上，并且改动不再询问。改动仍会记在这台电脑上。"
    } else {
        "人不在旁边时，这个账号也能连上。要改电脑时仍会等这台电脑点允许。"
    };
    state.model.lock().expect("model").dialog = Some(Dialog {
        id,
        kind: "account-unattended".into(),
        title: "开启无人值守？".into(),
        body: body.into(),
        detail: String::new(),
        primary: "开启".into(),
        secondary: "取消".into(),
        seconds: text::CONFIRM_SECS,
    });
    state.publish();
    Ok(state.snapshot())
}

#[tauri::command]
fn set_account_autostart(state: State<'_, std::sync::Arc<Shared>>, on: bool) -> Result<Snapshot, String> {
    if !cfg!(windows) {
        return Ok(note_account_error(&state, "这个平台暂不支持开机自动等待。".into()));
    }
    if on && !state.model.lock().expect("model").account_unattended {
        return Ok(note_account_error(&state, "请先开启无人值守。".into()));
    }
    let _ = save_settings(&state, |current| current.account_autostart = on)?;
    state.model.lock().expect("model").account_autostart = on;
    sync_autostart(state.inner());
    state.model.lock().expect("model").settings_hint = if on {
        "登录 Windows 后会自动打开鹊桥并等待连接。".into()
    } else {
        "已关闭开机自动等待。".into()
    };
    state.publish();
    Ok(state.snapshot())
}

#[tauri::command]
fn stop_account_unattended(state: State<'_, std::sync::Arc<Shared>>) -> Result<Snapshot, String> {
    stop_account_unattended_now(state.inner())?;
    Ok(state.snapshot())
}

fn set_update_busy(state: &Shared, busy: bool, hint: impl Into<String>) {
    state.update_busy.store(busy, Ordering::SeqCst);
    let mut model = state.model.lock().expect("model");
    model.update_busy = busy;
    model.update_hint = hint.into();
    drop(model);
    state.publish();
}

fn remember_available(state: &Shared, avail: &update::Available) {
    *state.pending_update.lock().expect("pending update") = Some(avail.clone());
    let mut model = state.model.lock().expect("model");
    model.update_available = true;
    model.update_version = avail.version.clone();
    if model.update_hint.is_empty() || !model.update_busy {
        model.update_hint = format!("有新版本 {} 可更新。", avail.version);
    }
}

fn show_update_dialog(state: &std::sync::Arc<Shared>, avail: &update::Available) {
    if state.model.lock().expect("model").dialog.is_some() {
        return;
    }
    let id = state.prompt_ids.fetch_add(1, Ordering::SeqCst);
    let notes = if avail.notes.is_empty() {
        String::new()
    } else {
        avail.notes.clone()
    };
    let body = if notes.is_empty() {
        format!(
            "当前版本 {}，最新版本 {}。更新会下载安装包并打开，完成后请按安装向导操作。",
            update::current_version(),
            avail.version
        )
    } else {
        format!(
            "当前版本 {}，最新版本 {}。\n\n{}",
            update::current_version(),
            avail.version,
            notes
        )
    };
    state.model.lock().expect("model").dialog = Some(Dialog {
        id,
        kind: "update".into(),
        title: "发现新版本".into(),
        body,
        detail: String::new(),
        primary: "立即更新".into(),
        secondary: "稍后".into(),
        seconds: 0,
    });
    state.publish();
}

fn begin_apply_update(state: &std::sync::Arc<Shared>) {
    if state.update_busy.swap(true, Ordering::SeqCst) {
        return;
    }
    let Some(avail) = state.pending_update.lock().expect("pending update").clone() else {
        state.update_busy.store(false, Ordering::SeqCst);
        set_update_busy(state, false, "没有可安装的更新。");
        return;
    };
    set_update_busy(
        state,
        true,
        format!("正在下载 {}…", avail.version),
    );
    let shared = state.clone();
    thread::spawn(move || {
        match update::download_to_temp(&avail) {
            Ok(path) => {
                set_update_busy(&shared, true, "正在打开安装程序…");
                match update::launch_installer(&path) {
                    Ok(()) => {
                        if let Some(app) = shared.app.lock().expect("app").clone() {
                            // 稍等安装器起来再退出，避免立刻被关。
                            thread::sleep(Duration::from_millis(600));
                            app.exit(0);
                        }
                    }
                    Err(err) => {
                        set_update_busy(&shared, false, err);
                    }
                }
            }
            Err(err) => set_update_busy(&shared, false, err),
        }
    });
}

fn run_update_check(state: &std::sync::Arc<Shared>, interactive: bool, auto_apply: bool) {
    if state.update_busy.load(Ordering::SeqCst) {
        return;
    }
    if interactive {
        set_update_busy(state, false, "正在检查更新…");
    }
    let shared = state.clone();
    thread::spawn(move || match update::check() {
        Ok(None) => {
            let mut model = shared.model.lock().expect("model");
            model.update_available = false;
            model.update_version.clear();
            model.update_busy = false;
            if interactive {
                model.update_hint = format!("已是最新版本（{}）。", update::current_version());
            }
            drop(model);
            *shared.pending_update.lock().expect("pending update") = None;
            shared.publish();
        }
        Ok(Some(avail)) => {
            remember_available(&shared, &avail);
            shared.model.lock().expect("model").update_busy = false;
            shared.publish();
            let idle = {
                let model = shared.model.lock().expect("model");
                !is_active(&model.phase) && model.dialog.is_none()
            };
            let want_auto = auto_apply && shared.model.lock().expect("model").auto_update;
            if want_auto && idle {
                begin_apply_update(&shared);
            } else {
                // 协助中也提醒，但不强行下载；已有其它确认框时 show_update_dialog 会跳过。
                show_update_dialog(&shared, &avail);
            }
        }
        Err(err) => {
            if interactive {
                set_update_busy(&shared, false, err);
            } else {
                shared.model.lock().expect("model").update_busy = false;
                shared.publish();
            }
        }
    });
}

#[tauri::command]
fn check_update(state: State<'_, std::sync::Arc<Shared>>) -> Result<Snapshot, String> {
    run_update_check(state.inner(), true, false);
    Ok(state.snapshot())
}

#[tauri::command]
fn apply_update(state: State<'_, std::sync::Arc<Shared>>) -> Result<Snapshot, String> {
    if state.pending_update.lock().expect("pending update").is_none() {
        run_update_check(state.inner(), true, true);
        return Ok(state.snapshot());
    }
    begin_apply_update(state.inner());
    Ok(state.snapshot())
}

#[tauri::command]
fn set_auto_update(state: State<'_, std::sync::Arc<Shared>>, on: bool) -> Result<Snapshot, String> {
    let _ = save_settings(&state, |current| current.auto_update = on)?;
    let mut model = state.model.lock().expect("model");
    model.auto_update = on;
    model.update_hint = if on {
        "已开启：有新版本时会自动下载并打开安装包。".into()
    } else {
        "已关闭自动更新。仍可手动检查。".into()
    };
    drop(model);
    state.publish();
    Ok(state.snapshot())
}

pub fn run() {
    diag::init();
    let shared = std::sync::Arc::new(Shared::new());
    tauri::Builder::default()
        .manage(shared)
        .setup(|app| {
            let shared = app.state::<std::sync::Arc<Shared>>();
            *shared.app.lock().expect("app") = Some(app.handle().clone());
            let saved = settings::load(&shared.settings_path);
            if !saved.unattended_code.is_empty() && saved.unattended_password.len() >= 8 {
                spawn_long((*shared).clone());
                // 老版本开过无人值守但没登记自启的，这次启动补上。
                sync_autostart(shared.inner());
            }
            if std::env::args().any(|a| a == "--minimized") {
                if let Some(win) = app.get_webview_window("main") {
                    let _ = win.hide();
                }
            }
            // 2026-11-12：账号/登录产品路径取消，界面已隐藏；启动时不再拉起账号上线。
            let boot = (*shared).clone();
            thread::spawn(move || {
                thread::sleep(Duration::from_secs(2));
                run_update_check(&boot, false, true);
            });

            let show_item = MenuItem::with_id(app, "tray-show", "打开鹊桥", true, None::<&str>)?;
            let quit_item = MenuItem::with_id(app, "tray-quit", "退出鹊桥", true, None::<&str>)?;
            let tray_menu = Menu::with_items(app, &[&show_item, &quit_item])?;
            if let Some(tray) = app.tray_by_id("main") {
                tray.set_menu(Some(tray_menu))?;
                let _ = tray.set_show_menu_on_left_click(false);
            }
            Ok(())
        })
        .on_menu_event(|app, event| match event.id().as_ref() {
            "tray-show" => show_main(app),
            "tray-quit" => {
                let shared = app.state::<std::sync::Arc<Shared>>();
                request_quit(shared.inner().as_ref());
            }
            _ => {}
        })
        .on_tray_icon_event(|app, event| {
            if let TrayIconEvent::Click {
                button: MouseButton::Left,
                button_state: MouseButtonState::Up,
                ..
            } = event
            {
                show_main(app);
            }
        })
        .on_window_event(|window, event| {
            if let WindowEvent::CloseRequested { api, .. } = event {
                // 关窗进托盘常驻；真正退出只走托盘「退出鹊桥」。
                api.prevent_close();
                let _ = window.hide();
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
            show_settings,
            ask_unattended,
            ask_revoke_unattended,
            copy_long_credential,
            account_signup,
            account_login,
            account_logout,
            refresh_devices,
            connect_account,
            ask_account_allow,
            set_device_name,
            ask_account_remove,
            ask_account_unattended,
            set_account_autostart,
            stop_account_unattended,
            check_update,
            apply_update,
            set_auto_update
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}

#[cfg(test)]
mod tests {
    use super::{retry_delay, sleep_while_alive};
    use std::time::{Duration, Instant};

    #[test]
    fn retry_backs_off_and_caps_at_one_minute() {
        for fails in 1..=9u32 {
            let base = 3u64.saturating_mul(1u64 << fails.min(5)).min(60);
            let delay = retry_delay(fails);
            assert!(delay >= Duration::from_secs(base));
            assert!(delay <= Duration::from_secs(base) + Duration::from_millis(base * 200));
        }
        assert!(retry_delay(1) < retry_delay(4));
        assert!(retry_delay(9) <= Duration::from_secs(72));
    }

    #[test]
    fn backoff_sleep_waits_but_stops_when_generation_dies() {
        let start = Instant::now();
        sleep_while_alive(Duration::from_millis(300), &|| true);
        assert!(start.elapsed() >= Duration::from_millis(280));
        let start = Instant::now();
        sleep_while_alive(Duration::from_secs(60), &|| false);
        assert!(start.elapsed() < Duration::from_secs(1));
    }
}
