import React from "react";
import { LauncherStatus } from "../types";
import { Play, Loader2, Gamepad2, ShieldCheck, AlertCircle } from "lucide-react";

interface PlaySectionProps {
  username: string;
  onUsernameChange: (val: string) => void;
  status: LauncherStatus;
  onLaunch: () => void;
  errorMessage: string | null;
}

export const PlaySection: React.FC<PlaySectionProps> = ({
  username,
  onUsernameChange,
  status,
  onLaunch,
  errorMessage,
}) => {
  const isBusy =
    status === "checking_files" ||
    status === "downloading" ||
    status === "launching";

  const getButtonText = () => {
    switch (status) {
      case "checking_files":
        return "DOSYALAR DOĞRULANIYOR...";
      case "downloading":
        return "EKSİKLER İNDİRİLİYOR...";
      case "launching":
        return "CLIENT BAŞLATILIYOR...";
      case "playing":
        return "OYUN AÇIK (1.8.9)";
      default:
        return "OYNA & DOĞRUDAN BAĞLAN";
    }
  };

  return (
    <div className="w-full flex flex-col space-y-4">
      {/* Error notification if any */}
      {errorMessage && (
        <div className="flex items-center space-x-2.5 p-3 rounded-lg bg-rose-950/70 border border-rose-800 text-rose-200 text-xs animate-shake">
          <AlertCircle className="w-4 h-4 text-rose-400 shrink-0" />
          <span className="font-mono">{errorMessage}</span>
        </div>
      )}

      {/* Profile Bar & Play Trigger */}
      <div className="grid grid-cols-1 md:grid-cols-12 gap-3 items-center">
        {/* Username Input & Skin Avatar */}
        <div className="md:col-span-5 cyber-panel p-2.5 rounded-xl border border-cyber-border flex items-center space-x-3 group focus-within:border-cyan-500/60 transition-colors">
          <div className="relative w-10 h-10 rounded-lg overflow-hidden bg-cyber-bg border border-cyber-border/80 shrink-0 shadow-inner">
            <img
              src={`https://minotar.net/avatar/${username.trim() || "MHF_Steve"}/64.png`}
              alt="Avatar"
              className="w-full h-full object-cover pixelated"
              onError={(e) => {
                // Fallback to Steve on network error
                (e.target as HTMLImageElement).src =
                  "https://minotar.net/avatar/MHF_Steve/64.png";
              }}
            />
            <div className="absolute inset-0 ring-1 ring-inset ring-white/10 rounded-lg" />
          </div>

          <div className="flex flex-col flex-1">
            <label className="text-[10px] uppercase font-mono tracking-wider text-cyber-muted flex items-center space-x-1">
              <span>Oyuncu Adı</span>
              <ShieldCheck className="w-2.5 h-2.5 text-cyan-400" />
            </label>
            <input
              type="text"
              value={username}
              disabled={isBusy}
              onChange={(e) => onUsernameChange(e.target.value)}
              placeholder="Kullanıcı Adı"
              className="bg-transparent text-white font-medium text-sm focus:outline-none placeholder:text-gray-600 font-sans tracking-wide"
              maxLength={16}
            />
          </div>
        </div>

        {/* The Mega Cyber Play Button */}
        <div className="md:col-span-7">
          <button
            onClick={onLaunch}
            disabled={isBusy || status === "playing" || username.trim().length === 0}
            className={`w-full relative group h-14 rounded-xl font-display font-extrabold text-base tracking-wider uppercase transition-all duration-300 flex items-center justify-center space-x-3 select-none overflow-hidden ${
              isBusy
                ? "bg-cyber-card border border-cyan-800 text-cyan-300 cursor-wait"
                : status === "playing"
                ? "bg-emerald-950/80 border border-emerald-600 text-emerald-400 shadow-[0_0_20px_rgba(16,185,129,0.3)]"
                : "bg-gradient-to-r from-cyan-500 via-indigo-600 to-purple-600 text-white shadow-neon-cyan hover:shadow-[0_0_35px_rgba(0,240,255,0.7)] hover:scale-[1.01] active:scale-[0.99] border border-cyan-300/40"
            }`}
          >
            {/* Shimmer sweep animation overlay */}
            <div className="absolute inset-0 -translate-x-full group-hover:translate-x-full duration-1000 bg-gradient-to-r from-transparent via-white/20 to-transparent transition-transform pointer-events-none" />

            {isBusy ? (
              <>
                <Loader2 className="w-5 h-5 animate-spin text-cyan-400" />
                <span className="font-mono tracking-wider text-sm">{getButtonText()}</span>
              </>
            ) : status === "playing" ? (
              <>
                <Gamepad2 className="w-5 h-5 text-emerald-400 animate-pulse" />
                <span>{getButtonText()}</span>
              </>
            ) : (
              <>
                <div className="p-1 rounded bg-black/20">
                  <Play className="w-5 h-5 fill-current text-white" />
                </div>
                <span className="tracking-widest">{getButtonText()}</span>
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  );
};
