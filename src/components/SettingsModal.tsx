import React, { useState } from "react";
import { LauncherConfig, SystemSpecs } from "../types";
import { X, Cpu, Monitor, Server, Save, Check, HardDrive } from "lucide-react";

interface SettingsModalProps {
  isOpen: boolean;
  onClose: () => void;
  config: LauncherConfig;
  onSave: (newConfig: LauncherConfig) => void;
  systemSpecs: SystemSpecs | null;
}

export const SettingsModal: React.FC<SettingsModalProps> = ({
  isOpen,
  onClose,
  config,
  onSave,
  systemSpecs,
}) => {
  const [form, setForm] = useState<LauncherConfig>({ ...config });
  const [savedBadge, setSavedBadge] = useState(false);

  if (!isOpen) return null;

  const totalRamGb = systemSpecs ? Math.floor(systemSpecs.total_ram_mb / 1024) : 16;
  const maxAllowedRam = Math.min(totalRamGb, 32);

  const handleRamChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const gb = parseInt(e.target.value, 10);
    const mb = gb * 1024;
    setForm({
      ...form,
      max_ram_mb: mb,
      min_ram_mb: Math.min(form.min_ram_mb, mb),
    });
  };

  const handleSave = () => {
    onSave(form);
    setSavedBadge(true);
    setTimeout(() => {
      setSavedBadge(false);
      onClose();
    }, 600);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 backdrop-blur-md p-4 animate-fadeIn">
      <div className="w-full max-w-xl cyber-panel-glow rounded-2xl p-6 border border-cyan-500/40 shadow-2xl flex flex-col space-y-5 max-h-[90vh] overflow-y-auto">
        {/* Header */}
        <div className="flex items-center justify-between pb-3 border-b border-cyber-border">
          <div className="flex items-center space-x-2.5">
            <div className="p-2 rounded-lg bg-cyan-950 text-cyan-400 border border-cyan-800">
              <Cpu className="w-5 h-5" />
            </div>
            <div>
              <h2 className="text-lg font-display font-bold text-white tracking-wide">
                CLIENT AYARLARI
              </h2>
              <p className="text-xs text-cyber-muted font-mono">
                JVM Performans, Bellek ve Bağlantı Parametreleri
              </p>
            </div>
          </div>

          <button
            onClick={onClose}
            className="p-1.5 rounded-lg text-cyber-muted hover:text-white hover:bg-cyber-card transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Form Body */}
        <div className="flex flex-col space-y-5 text-xs font-mono">
          {/* RAM Allocation Slider */}
          <div className="cyber-panel p-4 rounded-xl border border-cyber-border/70 flex flex-col space-y-2">
            <div className="flex justify-between items-center">
              <span className="text-white font-semibold flex items-center space-x-2">
                <HardDrive className="w-4 h-4 text-cyan-400" />
                <span>Maksimum RAM Tahsisi (-Xmx)</span>
              </span>
              <span className="text-cyan-400 font-bold text-sm bg-cyan-950/80 px-2 py-0.5 rounded border border-cyan-800">
                {(form.max_ram_mb / 1024).toFixed(0)} GB ({form.max_ram_mb} MB)
              </span>
            </div>

            <input
              type="range"
              min="1"
              max={maxAllowedRam}
              step="1"
              value={Math.round(form.max_ram_mb / 1024)}
              onChange={handleRamChange}
              className="w-full accent-cyan-400 bg-cyber-bg h-2 rounded-lg cursor-pointer"
            />

            <div className="flex justify-between text-[10px] text-gray-500">
              <span>1 GB</span>
              <span>
                Sistem RAM: {totalRamGb} GB (Önerilen: 3 - 4 GB)
              </span>
              <span>{maxAllowedRam} GB</span>
            </div>
          </div>

          {/* Server Connection Config */}
          <div className="cyber-panel p-4 rounded-xl border border-cyber-border/70 flex flex-col space-y-3">
            <div className="flex items-center space-x-2 text-white font-semibold">
              <Server className="w-4 h-4 text-cyan-400" />
              <span>Doğrudan Sunucu Bağlantısı (Direct-Connect)</span>
            </div>

            <div className="grid grid-cols-3 gap-3">
              <div className="col-span-2 flex flex-col space-y-1">
                <label className="text-[10px] text-cyber-muted uppercase">
                  Sunucu IP / Hostname
                </label>
                <input
                  type="text"
                  value={form.server_host}
                  onChange={(e) => setForm({ ...form, server_host: e.target.value })}
                  placeholder="localhost"
                  className="bg-cyber-bg border border-cyber-border rounded-lg px-3 py-2 text-white focus:outline-none focus:border-cyan-500"
                />
              </div>

              <div className="col-span-1 flex flex-col space-y-1">
                <label className="text-[10px] text-cyber-muted uppercase">Port</label>
                <input
                  type="number"
                  value={form.server_port}
                  onChange={(e) =>
                    setForm({ ...form, server_port: parseInt(e.target.value, 10) || 25565 })
                  }
                  placeholder="25565"
                  className="bg-cyber-bg border border-cyber-border rounded-lg px-3 py-2 text-white focus:outline-none focus:border-cyan-500"
                />
              </div>
            </div>

            {/* Auto-Connect Method */}
            <div className="flex flex-col space-y-1 pt-2">
              <label className="text-[10px] text-cyber-muted uppercase">
                Auto-Join Yöntemi
              </label>
              <select
                value={form.auto_connect_method}
                onChange={(e) =>
                  setForm({
                    ...form,
                    auto_connect_method: e.target.value as "direct_cli" | "java_agent",
                  })
                }
                className="bg-cyber-bg border border-cyber-border rounded-lg px-3 py-2 text-white focus:outline-none focus:border-cyan-500 text-xs"
              >
                <option value="direct_cli">
                  Vanilla CLI Enjeksiyonu (--server & --port) - Hızlı ve Kararlı
                </option>
                <option value="java_agent">
                  Bytecode Companion JavaAgent (CraftRise / Badlion Tarzı Menü Bypass)
                </option>
              </select>
            </div>
          </div>

          {/* Screen & Resolution */}
          <div className="cyber-panel p-4 rounded-xl border border-cyber-border/70 flex flex-col space-y-3">
            <div className="flex items-center space-x-2 text-white font-semibold">
              <Monitor className="w-4 h-4 text-cyan-400" />
              <span>Ekran ve Çözünürlük</span>
            </div>

            <div className="flex items-center justify-between py-1">
              <span className="text-gray-300">Tam Ekran Başlat (Fullscreen)</span>
              <input
                type="checkbox"
                checked={form.fullscreen}
                onChange={(e) => setForm({ ...form, fullscreen: e.target.checked })}
                className="w-4 h-4 accent-cyan-400 rounded cursor-pointer"
              />
            </div>

            {!form.fullscreen && (
              <div className="grid grid-cols-2 gap-3 pt-1">
                <div>
                  <label className="text-[10px] text-cyber-muted uppercase">
                    Genişlik (px)
                  </label>
                  <input
                    type="number"
                    value={form.resolution_width}
                    onChange={(e) =>
                      setForm({
                        ...form,
                        resolution_width: parseInt(e.target.value, 10) || 1280,
                      })
                    }
                    className="w-full bg-cyber-bg border border-cyber-border rounded-lg px-3 py-1.5 text-white focus:outline-none focus:border-cyan-500"
                  />
                </div>
                <div>
                  <label className="text-[10px] text-cyber-muted uppercase">
                    Yükseklik (px)
                  </label>
                  <input
                    type="number"
                    value={form.resolution_height}
                    onChange={(e) =>
                      setForm({
                        ...form,
                        resolution_height: parseInt(e.target.value, 10) || 720,
                      })
                    }
                    className="w-full bg-cyber-bg border border-cyber-border rounded-lg px-3 py-1.5 text-white focus:outline-none focus:border-cyan-500"
                  />
                </div>
              </div>
            )}
          </div>

          {/* Java Configuration */}
          <div className="cyber-panel p-4 rounded-xl border border-cyber-border/70 flex flex-col space-y-2">
            <span className="text-white font-semibold">Java 8 (JRE) Yönetimi</span>
            <div className="flex items-center space-x-2">
              <input
                type="checkbox"
                id="bundled_java"
                checked={form.use_bundled_java}
                onChange={(e) => setForm({ ...form, use_bundled_java: e.target.checked })}
                className="w-4 h-4 accent-cyan-400 rounded cursor-pointer"
              />
              <label htmlFor="bundled_java" className="text-gray-300 cursor-pointer">
                Otomatik İndirilen Azul Zulu 8 JRE (Önerilen - En Yüksek FPS)
              </label>
            </div>

            {!form.use_bundled_java && (
              <div className="flex flex-col space-y-1 pt-1">
                <label className="text-[10px] text-cyber-muted uppercase">
                  Özel Java Yolu (javaw.exe)
                </label>
                <input
                  type="text"
                  value={form.custom_java_path || ""}
                  onChange={(e) =>
                    setForm({ ...form, custom_java_path: e.target.value || null })
                  }
                  placeholder="C:\Program Files\Java\jre1.8.0_xxx\bin\javaw.exe"
                  className="w-full bg-cyber-bg border border-cyber-border rounded-lg px-3 py-1.5 text-white focus:outline-none focus:border-cyan-500 font-mono text-xs"
                />
              </div>
            )}
          </div>
        </div>

        {/* Footer Actions */}
        <div className="pt-3 border-t border-cyber-border flex justify-end space-x-3">
          <button
            onClick={onClose}
            className="px-4 py-2 rounded-lg bg-cyber-card text-cyber-muted hover:text-white border border-cyber-border transition-colors font-mono text-xs"
          >
            İptal
          </button>

          <button
            onClick={handleSave}
            className="px-5 py-2 rounded-lg bg-gradient-to-r from-cyan-500 to-indigo-600 text-white font-display font-bold tracking-wider uppercase flex items-center space-x-2 shadow-neon-cyan hover:opacity-90 transition-all text-sm"
          >
            {savedBadge ? (
              <>
                <Check className="w-4 h-4 text-white" />
                <span>KAYDEDİLDİ!</span>
              </>
            ) : (
              <>
                <Save className="w-4 h-4" />
                <span>AYARLARI KAYDET</span>
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  );
};
