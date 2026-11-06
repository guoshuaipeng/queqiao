//! 鹊桥 Tauri 壳 · D0
//! 本阶段仅窗口空壳；不接 relay、不解析 pair、不截屏/注入。

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
