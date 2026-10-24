import fs from "fs";
import path from "path";
import https from "https";
import http from "http";
import crypto from "crypto";
import { spawn } from "child_process";
import unzipper from "zlib";

const MANIFEST_URL = "https://piston-meta.mojang.com/v1/packages/d546f1707a3f2b7d034eece5ea2e311eda875787/1.8.9.json";
const APP_DIR = path.join(process.env.APPDATA || ".", ".etherial");
const GAME_DIR = path.join(APP_DIR, "game");
const NATIVES_DIR = path.join(GAME_DIR, "versions", "1.8.9", "1.8.9-natives");
const LIBRARIES_DIR = path.join(GAME_DIR, "libraries");
const CLIENT_JAR = path.join(GAME_DIR, "versions", "1.8.9", "1.8.9.jar");

console.log("==================================================");
console.log("  ETHERIAL CLIENT 1.8.9 CLI LAUNCH ENGINE         ");
console.log("==================================================");
console.log(`[Config] Game Dir: ${GAME_DIR}`);

function ensureDir(dir) {
  if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
}

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
      return resolve(false); // already up to date
    }
    ensureDir(path.dirname(dest));
    const file = fs.createWriteStream(dest);
    const client = url.startsWith("https") ? https : http;

    client.get(url, (res) => {
      if (res.statusCode >= 300 && res.statusCode < 400 && res.headers.location) {
        return downloadFile(res.headers.location, dest, expectedSha1).then(resolve).catch(reject);
      }
      if (res.statusCode !== 200) {
        return reject(new Error(`Failed to download ${url}: status ${res.statusCode}`));
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
      res.on("data", (chunk) => (data += chunk));
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

async function start() {
  ensureDir(GAME_DIR);
  ensureDir(NATIVES_DIR);
  ensureDir(LIBRARIES_DIR);

  console.log("\n[1/4] Mojang 1.8.9 Manifest alınıyor...");
  const manifest = await fetchJson(MANIFEST_URL);

  console.log("\n[2/4] Client JAR ve Kütüphaneler kontrol ediliyor...");
  await downloadFile(manifest.downloads.client.url, CLIENT_JAR, manifest.downloads.client.sha1);

  const classpathJars = [];
  const nativeJars = [];

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
      await downloadFile(art.url, dest, art.sha1);
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
  }
  classpathJars.push(CLIENT_JAR);

  console.log(`[OK] Toplam ${classpathJars.length} kütüphane doğrulandı.`);

  // 3. Extract natives
  console.log("\n[3/4] LWJGL 2.9.x Windows DLL Natives klasörüne ayıklanıyor...");
  // Use PowerShell expand or jar xf for natives
  for (const jar of nativeJars) {
    if (fs.existsSync(jar)) {
      try {
        const cmd = `& jar xf "${jar}"`;
        await new Promise((res) => {
          const p = spawn("powershell", ["-Command", `Set-Location "${NATIVES_DIR}"; & jar xf "${jar}"`], { stdio: "ignore" });
          p.on("close", res);
        });
      } catch (e) {}
    }
  }

  // Remove META-INF from natives dir if present
  const metaInf = path.join(NATIVES_DIR, "META-INF");
  if (fs.existsSync(metaInf)) {
    fs.rmSync(metaInf, { recursive: true, force: true });
  }

  // 4. Build JVM Args and Launch
  console.log("\n[4/4] JVM Argümanları Hazırlanıyor ve Minecraft Başlatılıyor...");
  const cpArg = classpathJars.join(";");
  const username = process.argv[2] || "EtherialPlayer";
  const targetHost = process.argv[3] || "localhost";
  const targetPort = process.argv[4] || "25565";
  const uuid = generateOfflineUuid(username);
  const agentPath = path.resolve("agent/target/etherial-agent-1.0.0.jar");

  const jvmArgs = [
    "-Xms2048M",
    "-Xmx4096M",
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

  if (fs.existsSync(agentPath)) {
    console.log(`[JavaAgent] Companion Hook Aktif: ${agentPath}`);
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
    "--width", "1280",
    "--height", "720",
    "--server", targetHost,
    "--port", targetPort
  ];

  const fullArgs = [...jvmArgs, ...gameArgs];

  console.log(`\n[Spawn] java.exe ${fullArgs.slice(0, 15).join(" ")} ... [truncated]`);
  console.log(`[Status] Doğrudan Bağlantı: ${targetHost}:${targetPort}`);
  console.log(`[Status] Oyuncu: ${username} (UUID: ${uuid})`);

  const mc = spawn("java", fullArgs, {
    cwd: GAME_DIR,
    stdio: "inherit"
  });

  mc.on("close", (code) => {
    console.log(`\n[Exit] Minecraft istemcisi kapandı (Exit Code: ${code})`);
  });
}

start().catch(console.error);
