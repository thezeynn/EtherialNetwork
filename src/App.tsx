import React, { useEffect, useState, useCallback } from "react";
import { invoke } from "@tauri-apps/api/core";
import { listen } from "@tauri-apps/api/event";
import {
  LauncherConfig,
  ServerStatusResponse,
  DownloadProgressPayload,
  SystemSpecs,
  LauncherStatus,
} from "./types";
import { TitleBar } from "./components/TitleBar";
import { ServerStatus } from "./components/ServerStatus";
import { PlaySection } from "./components/PlaySection";
import { ProgressBar } from "./components/ProgressBar";
import { SettingsModal } from "./components/SettingsModal";
import { Shield, Zap, Sparkles, Terminal } from "lucide-react";

export const App: React.FC = () => {
  const [config, setConfig] = useState<LauncherConfig>({
    username: "EtherialPlayer",
    min_ram_mb: 2048,
    max_ram_mb: 4096,
    server_host: "localhost",
    server_port: 25565,
    fullscreen: false,
    resolution_width: 1280,
    resolution_height: 720,
    custom_java_path: null,
    use_bundled_java: true,
    auto_connect_method: "direct_cli",
  });

  const [systemSpecs, setSystemSpecs] = useState<SystemSpecs | null>(null);
  const [serverStatus, setServerStatus] = useState<ServerStatusResponse | null>(null);
  const [status, setStatus] = useState<LauncherStatus>("idle");
  const [progress, setProgress] = useState<DownloadProgressPayload | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);

  // Load configuration and specs on startup
  useEffect(() => {
    const initLauncher = async () => {
      try {
        const loadedConfig = await invoke<LauncherConfig>("get_launcher_config");
        setConfig(loadedConfig);
      } catch (e) {
        console.warn("Could not load config via Tauri, using defaults:", e);
      }

      try {
        const specs = await invoke<SystemSpecs>("get_system_specs");
        setSystemSpecs(specs);
      } catch (e) {
        console.warn("Could not query system specs:", e);
      }
    };

    initLauncher();
  }, []);

  // Ping Server function
  const fetchServerPing = useCallback(async (host: string, port: number) => {
    try {
      const res = await invoke<ServerStatusResponse>("ping_server", { host, port });
      setServerStatus(res);
    } catch (e) {
      setServerStatus({
        online: false,
        latency_ms: 0,
        players_online: 0,
        players_max: 0,
        motd: "Bağlantı hatası",
        version: "1.8.9",
      });
    }
  }, []);

  // Poll server status on interval
  useEffect(() => {
    fetchServerPing(config.server_host, config.server_port);
    const interval = setInterval(() => {
      fetchServerPing(config.server_host, config.server_port);
    }, 5000);
    return () => clearInterval(interval);
  }, [config.server_host, config.server_port, fetchServerPing]);

  // Listen to download progress event emitted from Rust
  useEffect(() => {
    let unlistenFn: (() => void) | undefined;

    const setupListener = async () => {
      try {
        unlistenFn = await listen<DownloadProgressPayload>(
          "download_progress",
          (event) => {
            setProgress(event.payload);
            if (event.payload.percentage < 100) {
              setStatus("downloading");
            }
          }
        );
      } catch (e) {
        console.warn("Tauri event listener failed:", e);
      }
    };

    setupListener();

    return () => {
      if (unlistenFn) unlistenFn();
    };
  }, []);

  // Handle Username update
  const handleUsernameChange = (newUsername: string) => {
    const updated = { ...config, username: newUsername };
    setConfig(updated);
    invoke("save_launcher_config", { config: updated }).catch(() => {});
  };

  // Handle Settings Save
  const handleSaveConfig = async (newConfig: LauncherConfig) => {
    setConfig(newConfig);
    try {
      await invoke("save_launcher_config", { config: newConfig });
      fetchServerPing(newConfig.server_host, newConfig.server_port);
    } catch (e) {
      console.error("Config kaydedilemedi:", e);
    }
  };

  // Launch Game
  const handleLaunch = async () => {
    setErrorMessage(null);
    setStatus("checking_files");

    try {
      await invoke("save_launcher_config", { config });
      setStatus("downloading");

      const launchMsg = await invoke<string>("start_game");
      console.log(launchMsg);
      setStatus("playing");

      // Reset to idle after 10 seconds or keep playing status
      setTimeout(() => {
        setStatus("idle");
        setProgress(null);
      }, 8000);
    } catch (err: any) {
      console.error("Launch Error:", err);
      setStatus("error");
      setErrorMessage(
        typeof err === "string" ? err : err?.message || "Oyun başlatılırken bilinmeyen bir hata oluştu."
      );
    }
  };

  return (
    <div className="w-screen h-screen flex flex-col bg-cyber-bg text-cyber-text relative overflow-hidden select-none">
      {/* Background Cyber Ambient Lights */}
      <div className="absolute -top-32 -left-32 w-96 h-96 bg-cyan-500/10 rounded-full blur-[100px] pointer-events-none" />
      <div className="absolute -bottom-32 -right-32 w-96 h-96 bg-purple-600/15 rounded-full blur-[120px] pointer-events-none" />

      {/* Grid Pattern Background */}
      <div
        className="absolute inset-0 opacity-[0.03] pointer-events-none"
        style={{
          backgroundImage:
            "linear-gradient(to right, #ffffff 1px, transparent 1px), linear-gradient(to bottom, #ffffff 1px, transparent 1px)",
          backgroundSize: "40px 40px",
        }}
      />

      {/* Frameless Custom TitleBar */}
      <TitleBar
        onOpenSettings={() => setIsSettingsOpen(true)}
        serverOnline={serverStatus?.online ?? false}
        latency={serverStatus?.latency_ms ?? 0}
      />

      {/* Main Content Area */}
      <div className="flex-1 p-6 flex flex-col justify-between z-10 relative">
        {/* Top Hero / Server Card Area */}
        <div className="grid grid-cols-1 md:grid-cols-12 gap-5 items-stretch">
          {/* Left: Client Banner & Highlights */}
          <div className="md:col-span-7 cyber-panel rounded-2xl p-5 border border-cyber-border/80 flex flex-col justify-between relative overflow-hidden">
            <div className="flex items-center space-x-2 text-cyan-400 text-xs font-mono font-semibold tracking-wider uppercase mb-1">
              <Sparkles className="w-4 h-4" />
              <span>Next-Gen Competitive Architecture</span>
            </div>

            <div>
              <h1 className="text-3xl font-display font-extrabold text-white tracking-wide">
                ETHERIAL <span className="text-transparent bg-clip-text bg-gradient-to-r from-cyan-400 to-indigo-400">1.8.9 CLIENT</span>
              </h1>
              <p className="text-xs text-cyber-muted mt-2 leading-relaxed">
                SonOyuncu & CraftRise seviyesinde optimize edilmiş, LWJGL 2.9.x bellek sızıntılarını önleyen Aikar G1GC optimizasyonu ve anında doğrudan sunucuya bağlanma motoru.
              </p>
            </div>

            {/* Feature Pills */}
            <div className="flex flex-wrap gap-2 mt-4">
              <div className="flex items-center space-x-1.5 px-2.5 py-1 rounded-lg bg-cyber-bg/80 border border-cyber-border text-[11px] font-mono text-gray-300">
                <Zap className="w-3.5 h-3.5 text-cyan-400" />
                <span>Azul Zulu 8 JRE</span>
              </div>
              <div className="flex items-center space-x-1.5 px-2.5 py-1 rounded-lg bg-cyber-bg/80 border border-cyber-border text-[11px] font-mono text-gray-300">
                <Shield className="w-3.5 h-3.5 text-emerald-400" />
                <span>SHA-1 Doğrulama</span>
              </div>
              <div className="flex items-center space-x-1.5 px-2.5 py-1 rounded-lg bg-cyber-bg/80 border border-cyber-border text-[11px] font-mono text-gray-300">
                <Terminal className="w-3.5 h-3.5 text-purple-400" />
                <span>Direct-Connect Engine</span>
              </div>
            </div>
          </div>

          {/* Right: Server Live Status */}
          <div className="md:col-span-5">
            <ServerStatus
              status={serverStatus}
              host={config.server_host}
              port={config.server_port}
            />
          </div>
        </div>

        {/* Bottom Interactive Area */}
        <div className="flex flex-col space-y-4">
          {/* Progress Bar (Visible during checking/downloading) */}
          {(status === "checking_files" || status === "downloading") && (
            <ProgressBar progress={progress} />
          )}

          {/* Play Trigger & Username */}
          <PlaySection
            username={config.username}
            onUsernameChange={handleUsernameChange}
            status={status}
            onLaunch={handleLaunch}
            errorMessage={errorMessage}
          />
        </div>
      </div>

      {/* Settings Modal */}
      <SettingsModal
        isOpen={isSettingsOpen}
        onClose={() => setIsSettingsOpen(false)}
        config={config}
        onSave={handleSaveConfig}
        systemSpecs={systemSpecs}
      />
    </div>
  );
};
export default App;
