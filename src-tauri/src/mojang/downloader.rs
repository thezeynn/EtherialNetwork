use crate::mojang::manifest::{AssetIndex, Library, VersionManifest};
use futures_util::StreamExt;
use sha1::{Digest, Sha1};
use std::fs::{self, File};
use std::io::{Read, Write};
use std::path::{Path, PathBuf};
use std::sync::atomic::{AtomicU64, AtomicUsize, Ordering};
use std::sync::Arc;
use std::time::Instant;
use tauri::Emitter;

pub const VERSION_1_8_9_URL: &str =
    "https://piston-meta.mojang.com/v1/packages/d546f1707a3f2b7d034eece5ea2e311eda875787/1.8.9.json";
pub const RESOURCES_BASE_URL: &str = "https://resources.download.minecraft.net";

#[derive(Clone, serde::Serialize)]
pub struct DownloadProgressPayload {
    pub step: String,
    pub current_file: String,
    pub completed_items: usize,
    pub total_items: usize,
    pub downloaded_bytes: u64,
    pub total_bytes: u64,
    pub speed_mb_s: f64,
    pub percentage: f32,
}

#[derive(Clone)]
pub struct DownloadTask {
    pub url: String,
    pub destination: PathBuf,
    pub expected_sha1: Option<String>,
    pub size: u64,
    pub is_native: bool,
}

pub struct IntegrityEngine {
    client: reqwest::Client,
    game_dir: PathBuf,
}

impl IntegrityEngine {
    pub fn new(game_dir: PathBuf) -> Self {
        let client = reqwest::Client::builder()
            .tcp_nodelay(true)
            .pool_max_idle_per_host(32)
            .timeout(std::time::Duration::from_secs(30))
            .build()
            .unwrap_or_default();
        Self { client, game_dir }
    }

    pub fn compute_sha1(path: &Path) -> Option<String> {
        if !path.exists() {
            return None;
        }
        let mut file = File::open(path).ok()?;
        let mut hasher = Sha1::new();
        let mut buffer = [0u8; 65536];
        loop {
            let n = file.read(&mut buffer).ok()?;
            if n == 0 {
                break;
            }
            hasher.update(&buffer[..n]);
        }
        Some(format!("{:x}", hasher.finalize()))
    }

    pub fn verify_file(path: &Path, expected_sha1: Option<&str>, expected_size: u64) -> bool {
        if !path.exists() {
            return false;
        }
        if let Ok(metadata) = fs::metadata(path) {
            if expected_size > 0 && metadata.len() != expected_size {
                return false;
            }
        } else {
            return false;
        }

        if let Some(expected_hash) = expected_sha1 {
            if let Some(actual_hash) = Self::compute_sha1(path) {
                return actual_hash.eq_ignore_ascii_case(expected_hash);
            }
            return false;
        }

        true
    }

    pub async fn fetch_version_manifest(&self) -> Result<VersionManifest, String> {
        let resp = self
            .client
            .get(VERSION_1_8_9_URL)
            .send()
            .await
            .map_err(|e| format!("Failed to fetch 1.8.9 manifest: {}", e))?;
        let manifest = resp
            .json::<VersionManifest>()
            .await
            .map_err(|e| format!("Failed to parse version manifest JSON: {}", e))?;
        Ok(manifest)
    }

    pub async fn fetch_asset_index(&self, url: &str) -> Result<AssetIndex, String> {
        let resp = self
            .client
            .get(url)
            .send()
            .await
            .map_err(|e| format!("Failed to fetch asset index: {}", e))?;
        let index = resp
            .json::<AssetIndex>()
            .await
            .map_err(|e| format!("Failed to parse asset index JSON: {}", e))?;
        Ok(index)
    }

    pub async fn sync_all<R: tauri::Runtime>(
        &self,
        app: &tauri::AppHandle<R>,
    ) -> Result<Vec<PathBuf>, String> {
        let _ = app.emit(
            "download_progress",
            DownloadProgressPayload {
                step: "Manifest Alınıyor...".to_string(),
                current_file: "1.8.9.json".to_string(),
                completed_items: 0,
                total_items: 1,
                downloaded_bytes: 0,
                total_bytes: 0,
                speed_mb_s: 0.0,
                percentage: 1.0,
            },
        );

        let manifest = self.fetch_version_manifest().await?;
        let mut download_tasks: Vec<DownloadTask> = Vec::new();
        let mut classpath_jars: Vec<PathBuf> = Vec::new();

        // 1. Client JAR
        let client_jar_path = self.game_dir.join("versions/1.8.9/1.8.9.jar");
        download_tasks.push(DownloadTask {
            url: manifest.downloads.client.url.clone(),
            destination: client_jar_path.clone(),
            expected_sha1: Some(manifest.downloads.client.sha1.clone()),
            size: manifest.downloads.client.size,
            is_native: false,
        });
        classpath_jars.push(client_jar_path);

        // 2. Libraries & Natives
        let natives_dir = self.game_dir.join("versions/1.8.9/1.8.9-natives");
        fs::create_dir_all(&natives_dir).map_err(|e| e.to_string())?;

        for lib in &manifest.libraries {
            if !lib.is_allowed_on_windows() {
                continue;
            }

            // Normal artifact library
            if let Some(downloads) = &lib.downloads {
                if let Some(artifact) = &downloads.artifact {
                    let rel_path = artifact.path.clone().unwrap_or_else(|| {
                        lib_name_to_path(&lib.name)
                    });
                    let dest = self.game_dir.join("libraries").join(rel_path);
                    download_tasks.push(DownloadTask {
                        url: artifact.url.clone(),
                        destination: dest.clone(),
                        expected_sha1: Some(artifact.sha1.clone()),
                        size: artifact.size,
                        is_native: false,
                    });
                    classpath_jars.push(dest);
                }

                // Check native classifier for Windows
                if let Some(classifier_key) = lib.get_windows_native_classifier() {
                    if let Some(classifiers) = &downloads.classifiers {
                        if let Some(native_artifact) = classifiers.get(&classifier_key) {
                            let rel_path = native_artifact.path.clone().unwrap_or_else(|| {
                                format!("natives/{}-{}.jar", lib.name, classifier_key)
                            });
                            let dest = self.game_dir.join("libraries").join(rel_path);
                            download_tasks.push(DownloadTask {
                                url: native_artifact.url.clone(),
                                destination: dest,
                                expected_sha1: Some(native_artifact.sha1.clone()),
                                size: native_artifact.size,
                                is_native: true,
                            });
                        }
                    }
                }
            } else {
                // Fallback for legacy maven repository
                let rel_path = lib_name_to_path(&lib.name);
                let url = format!(
                    "{}/{}",
                    lib.url.as_deref().unwrap_or("https://libraries.minecraft.net"),
                    rel_path
                );
                let dest = self.game_dir.join("libraries").join(rel_path);
                download_tasks.push(DownloadTask {
                    url,
                    destination: dest.clone(),
                    expected_sha1: None,
                    size: 0,
                    is_native: false,
                });
                classpath_jars.push(dest);
            }
        }

        // 3. Asset Index & Objects
        let asset_index_file = self
            .game_dir
            .join("assets/indexes")
            .join(format!("{}.json", manifest.assetIndex.id));
        if !Self::verify_file(
            &asset_index_file,
            Some(&manifest.assetIndex.sha1),
            manifest.assetIndex.size,
        ) {
            if let Some(parent) = asset_index_file.parent() {
                let _ = fs::create_dir_all(parent);
            }
            let bytes = self
                .client
                .get(&manifest.assetIndex.url)
                .send()
                .await
                .map_err(|e| e.to_string())?
                .bytes()
                .await
                .map_err(|e| e.to_string())?;
            fs::write(&asset_index_file, &bytes).map_err(|e| e.to_string())?;
        }

        let asset_index = self.fetch_asset_index(&manifest.assetIndex.url).await?;
        for (_, obj) in asset_index.objects {
            let prefix = &obj.hash[0..2];
            let obj_path = self
                .game_dir
                .join("assets/objects")
                .join(prefix)
                .join(&obj.hash);
            let url = format!("{}/{}/{}", RESOURCES_BASE_URL, prefix, obj.hash);

            download_tasks.push(DownloadTask {
                url,
                destination: obj_path,
                expected_sha1: Some(obj.hash),
                size: obj.size,
                is_native: false,
            });
        }

        // Filter tasks that already exist and pass hash check
        let _ = app.emit(
            "download_progress",
            DownloadProgressPayload {
                step: "Dosya Bütünlüğü Doğrulanıyor (Hash Kontrolü)...".to_string(),
                current_file: "SHA-1 Doğrulaması".to_string(),
                completed_items: 0,
                total_items: download_tasks.len(),
                downloaded_bytes: 0,
                total_bytes: 0,
                speed_mb_s: 0.0,
                percentage: 5.0,
            },
        );

        let mut tasks_to_download = Vec::new();
        let mut natives_to_extract = Vec::new();

        for task in download_tasks {
            let is_valid = Self::verify_file(
                &task.destination,
                task.expected_sha1.as_deref(),
                task.size,
            );
            if !is_valid {
                tasks_to_download.push(task.clone());
            }
            if task.is_native {
                natives_to_extract.push(task.destination.clone());
            }
        }

        let total_items = tasks_to_download.len();
        let total_bytes: u64 = tasks_to_download.iter().map(|t| t.size).sum();

        if total_items > 0 {
            let completed_items = Arc::new(AtomicUsize::new(0));
            let downloaded_bytes = Arc::new(AtomicU64::new(0));
            let start_time = Instant::now();

            let semaphore = Arc::new(tokio::sync::Semaphore::new(32));
            let mut handles = Vec::new();

            for task in tasks_to_download {
                let sem = semaphore.clone();
                let client = self.client.clone();
                let completed = completed_items.clone();
                let downloaded = downloaded_bytes.clone();
                let app_handle = app.clone();
                let file_name = task
                    .destination
                    .file_name()
                    .unwrap_or_default()
                    .to_string_lossy()
                    .to_string();

                handles.push(tokio::spawn(async move {
                    let _permit = sem.acquire().await.unwrap();

                    if let Some(parent) = task.destination.parent() {
                        let _ = fs::create_dir_all(parent);
                    }

                    // Download with retry
                    let mut attempts = 0;
                    let max_attempts = 3;
                    let mut success = false;

                    while attempts < max_attempts && !success {
                        attempts += 1;
                        if let Ok(resp) = client.get(&task.url).send().await {
                            if resp.status().is_success() {
                                if let Ok(bytes) = resp.bytes().await {
                                    if let Ok(mut out_file) = File::create(&task.destination) {
                                        if out_file.write_all(&bytes).is_ok() {
                                            success = true;
                                            downloaded.fetch_add(bytes.len() as u64, Ordering::SeqCst);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    let cur_completed = completed.fetch_add(1, Ordering::SeqCst) + 1;
                    let cur_bytes = downloaded.load(Ordering::SeqCst);
                    let elapsed = start_time.elapsed().as_secs_f64().max(0.001);
                    let speed = (cur_bytes as f64 / 1_048_576.0) / elapsed;
                    let percent = 10.0 + (cur_completed as f32 / total_items as f32) * 85.0;

                    let _ = app_handle.emit(
                        "download_progress",
                        DownloadProgressPayload {
                            step: "İndiriliyor...".to_string(),
                            current_file: file_name,
                            completed_items: cur_completed,
                            total_items,
                            downloaded_bytes: cur_bytes,
                            total_bytes,
                            speed_mb_s: (speed * 100.0).round() / 100.0,
                            percentage: percent,
                        },
                    );
                }));
            }

            for handle in handles {
                let _ = handle.await;
            }
        }

        // 4. Extract LWJGL Natives
        let _ = app.emit(
            "download_progress",
            DownloadProgressPayload {
                step: "LWJGL 2.9.x Natives Ayıklanıyor...".to_string(),
                current_file: "DLL Kütüphaneleri".to_string(),
                completed_items: total_items,
                total_items,
                downloaded_bytes: total_bytes,
                total_bytes,
                speed_mb_s: 0.0,
                percentage: 97.0,
            },
        );

        for native_jar in natives_to_extract {
            if native_jar.exists() {
                extract_natives(&native_jar, &natives_dir)?;
            }
        }

        let _ = app.emit(
            "download_progress",
            DownloadProgressPayload {
                step: "Tüm Dosyalar Hazır!".to_string(),
                current_file: "Tamamlandı".to_string(),
                completed_items: total_items,
                total_items,
                downloaded_bytes: total_bytes,
                total_bytes,
                speed_mb_s: 0.0,
                percentage: 100.0,
            },
        );

        Ok(classpath_jars)
    }
}

fn extract_natives(jar_path: &Path, output_dir: &Path) -> Result<(), String> {
    let file = File::open(jar_path).map_err(|e| e.to_string())?;
    let mut archive = zip::ZipArchive::new(file).map_err(|e| e.to_string())?;

    for i in 0..archive.len() {
        let mut file = archive.by_index(i).map_err(|e| e.to_string())?;
        let name = file.name().to_string();

        // Skip META-INF and non-native binaries
        if name.starts_with("META-INF/") || (!name.ends_with(".dll") && !name.ends_with(".so") && !name.ends_with(".dylib")) {
            continue;
        }

        let out_file_path = output_dir.join(Path::new(&name).file_name().unwrap_or_else(|| Path::new(&name)));
        let mut out_file = File::create(&out_file_path).map_err(|e| e.to_string())?;
        std::io::copy(&mut file, &mut out_file).map_err(|e| e.to_string())?;
    }
    Ok(())
}

fn lib_name_to_path(name: &str) -> String {
    // Format: group:artifact:version[:classifier]
    let parts: Vec<&str> = name.split(':').collect();
    if parts.len() < 3 {
        return name.to_string();
    }
    let group = parts[0].replace('.', "/");
    let artifact = parts[1];
    let version = parts[2];
    let classifier = if parts.len() > 3 {
        format!("-{}", parts[3])
    } else {
        "".to_string()
    };

    format!("{}/{}/{}/{}-{}{}.jar", group, artifact, version, artifact, version, classifier)
}
