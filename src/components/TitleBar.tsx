import React from "react";
import { getCurrentWindow } from "@tauri-apps/api/window";
import { Minus, X, Settings, Cpu } from "lucide-react";

interface TitleBarProps {
  onOpenSettings: () => void;
  serverOnline: boolean;
  latency: number;
}

export const TitleBar: React.FC<TitleBarProps> = ({
  onOpenSettings,
  serverOnline,
  latency,
}) => {
  const handleMinimize = async () => {
    try {
      const appWindow = getCurrentWindow();
      await appWindow.minimize();
    } catch (e) {
      console.warn("Tauri minimize error:", e);
    }
  };

  const handleClose = async () => {
    try {
      const appWindow = getCurrentWindow();
      await appWindow.close();
    } catch (e) {
      console.warn("Tauri close error:", e);
    }
  };

  return (
    <div
      data-tauri-drag-region
      className="h-12 w-full flex items-center justify-between px-4 bg-cyber-bg/90 border-b border-cyber-border/60 select-none z-50 backdrop-blur-md"
    >
      {/* Brand & Client Info */}
      <div className="flex items-center space-x-3 pointer-events-none" data-tauri-drag-region>
        <div className="relative flex items-center justify-center w-8 h-8 rounded-lg bg-gradient-to-tr from-cyan-500 to-indigo-600 shadow-neon-cyan/50">
          <Cpu className="w-5 h-5 text-white" />
          <div className="absolute -bottom-0.5 -right-0.5 w-2.5 h-2.5 rounded-full bg-cyan-400 animate-ping" />
        </div>
        <div className="flex flex-col">
          <div className="flex items-center space-x-2">
            <span className="font-display font-extrabold tracking-wider text-lg text-white text-glow-cyan">
              ETHERIAL
            </span>
            <span className="px-1.5 py-0.5 text-[10px] font-semibold bg-cyan-950 text-cyan-300 border border-cyan-800/80 rounded tracking-widest uppercase">
              1.8.9 Client
            </span>
          </div>
          <span className="text-[10px] text-cyber-muted -mt-1 font-mono">
            Azul Zulu 8 JRE • G1GC Turbo • Low-Latency
          </span>
        </div>
      </div>

      {/* Center Server Quick Badge */}
      <div className="hidden md:flex items-center space-x-2 px-3 py-1 bg-cyber-card/70 border border-cyber-border rounded-full text-xs">
        <span
          className={`w-2 h-2 rounded-full ${
            serverOnline ? "bg-emerald-400 shadow-[0_0_8px_#34d399]" : "bg-rose-500"
          }`}
        />
        <span className="text-gray-300 font-mono text-[11px]">
          {serverOnline ? `Direct-Connect: ${latency}ms` : "Sunucu Çevrimdışı"}
        </span>
      </div>

      {/* Window Controls */}
      <div className="flex items-center space-x-1">
        <button
          onClick={onOpenSettings}
          title="Ayarlar"
          className="p-1.5 rounded-md text-cyber-muted hover:text-white hover:bg-cyber-card transition-colors"
        >
          <Settings className="w-4 h-4" />
        </button>
        <button
          onClick={handleMinimize}
          title="Küçült"
          className="p-1.5 rounded-md text-cyber-muted hover:text-white hover:bg-cyber-card transition-colors"
        >
          <Minus className="w-4 h-4" />
        </button>
        <button
          onClick={handleClose}
          title="Kapat"
          className="p-1.5 rounded-md text-cyber-muted hover:text-white hover:bg-rose-600/80 transition-colors"
        >
          <X className="w-4 h-4" />
        </button>
      </div>
    </div>
  );
};
