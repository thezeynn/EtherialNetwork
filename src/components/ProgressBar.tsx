import React from "react";
import { DownloadProgressPayload } from "../types";
import { DownloadCloud, Activity, HardDrive } from "lucide-react";

interface ProgressBarProps {
  progress: DownloadProgressPayload | null;
}

export const ProgressBar: React.FC<ProgressBarProps> = ({ progress }) => {
  if (!progress) return null;

  const percent = Math.min(Math.max(progress.percentage, 0), 100);

  return (
    <div className="w-full cyber-panel p-4 rounded-xl border border-cyan-500/30 shadow-[0_0_20px_rgba(0,240,255,0.08)] flex flex-col space-y-2.5 animate-fadeIn">
      {/* Top Labels: Step & Metrics */}
      <div className="flex items-center justify-between text-xs font-mono">
        <div className="flex items-center space-x-2 text-cyan-400 font-semibold">
          <DownloadCloud className="w-4 h-4 animate-bounce" />
          <span>{progress.step}</span>
        </div>

        <div className="flex items-center space-x-3 text-cyber-muted text-[11px]">
          {progress.speed_mb_s > 0 && (
            <div className="flex items-center space-x-1 text-emerald-400">
              <Activity className="w-3 h-3" />
              <span>{progress.speed_mb_s.toFixed(2)} MB/s</span>
            </div>
          )}
          <div className="flex items-center space-x-1 text-gray-400">
            <HardDrive className="w-3 h-3 text-cyan-400" />
            <span>
              {progress.completed_items} / {progress.total_items}
            </span>
          </div>
          <span className="font-bold text-white">{percent.toFixed(1)}%</span>
        </div>
      </div>

      {/* Progress Track */}
      <div className="w-full h-2.5 bg-cyber-bg/90 rounded-full overflow-hidden p-0.5 border border-cyber-border">
        <div
          className="h-full bg-gradient-to-r from-cyan-500 via-blue-500 to-indigo-500 rounded-full transition-all duration-200 relative shadow-neon-cyan"
          style={{ width: `${percent}%` }}
        >
          <div className="absolute inset-0 bg-white/20 animate-pulse rounded-full" />
        </div>
      </div>

      {/* Currently downloading file name */}
      <div className="flex justify-between items-center text-[10px] font-mono text-gray-500">
        <span className="truncate max-w-[80%]">Dosya: {progress.current_file}</span>
        <span>
          {(progress.downloaded_bytes / (1024 * 1024)).toFixed(1)} MB /{" "}
          {(progress.total_bytes / (1024 * 1024)).toFixed(1)} MB
        </span>
      </div>
    </div>
  );
};
