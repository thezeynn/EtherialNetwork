use crate::config::{get_etherial_dir, load_config, save_config, LauncherConfig};
use crate::java_manager::JavaManager;
use crate::launcher::process::{launch_game, LaunchOptions};
use crate::mojang::downloader::IntegrityEngine;
use crate::server_ping::{ping_minecraft_server, ServerStatusResponse};
use serde::{Deserialize, Serialize};
use sysinfo::System;
use tauri::command;

#[derive(Debug, Serialize, Deserialize)]
pub struct SystemSpecs {
    pub total_ram_mb: u64,
    pub available_ram_mb: u64,
    pub cpu_cores: usize,
    pub os_name: String,
}

#[command]
pub fn get_launcher_config() -> LauncherConfig {
    load_config()
}

#[command]
pub fn save_launcher_config(config: LauncherConfig) -> Result<(), String> {
    save_config(&config)
}

#[command]
pub fn ping_server(host: String, port: u16) -> ServerStatusResponse {
    ping_minecraft_server(&host, port)
}

#[command]
pub fn get_system_specs() -> SystemSpecs {
    let mut sys = System::new_all();
    sys.refresh_all();

    let total_ram = sys.total_memory() / (1024 * 1024);
    let available_ram = sys.available_memory() / (1024 * 1024);
    let cpu_cores = sys.cpus().len();

    SystemSpecs {
        total_ram_mb: total_ram,
        available_ram_mb: available_ram,
        cpu_cores,
        os_name: std::env::consts::OS.to_string(),
    }
}

#[command]
pub async fn start_game<R: tauri::Runtime>(app: tauri::AppHandle<R>) -> Result<String, String> {
    let config = load_config();
    let game_dir = get_etherial_dir().join("game");

    if !game_dir.exists() {
        let _ = std::fs::create_dir_all(&game_dir);
    }

    // 1. Resolve or Download Java 8
    let java_mgr = JavaManager::new(get_etherial_dir());
    let java_binary = if config.use_bundled_java {
        java_mgr.ensure_zulu_8(&app).await?
    } else {
        match java_mgr.get_java_executable(config.custom_java_path.as_deref()) {
            Ok(p) => p,
            Err(_) => java_mgr.ensure_zulu_8(&app).await?,
        }
    };

    // 2. Sync and verify Assets, Libraries, Natives, Client JAR
    let engine = IntegrityEngine::new(game_dir.clone());
    let classpath_jars = engine.sync_all(&app).await?;

    // 3. Locate Companion JavaAgent (if enabled)
    let agent_jar = {
        let local_agent = get_etherial_dir().join("etherial-agent.jar");
        if local_agent.exists() {
            Some(local_agent)
        } else {
            let relative_agent = std::path::PathBuf::from("agent/target/etherial-agent-1.0.0.jar");
            if relative_agent.exists() {
                Some(relative_agent)
            } else {
                None
            }
        }
    };

    // 4. Launch Process
    let options = LaunchOptions {
        java_binary,
        game_dir,
        classpath_jars,
        agent_jar,
        config,
    };

    let mut child = launch_game(&options)?;
    let pid = child.id();

    // Spawn a watcher task for the process
    tokio::spawn(async move {
        let status = child.wait();
        println!("[Etherial Process] Minecraft process exited with: {:?}", status);
    });

    Ok(format!("Minecraft başarıyla başlatıldı (PID: {})", pid))
}
