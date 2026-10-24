import React from "react";
import { ServerStatusResponse } from "../types";
import { Wifi, WifiOff, Users, Radio, Globe } from "lucide-react";

interface ServerStatusProps {
  status: ServerStatusResponse | null;
  host: string;
  port: number;
}

export const ServerStatus: React.FC<ServerStatusProps> = ({
  status,
  host,
  port,
}) => {
  const isOnline = status?.online ?? false;

  return (
    <div className="cyber-panel rounded-xl p-4 border border-cyber-border/70 flex flex-col justify-between relative overflow-hidden group hover:border-cyan-500/40 transition-all duration-300">
      {/* Background Subtle Cyber Glow */}
      <div className="absolute top-0 right-0 w-32 h-32 bg-cyan-500/5 rounded-full blur-2xl pointer-events-none group-hover:bg-cyan-500/10 transition-all" />

      {/* Header Info */}
      <div className="flex items-center justify-between mb-3">
        <div className="flex items-center space-x-2">
          <Globe className="w-4 h-4 text-cyan-400" />
          <span className="text-xs font-semibold tracking-wider uppercase text-cyber-muted font-display">
            Hedef Oyun Sunucusu
          </span>
        </div>

        <div className="flex items-center space-x-1.5 px-2 py-0.5 rounded-full text-[11px] font-mono border border-cyber-border bg-cyber-bg/60">
          {isOnline ? (
            <>
              <Wifi className="w-3.5 h-3.5 text-emerald-400" />
              <span className="text-emerald-400 font-bold">{status?.latency_ms} ms</span>
            </>
          ) : (
            <>
              <WifiOff className="w-3.5 h-3.5 text-rose-500" />
              <span className="text-rose-500 font-bold">Offline</span>
            </>
          )}
        </div>
      </div>

      {/* Server IP & MOTD */}
      <div className="mb-3">
        <div className="flex items-baseline space-x-1.5 font-mono">
          <span className="text-base font-bold text-white tracking-wide">
            {host}
          </span>
          <span className="text-xs text-cyber-muted">:{port}</span>
        </div>
        <p className="text-xs text-gray-400 mt-1 line-clamp-1 italic">
          {status?.motd || "Sunucu durumu sorgulanıyor..."}
        </p>
      </div>

      {/* Footer Metrics */}
      <div className="pt-2 border-t border-cyber-border/40 flex items-center justify-between text-xs text-cyber-muted">
        <div className="flex items-center space-x-1.5 font-mono">
          <Users className="w-3.5 h-3.5 text-cyan-400" />
          <span className="text-white font-medium">
            {isOnline ? `${status?.players_online} / ${status?.players_max}` : "-- / --"}
          </span>
          <span className="text-[10px]">Oyuncu</span>
        </div>

        <div className="flex items-center space-x-1 font-mono text-[11px] text-cyan-300/80">
          <Radio className="w-3 h-3 animate-pulse text-cyan-400" />
          <span>Auto-Join Aktif</span>
        </div>
      </div>
    </div>
  );
};
