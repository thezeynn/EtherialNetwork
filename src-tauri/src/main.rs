// Prevents additional console window on Windows in release, DO NOT REMOVE!!
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

mod config;
mod commands;
mod java_manager;
mod launcher;
mod mojang;
mod server_ping;

use commands::*;

fn main() {
    tauri::Builder::default()
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_fs::init())
        .plugin(tauri_plugin_os::init())
        .invoke_handler(tauri::generate_handler![
            get_launcher_config,
            save_launcher_config,
            ping_server,
            get_system_specs,
            start_game
        ])
        .run(tauri::generate_context!())
        .expect("Etherial Launcher başlatılırken kritik hata oluştu!");
}
