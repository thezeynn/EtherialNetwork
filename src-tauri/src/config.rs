use serde::{Deserialize, Serialize};
use std::fs;
use std::path::PathBuf;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct LauncherConfig {
    pub username: String,
    pub min_ram_mb: u32,
    pub max_ram_mb: u32,
    pub server_host: String,
    pub server_port: u16,
    pub fullscreen: bool,
    pub resolution_width: u32,
    pub resolution_height: u32,
    pub custom_java_path: Option<String>,
    pub use_bundled_java: bool,
    pub auto_connect_method: String, // "direct_cli" or "java_agent"
}

impl Default for LauncherConfig {
    fn default() -> Self {
        Self {
            username: "Player".to_string(),
            min_ram_mb: 2048,
            max_ram_mb: 4096,
            server_host: "localhost".to_string(),
            server_port: 25565,
            fullscreen: false,
            resolution_width: 1280,
            resolution_height: 720,
            custom_java_path: None,
            use_bundled_java: true,
            auto_connect_method: "direct_cli".to_string(),
        }
    }
}

pub fn get_etherial_dir() -> PathBuf {
    let app_data = dirs::data_dir().unwrap_or_else(|| PathBuf::from("."));
    let dir = app_data.join(".etherial");
    if !dir.exists() {
        let _ = fs::create_dir_all(&dir);
    }
    dir
}

pub fn get_config_path() -> PathBuf {
    get_etherial_dir().join("launcher_config.json")
}

pub fn load_config() -> LauncherConfig {
    let path = get_config_path();
    if path.exists() {
        if let Ok(content) = fs::read_to_string(&path) {
            if let Ok(cfg) = serde_json::from_str::<LauncherConfig>(&content) {
                return cfg;
            }
        }
    }
    let default_cfg = LauncherConfig::default();
    let _ = save_config(&default_cfg);
    default_cfg
}

pub fn save_config(config: &LauncherConfig) -> Result<(), String> {
    let path = get_config_path();
    let json = serde_json::to_string_pretty(config).map_err(|e| e.to_string())?;
    fs::write(&path, json).map_err(|e| e.to_string())?;
    Ok(())
}
