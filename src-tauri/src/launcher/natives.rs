use std::path::{Path, PathBuf};

pub fn get_natives_directory(game_dir: &Path) -> PathBuf {
    game_dir.join("versions/1.8.9/1.8.9-natives")
}

pub fn verify_natives(natives_dir: &Path) -> Result<(), String> {
    if !natives_dir.exists() {
        return Err("Natives klasörü mevcut değil!".to_string());
    }

    #[cfg(target_os = "windows")]
    {
        let required_dlls = ["lwjgl.dll", "lwjgl64.dll", "OpenAL32.dll", "OpenAL64.dll"];
        for dll in &required_dlls {
            let p = natives_dir.join(dll);
            if !p.exists() {
                // Warning only: some packs merge 32 and 64 bit or use distinct naming
                println!("[Etherial Warn] Native library eksik olabilir: {}", dll);
            }
        }
    }

    Ok(())
}
