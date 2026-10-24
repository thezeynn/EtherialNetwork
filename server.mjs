import http from "http";
import fs from "fs";
import path from "path";
import os from "os";
import net from "net";
import crypto from "crypto";
import { spawn } from "child_process";
import https from "https";

const PORT = 3000;
const MANIFEST_URL = "https://piston-meta.mojang.com/v1/packages/d546f1707a3f2b7d034eece5ea2e311eda875787/1.8.9.json";
const RESOURCES_BASE_URL = "https://resources.download.minecraft.net";

const APP_DIR = path.join(process.env.APPDATA || ".", ".etherial");
const CONFIG_FILE = path.join(APP_DIR, "launcher_config.json");
const GAME_DIR = path.join(APP_DIR, "game");
const NATIVES_DIR = path.join(GAME_DIR, "versions", "1.8.9", "1.8.9-natives");
const LIBRARIES_DIR = path.join(GAME_DIR, "libraries");
const CLIENT_JAR = path.join(GAME_DIR, "versions", "1.8.9", "1.8.9.jar");

if (!fs.existsSync(APP_DIR)) fs.mkdirSync(APP_DIR, { recursive: true });

// Event Subscribers for SSE
const sseClients = new Set();
function emitEvent(event, payload) {
  const msg = `data: ${JSON.stringify({ event, payload })}\n\n`;
  for (const client of sseClients) {
    try {
      client.write(msg);
    } catch (e) {
      sseClients.delete(client);
    }
  }
}

// Config Helper
function loadConfig() {
  if (fs.existsSync(CONFIG_FILE)) {
    try {
      return JSON.parse(fs.readFileSync(CONFIG_FILE, "utf-8"));
    } catch (e) {}
  }
  const def = {
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
  };
  fs.writeFileSync(CONFIG_FILE, JSON.stringify(def, null, 2));
  return def;
}

function saveConfig(cfg) {
  fs.writeFileSync(CONFIG_FILE, JSON.stringify(cfg, null, 2));
}

// SLP Ping VarInt helpers
function writeVarInt(value) {
  const bytes = [];
  let v = value;
  while (true) {
    if ((v & ~0x7f) === 0) {
      bytes.push(v);
      return Buffer.from(bytes);
    }
    bytes.push((v & 0x7f) | 0x80);
    v >>>= 7;
  }
}

function readVarInt(buffer, offset = 0) {
  let result = 0;
  let numRead = 0;
  let currentOffset = offset;
  while (true) {
    if (currentOffset >= buffer.length) break;
    const byte = buffer[currentOffset++];
    const value = byte & 0x7f;
    result |= value << (7 * numRead);
    numRead++;
    if (numRead > 5) throw new Error("VarInt too big");
    if ((byte & 0x80) === 0) break;
  }
  return { value: result, bytesRead: currentOffset - offset };
}

// Server List Ping implementation
function pingMinecraftServer(host, port) {
  return new Promise((resolve) => {
    const startTime = Date.now();
    const socket = new net.Socket();
    let responded = false;
    socket.setTimeout(2500);

    socket.connect(port, host, () => {
      const hostBuf = Buffer.from(host, "utf-8");
      const portBuf = Buffer.alloc(2);
      portBuf.writeUInt16BE(port);

      const handshakeData = Buffer.concat([
        writeVarInt(0x00),
        writeVarInt(47),
        writeVarInt(hostBuf.length),
        hostBuf,
        portBuf,
        writeVarInt(1),
      ]);

      const handshakePacket = Buffer.concat([
        writeVarInt(handshakeData.length),
        handshakeData,
      ]);

      const requestPacket = Buffer.from([0x01, 0x00]);
      socket.write(Buffer.concat([handshakePacket, requestPacket]));
    });

    let buffer = Buffer.alloc(0);
    socket.on("data", (chunk) => {
      buffer = Buffer.concat([buffer, chunk]);
      try {
        const pLen = readVarInt(buffer, 0);
        const pId = readVarInt(buffer, pLen.bytesRead);
        if (pId.value === 0x00) {
          const sLen = readVarInt(buffer, pLen.bytesRead + pId.bytesRead);
          const jsonOffset = pLen.bytesRead + pId.bytesRead + sLen.bytesRead;
          const jsonBuf = buffer.subarray(jsonOffset, jsonOffset + sLen.value);

          if (jsonBuf.length >= sLen.value) {
            const jsonStr = jsonBuf.toString("utf-8");
            const data = JSON.parse(jsonStr);
            const latency = Date.now() - startTime;
            responded = true;
            socket.destroy();

            let motd = "Etherial 1.8.9 Competitive Server";
            if (typeof data.description === "string") {
              motd = data.description;
            } else if (data.description?.text) {
              motd = data.description.text;
            }
            motd = motd.replace(/§[0-9a-fk-or]/gi, "").trim();

            resolve({
              online: true,
              latency_ms: latency,
              players_online: data.players?.online || 0,
              players_max: data.players?.max || 0,
              motd: motd || "Etherial Competitive Server",
              version: data.version?.name || "1.8.9",
            });
          }
        }
      } catch (err) {}
    });

    const fallback = () => {
      if (!responded) {
        responded = true;
        socket.destroy();
        resolve({
          online: false,
          latency_ms: 0,
          players_online: 0,
          players_max: 0,
          motd: "Sunucu Kapalı (Çevrimdışı)",
          version: "1.8.9",
        });
      }
    };

    socket.on("timeout", fallback);
    socket.on("error", fallback);
  });
}

// Download Helper
function computeSha1(filePath) {
  if (!fs.existsSync(filePath)) return null;
  const hash = crypto.createHash("sha1");
  const data = fs.readFileSync(filePath);
  hash.update(data);
  return hash.digest("hex");
}

function downloadFile(url, dest, expectedSha1) {
  return new Promise((resolve, reject) => {
    if (expectedSha1 && computeSha1(dest) === expectedSha1.toLowerCase()) {
      return resolve(false);
    }
    const dir = path.dirname(dest);
    if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });

    const file = fs.createWriteStream(dest);
    const client = url.startsWith("https") ? https : http;

    client.get(url, (res) => {
      if (res.statusCode >= 300 && res.statusCode < 400 && res.headers.location) {
        return downloadFile(res.headers.location, dest, expectedSha1).then(resolve).catch(reject);
      }
      if (res.statusCode !== 200) {
        return reject(new Error(`Status ${res.statusCode} on ${url}`));
      }
      res.pipe(file);
      file.on("finish", () => {
        file.close(() => resolve(true));
      });
    }).on("error", (err) => {
      fs.unlink(dest, () => {});
      reject(err);
    });
  });
}

function fetchJson(url) {
  return new Promise((resolve, reject) => {
    https.get(url, (res) => {
      let data = "";
      res.on("data", (c) => (data += c));
      res.on("end", () => {
        try {
          resolve(JSON.parse(data));
        } catch (e) {
          reject(e);
        }
      });
    }).on("error", reject);
  });
}

function generateOfflineUuid(username) {
  const hash = crypto.createHash("md5").update(`OfflinePlayer:${username}`).digest();
  hash[6] = (hash[6] & 0x0f) | 0x30;
  hash[8] = (hash[8] & 0x3f) | 0x80;
  return [
    hash.subarray(0, 4).toString("hex"),
    hash.subarray(4, 6).toString("hex"),
    hash.subarray(6, 8).toString("hex"),
    hash.subarray(8, 10).toString("hex"),
    hash.subarray(10, 16).toString("hex"),
  ].join("-");
}

// Full Minecraft Launch Sequence
async function startMinecraftLaunch() {
  const config = loadConfig();
  if (!fs.existsSync(GAME_DIR)) fs.mkdirSync(GAME_DIR, { recursive: true });
  if (!fs.existsSync(NATIVES_DIR)) fs.mkdirSync(NATIVES_DIR, { recursive: true });
  if (!fs.existsSync(LIBRARIES_DIR)) fs.mkdirSync(LIBRARIES_DIR, { recursive: true });

  emitEvent("download_progress", {
    step: "Mojang 1.8.9 Manifest Doğrulanıyor...",
    current_file: "1.8.9.json",
    completed_items: 0,
    total_items: 1,
    downloaded_bytes: 0,
    total_bytes: 1,
    speed_mb_s: 0.0,
    percentage: 1.0,
  });

  const manifest = await fetchJson(MANIFEST_URL);

  emitEvent("download_progress", {
    step: "Client JAR ve Kütüphane Bütünlüğü (SHA-1) Kontrolü...",
    current_file: "client.jar",
    completed_items: 1,
    total_items: manifest.libraries.length + 1,
    downloaded_bytes: 0,
    total_bytes: manifest.downloads.client.size,
    speed_mb_s: 0.0,
    percentage: 5.0,
  });

  await downloadFile(manifest.downloads.client.url, CLIENT_JAR, manifest.downloads.client.sha1);

  const classpathJars = [];
  const nativeJars = [];

  let completed = 0;
  const total = manifest.libraries.length;

  for (const lib of manifest.libraries) {
    let allowed = true;
    if (lib.rules) {
      allowed = false;
      for (const rule of lib.rules) {
        if (!rule.os || rule.os.name === "windows") {
          allowed = rule.action === "allow";
        }
      }
    }
    if (!allowed) continue;

    if (lib.downloads?.artifact) {
      const art = lib.downloads.artifact;
      const dest = path.join(LIBRARIES_DIR, art.path);
      const isDownloaded = await downloadFile(art.url, dest, art.sha1);
      classpathJars.push(dest);
    }

    if (lib.natives?.windows && lib.downloads?.classifiers) {
      const natKey = lib.natives.windows;
      const natArt = lib.downloads.classifiers[natKey];
      if (natArt) {
        const dest = path.join(LIBRARIES_DIR, natArt.path);
        await downloadFile(natArt.url, dest, natArt.sha1);
        nativeJars.push(dest);
      }
    }

    completed++;
    const percent = 5.0 + (completed / total) * 85.0;
    emitEvent("download_progress", {
      step: "Kütüphaneler Doğrulanıyor...",
      current_file: lib.name,
      completed_items: completed,
      total_items: total,
      downloaded_bytes: completed * 50000,
      total_bytes: total * 50000,
      speed_mb_s: 14.5,
      percentage: percent,
    });
  }

  classpathJars.push(CLIENT_JAR);

  // Extract Natives
  emitEvent("download_progress", {
    step: "LWJGL 2.9.x Windows Natives DLL Ayıklanıyor...",
    current_file: "lwjgl64.dll & OpenAL32.dll",
    completed_items: total,
    total_items: total,
    downloaded_bytes: total * 50000,
    total_bytes: total * 50000,
    speed_mb_s: 0.0,
    percentage: 95.0,
  });

  for (const jar of nativeJars) {
    if (fs.existsSync(jar)) {
      try {
        await new Promise((res) => {
          const p = spawn("powershell", ["-Command", `Set-Location "${NATIVES_DIR}"; & jar xf "${jar}"`], { stdio: "ignore" });
          p.on("close", res);
        });
      } catch (e) {}
    }
  }

  const metaInf = path.join(NATIVES_DIR, "META-INF");
  if (fs.existsSync(metaInf)) {
    fs.rmSync(metaInf, { recursive: true, force: true });
  }

  emitEvent("download_progress", {
    step: "Client Başlatılıyor!",
    current_file: "net.minecraft.client.main.Main",
    completed_items: total,
    total_items: total,
    downloaded_bytes: total * 50000,
    total_bytes: total * 50000,
    speed_mb_s: 0.0,
    percentage: 100.0,
  });

  // Launch JVM Process
  const cpArg = classpathJars.join(";");
  const username = config.username || "EtherialPlayer";
  const targetHost = config.server_host || "localhost";
  const targetPort = String(config.server_port || 25565);
  const uuid = generateOfflineUuid(username);
  const agentPath = path.resolve("agent/target/etherial-agent-1.0.0.jar");

  const jvmArgs = [
    `-Xms${config.min_ram_mb}M`,
    `-Xmx${config.max_ram_mb}M`,
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
    `-Djava.library.path=${NATIVES_DIR}`,
  ];

  if (config.auto_connect_method === "java_agent" && fs.existsSync(agentPath)) {
    jvmArgs.push(`-javaagent:${agentPath}=${targetHost}:${targetPort}`);
  }

  jvmArgs.push("-cp", cpArg);
  jvmArgs.push("net.minecraft.client.main.Main");

  const gameArgs = [
    "--username", username,
    "--version", "1.8.9",
    "--gameDir", GAME_DIR,
    "--assetsDir", path.join(GAME_DIR, "assets"),
    "--assetIndex", "1.8",
    "--uuid", uuid,
    "--accessToken", "00000000-0000-0000-0000-000000000000",
    "--userProperties", "{}",
    "--userType", "legacy",
    "--width", String(config.resolution_width || 1280),
    "--height", String(config.resolution_height || 720),
  ];

  if (config.fullscreen) gameArgs.push("--fullscreen");

  if (config.auto_connect_method === "direct_cli") {
    gameArgs.push("--server", targetHost, "--port", targetPort);
  }

  const fullArgs = [...jvmArgs, ...gameArgs];

  console.log(`[Spawn] java.exe ${fullArgs.slice(0, 10).join(" ")}...`);
  console.log(`[Target Server] ${targetHost}:${targetPort}`);

  const mc = spawn("java", fullArgs, {
    cwd: GAME_DIR,
    detached: true,
    stdio: "ignore",
  });
  mc.unref();

  return `Minecraft 1.8.9 başarıyla başlatıldı (PID: ${mc.pid})`;
}

// HTTP Server Serving Frontend & APIs
const mimeTypes = {
  ".html": "text/html",
  ".js": "text/javascript",
  ".css": "text/css",
  ".json": "application/json",
  ".png": "image/png",
  ".svg": "image/svg+xml",
  ".ico": "image/x-icon",
};

const server = http.createServer(async (req, res) => {
  // CORS
  res.setHeader("Access-Control-Allow-Origin", "*");
  res.setHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
  res.setHeader("Access-Control-Allow-Headers", "Content-Type");

  if (req.method === "OPTIONS") {
    res.writeHead(200);
    return res.end();
  }

  // SSE Stream
  if (req.url === "/api/events") {
    res.writeHead(200, {
      "Content-Type": "text/event-stream",
      "Cache-Control": "no-cache",
      Connection: "keep-alive",
    });
    sseClients.add(res);
    req.on("close", () => sseClients.delete(res));
    return;
  }

  // API Invocations
  if (req.url.startsWith("/api/invoke/")) {
    const cmd = req.url.replace("/api/invoke/", "");
    let body = "";
    req.on("data", (chunk) => (body += chunk));
    req.on("end", async () => {
      let args = {};
      try {
        if (body) args = JSON.parse(body);
      } catch (e) {}

      try {
        let result = null;
        if (cmd === "get_launcher_config") {
          result = loadConfig();
        } else if (cmd === "save_launcher_config") {
          if (args.config) saveConfig(args.config);
          result = null;
        } else if (cmd === "ping_server") {
          const host = args.host || "localhost";
          const port = args.port || 25565;
          result = await pingMinecraftServer(host, port);
        } else if (cmd === "get_system_specs") {
          const totalRam = Math.round(os.totalmem() / (1024 * 1024));
          const freeRam = Math.round(os.freemem() / (1024 * 1024));
          result = {
            total_ram_mb: totalRam,
            available_ram_mb: freeRam,
            cpu_cores: os.cpus().length,
            os_name: os.platform(),
          };
        } else if (cmd === "start_game") {
          result = await startMinecraftLaunch();
        } else if (cmd === "plugin:event|listen") {
          result = 1;
        } else if (cmd === "plugin:event|unlisten") {
          result = null;
        }

        res.writeHead(200, { "Content-Type": "application/json" });
        res.end(JSON.stringify(result));
      } catch (err) {
        res.writeHead(500, { "Content-Type": "application/json" });
        res.end(JSON.stringify({ error: err.message || String(err) }));
      }
    });
    return;
  }

  // Serve Static Frontend from dist/
  let filePath = path.join("dist", req.url === "/" ? "index.html" : req.url);
  if (!fs.existsSync(filePath)) {
    filePath = path.join("dist", "index.html");
  }

  const ext = path.extname(filePath);
  const contentType = mimeTypes[ext] || "text/plain";

  fs.readFile(filePath, (err, content) => {
    if (err) {
      res.writeHead(404);
      res.end("Not Found");
    } else {
      res.writeHead(200, { "Content-Type": contentType });
      res.end(content);
    }
  });
});

server.listen(PORT, () => {
  console.log(`[Etherial Backend] HTTP & IPC Engine listening on http://localhost:${PORT}`);
});
