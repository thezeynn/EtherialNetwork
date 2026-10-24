use std::fs::{self, File};
use std::path::{Path, PathBuf};
use std::process::Command;
use tauri::Emitter;

pub const AZUL_ZULU_8_URL: &str =
    "https://cdn.azul.com/zulu/bin/zulu8.78.0.19-ca-jre8.0.412-win_x64.zip";

pub struct JavaManager {
    base_dir: PathBuf,
}

impl JavaManager {
    pub fn new(base_dir: PathBuf) -> Self {
        Self { base_dir }
    }

    pub fn get_runtime_dir(&self) -> PathBuf {
        self.base_dir.join("runtime").join("zulu8")
    }

    pub fn get_java_executable(&self, custom_path: Option<&str>) -> Result<PathBuf, String> {
        // 1. Check custom user-specified path
        if let Some(path) = custom_path {
            let p = PathBuf::from(path);
            if p.exists() && self.validate_java_binary(&p) {
                return Ok(p);
            }
        }

        // 2. Check bundled/managed Zulu JRE
        let javaw = self.get_runtime_dir().join("bin").join("javaw.exe");
        if javaw.exists() && self.validate_java_binary(&javaw) {
            return Ok(javaw);
        }

        // Check java.exe as fallback in runtime dir
        let java_exe = self.get_runtime_dir().join("bin").join("java.exe");
        if java_exe.exists() && self.validate_java_binary(&java_exe) {
            return Ok(java_exe);
        }

        // 3. Check system PATH or JAVA_HOME if available
        if let Ok(path) = which_java() {
            if self.validate_java_binary(&path) {
                return Ok(path);
            }
        }

        Err("Uygun 64-bit Java 8 bulunamadı! Otomatik indirme başlatılmalı.".to_string())
    }

    pub fn validate_java_binary(&self, path: &Path) -> bool {
        let output = Command::new(path)
            .arg("-version")
            .output();

        match output {
            Ok(out) => {
                let stderr = String::from_utf8_lossy(&out.stderr);
                let stdout = String::from_utf8_lossy(&out.stdout);
                let combined = format!("{}\n{}", stdout, stderr);

                // Verify it is Java 8 (1.8) and 64-Bit
                let is_java_8 = combined.contains("1.8.0") || combined.contains("build 8.");
                let is_64_bit = combined.contains("64-Bit") || combined.contains("x86_64") || combined.contains("amd64");

                is_java_8 && is_64_bit
            }
            Err(_) => false,
        }
    }

    pub async fn ensure_zulu_8<R: tauri::Runtime>(
        &self,
        app: &tauri::AppHandle<R>,
    ) -> Result<PathBuf, String> {
        let javaw = self.get_runtime_dir().join("bin").join("javaw.exe");
        if javaw.exists() && self.validate_java_binary(&javaw) {
            return Ok(javaw);
        }

        // Emit downloading Java event
        let _ = app.emit(
            "download_progress",
            crate::mojang::downloader::DownloadProgressPayload {
                step: "Optimize Azul Zulu 8 JRE İndiriliyor...".to_string(),
                current_file: "zulu8-win_x64.zip".to_string(),
                completed_items: 0,
                total_items: 1,
                downloaded_bytes: 0,
                total_bytes: 50_000_000,
                speed_mb_s: 0.0,
                percentage: 0.0,
            },
        );

        let runtime_dir = self.get_runtime_dir();
        fs::create_dir_all(&runtime_dir).map_err(|e| e.to_string())?;

        let temp_zip = self.base_dir.join("zulu8_download.zip");
        let client = reqwest::Client::new();
        let resp = client
            .get(AZUL_ZULU_8_URL)
            .send()
            .await
            .map_err(|e| format!("Azul CDN bağlantı hatası: {}", e))?;

        if !resp.status().is_success() {
            return Err(format!("Azul Zulu indirilemedi. HTTP Status: {}", resp.status()));
        }

        let bytes = resp.bytes().await.map_err(|e| e.to_string())?;
        fs::write(&temp_zip, &bytes).map_err(|e| e.to_string())?;

        // Extract zip
        let zip_file = File::open(&temp_zip).map_err(|e| e.to_string())?;
        let mut archive = zip::ZipArchive::new(zip_file).map_err(|e| e.to_string())?;

        // Zulu zip contains a root folder like zulu8.78.0.19-ca-jre8.0.412-win_x64/
        // We strip this prefix and extract directly into runtime_dir
        for i in 0..archive.len() {
            let mut file = archive.by_index(i).map_err(|e| e.to_string())?;
            let enclosed_name = match file.enclosed_name() {
                Some(p) => p.to_owned(),
                None => continue,
            };

            let mut components = enclosed_name.components();
            components.next(); // Skip root folder
            let stripped_path: PathBuf = components.as_path().to_path_buf();

            if stripped_path.as_os_str().is_empty() {
                continue;
            }

            let out_path = runtime_dir.join(&stripped_path);

            if file.is_dir() {
                fs::create_dir_all(&out_path).map_err(|e| e.to_string())?;
            } else {
                if let Some(p) = out_path.parent() {
                    if !p.exists() {
                        fs::create_dir_all(p).map_err(|e| e.to_string())?;
                    }
                }
                let mut outfile = File::create(&out_path).map_err(|e| e.to_string())?;
                std::io::copy(&mut file, &mut outfile).map_err(|e| e.to_string())?;
            }
        }

        let _ = fs::remove_file(&temp_zip);

        if javaw.exists() {
            Ok(javaw)
        } else {
            let java_exe = runtime_dir.join("bin").join("java.exe");
            if java_exe.exists() {
                Ok(java_exe)
            } else {
                Err("Java çıkartıldı fakat çalıştırılabilir binary bulunamadı!".to_string())
            }
        }
    }
}

fn which_java() -> Result<PathBuf, String> {
    if let Ok(java_home) = std::env::var("JAVA_HOME") {
        let p = PathBuf::from(java_home).join("bin").join("javaw.exe");
        if p.exists() {
            return Ok(p);
        }
    }

    let default_system = PathBuf::from("C:\\Program Files\\Java\\jre1.8.0_503\\bin\\javaw.exe");
    if default_system.exists() {
        return Ok(default_system);
    }

    Err("System Java not found".to_string())
}
