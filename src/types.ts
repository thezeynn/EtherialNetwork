export interface LauncherConfig {
  username: string;
  min_ram_mb: number;
  max_ram_mb: number;
  server_host: string;
  server_port: number;
  fullscreen: boolean;
  resolution_width: number;
  resolution_height: number;
  custom_java_path: string | null;
  use_bundled_java: boolean;
  auto_connect_method: "direct_cli" | "java_agent";
}

export interface ServerStatusResponse {
  online: boolean;
  latency_ms: number;
  players_online: number;
  players_max: number;
  motd: string;
  version: string;
}

export interface DownloadProgressPayload {
  step: string;
  current_file: string;
  completed_items: number;
  total_items: number;
  downloaded_bytes: number;
  total_bytes: number;
  speed_mb_s: number;
  percentage: number;
}

export interface SystemSpecs {
  total_ram_mb: number;
  available_ram_mb: number;
  cpu_cores: number;
  os_name: string;
}

export type LauncherStatus =
  | "idle"
  | "checking_files"
  | "downloading"
  | "launching"
  | "playing"
  | "error";
