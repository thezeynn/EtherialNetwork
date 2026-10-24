use std::collections::HashSet;
use std::path::{Path, PathBuf};

pub fn build_classpath(jars: &[PathBuf]) -> String {
    let mut seen = HashSet::new();
    let mut entries = Vec::new();

    for jar in jars {
        if jar.exists() {
            let canon = jar.to_string_lossy().to_string();
            if seen.insert(canon.clone()) {
                entries.push(canon);
            }
        }
    }

    #[cfg(target_os = "windows")]
    let separator = ";";
    #[cfg(not(target_os = "windows"))]
    let separator = ":";

    entries.join(separator)
}

pub fn collect_all_libraries(game_dir: &Path) -> Vec<PathBuf> {
    let lib_dir = game_dir.join("libraries");
    let mut result = Vec::new();

    if lib_dir.exists() {
        for entry in walkdir::WalkDir::new(&lib_dir)
            .into_iter()
            .filter_map(|e| e.ok())
        {
            let path = entry.path();
            if path.is_file() {
                if let Some(ext) = path.extension() {
                    if ext == "jar" {
                        result.push(path.to_path_buf());
                    }
                }
            }
        }
    }

    let client_jar = game_dir.join("versions/1.8.9/1.8.9.jar");
    if client_jar.exists() {
        result.push(client_jar);
    }

    result
}
