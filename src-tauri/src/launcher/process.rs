use crate::config::LauncherConfig;
use crate::launcher::classpath::build_classpath;
use crate::launcher::natives::get_natives_directory;
use std::path::{Path, PathBuf};
use std::process::{Child, Command, Stdio};

pub struct LaunchOptions {
    pub java_binary: PathBuf,
    pub game_dir: PathBuf,
    pub classpath_jars: Vec<PathBuf>,
    pub agent_jar: Option<PathBuf>,
    pub config: LauncherConfig,
}

pub fn generate_offline_uuid(username: &str) -> String {
    // Mojang offline player UUID: MD5("OfflinePlayer:" + username) with version 3 UUID bits
    let hash = md5::compute(format!("OfflinePlayer:{}", username).as_bytes());
    let mut bytes = hash.0;

    // Set UUID version to 3 (name-based MD5)
    bytes[6] = (bytes[6] & 0x0f) | 0x30;
    // Set UUID variant to IETF RFC 4122
    bytes[8] = (bytes[8] & 0x3f) | 0x80;

    format!(
        "{:02x}{:02x}{:02x}{:02x}-{:02x}{:02x}-{:02x}{:02x}-{:02x}{:02x}-{:02x}{:02x}{:02x}{:02x}{:02x}{:02x}",
        bytes[0], bytes[1], bytes[2], bytes[3],
        bytes[4], bytes[5],
        bytes[6], bytes[7],
        bytes[8], bytes[9],
        bytes[10], bytes[11], bytes[12], bytes[13], bytes[14], bytes[15]
    )
}

pub fn build_launch_command(opts: &LaunchOptions) -> Command {
    let mut cmd = Command::new(&opts.java_binary);

    // 1. RAM Allocation (-Xms & -Xmx)
    cmd.arg(format!("-Xms{}M", opts.config.min_ram_mb));
    cmd.arg(format!("-Xmx{}M", opts.config.max_ram_mb));

    // 2. High-Performance FPS Boost GC Flags (Aikar & Java 8 G1GC Tweaks)
    cmd.args([
        "-XX:+UseG1GC",
        "-XX:+ParallelRefProcEnabled",
        "-XX:MaxGCPauseMillis=20",
        "-XX:+UnlockExperimentalVMOptions",
        "-XX:+DisableExplicitGC",
        "-XX:+AlwaysPreTouch",
        "-XX:G1NewSizePercent=30",
        "-XX:G1MaxNewSizePercent=40",
        "-XX:G1ReservePercent=15",
        "-XX:G1HeapWastePercent=5",
        "-XX:G1MixedGCCountTarget=4",
        "-XX:InitiatingHeapOccupancyPercent=15",
        "-XX:G1MixedGCLiveThresholdPercent=90",
        "-XX:G1RSetUpdatingPauseTimePercent=5",
        "-XX:SurvivorRatio=32",
        "-XX:+PerfDisableSharedMem",
        "-XX:MaxTenuringThreshold=1",
        "-Dsun.rmi.dgc.server.gcInterval=2147483646",
    ]);

    // 3. Native Libraries Path
    let natives_dir = get_natives_directory(&opts.game_dir);
    cmd.arg(format!("-Djava.library.path={}", natives_dir.to_string_lossy()));

    // 4. Companion JavaAgent (if selected or present)
    if opts.config.auto_connect_method == "java_agent" {
        if let Some(agent) = &opts.agent_jar {
            if agent.exists() {
                cmd.arg(format!(
                    "-javaagent:{}={}:{}",
                    agent.to_string_lossy(),
                    opts.config.server_host,
                    opts.config.server_port
                ));
            }
        }
    }

    // 5. Classpath Construction
    let cp_string = build_classpath(&opts.classpath_jars);
    cmd.arg("-cp").arg(cp_string);

    // 6. Main Class
    cmd.arg("net.minecraft.client.main.Main");

    // 7. Minecraft Arguments
    let uuid = generate_offline_uuid(&opts.config.username);
    let assets_dir = opts.game_dir.join("assets");

    cmd.args([
        "--username",
        &opts.config.username,
        "--version",
        "1.8.9",
        "--gameDir",
        &opts.game_dir.to_string_lossy(),
        "--assetsDir",
        &assets_dir.to_string_lossy(),
        "--assetIndex",
        "1.8",
        "--uuid",
        &uuid,
        "--accessToken",
        "00000000-0000-0000-0000-000000000000",
        "--userProperties",
        "{}",
        "--userType",
        "legacy",
        "--width",
        &opts.config.resolution_width.to_string(),
        "--height",
        &opts.config.resolution_height.to_string(),
    ]);

    if opts.config.fullscreen {
        cmd.arg("--fullscreen");
    }

    // 8. Auto-Connect Direct CLI argument (Vanilla 1.8.9 parameter)
    if opts.config.auto_connect_method == "direct_cli" {
        cmd.arg("--server").arg(&opts.config.server_host);
        cmd.arg("--port").arg(&opts.config.server_port.to_string());
    }

    // Current working directory set to game directory
    cmd.current_dir(&opts.game_dir);

    cmd
}

pub fn launch_game(opts: &LaunchOptions) -> Result<Child, String> {
    let mut cmd = build_launch_command(opts);

    // Detach stdout/stderr or pipe them
    cmd.stdout(Stdio::piped());
    cmd.stderr(Stdio::piped());

    println!("[Etherial Process] Spawning Minecraft 1.8.9...");
    let child = cmd
        .spawn()
        .map_err(|e| format!("Minecraft başlatılamadı: {}", e))?;

    Ok(child)
}
