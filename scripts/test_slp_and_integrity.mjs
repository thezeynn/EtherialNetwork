import net from "net";
import https from "https";
import crypto from "crypto";
import fs from "fs";
import path from "path";

console.log("==================================================");
console.log("  ETHERIAL 1.8.9 CLIENT ENGINE DIAGNOSTIC TEST    ");
console.log("==================================================");

// 1. Test Mojang Manifest Fetch & Verification
const MANIFEST_URL = "https://piston-meta.mojang.com/v1/packages/d546f1707a3f2b7d034eece5ea2e311eda875787/1.8.9.json";

function getJson(url) {
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

async function runDiagnostics() {
  console.log("\n[1/3] Mojang 1.8.9 Version Manifest Bağlantısı Test Ediliyor...");
  try {
    const manifest = await getJson(MANIFEST_URL);
    console.log(`[OK] Manifest başarıyla alındı. Sürüm ID: ${manifest.id}`);
    console.log(`[OK] Client JAR URL: ${manifest.downloads.client.url}`);
    console.log(`[OK] Client JAR SHA-1: ${manifest.downloads.client.sha1}`);
    console.log(`[OK] Kütüphane sayısı: ${manifest.libraries.length}`);
    console.log(`[OK] Asset Index ID: ${manifest.assetIndex.id}`);

    // Verify LWJGL Windows classifier presence
    const lwjglLib = manifest.libraries.find((l) => l.name.includes("lwjgl-platform"));
    if (lwjglLib && lwjglLib.downloads && lwjglLib.downloads.classifiers) {
      console.log(`[OK] LWJGL 2.9.x Windows Native artifact doğrulandı:`, 
        lwjglLib.downloads.classifiers["natives-windows"]?.url);
    }
  } catch (err) {
    console.error(`[HATA] Manifest alınamadı:`, err.message);
  }

  // 2. Test Server List Ping (SLP) Protocol against localhost:25565
  console.log("\n[2/3] Localhost Minecraft Server Ping (SLP) Test Ediliyor...");
  testMinecraftPing("127.0.0.1", 25565);
}

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

function testMinecraftPing(host, port) {
  const startTime = Date.now();
  const client = new net.Socket();
  client.setTimeout(2500);

  client.connect(port, host, () => {
    const latency = Date.now() - startTime;
    console.log(`[OK] ${host}:${port} portuna TCP bağlantısı sağlandı (${latency}ms).`);

    // Handshake packet (0x00, protocol 47 for 1.8.x, host, port, nextState 1)
    const hostBuf = Buffer.from(host, "utf-8");
    const portBuf = Buffer.alloc(2);
    portBuf.writeUInt16BE(port);

    const handshakeData = Buffer.concat([
      writeVarInt(0x00),
      writeVarInt(47),
      writeVarInt(hostBuf.length),
      hostBuf,
      portBuf,
      writeVarInt(1)
    ]);

    const handshakePacket = Buffer.concat([
      writeVarInt(handshakeData.length),
      handshakeData
    ]);

    // Request packet (0x00)
    const requestPacket = Buffer.from([0x01, 0x00]);

    client.write(Buffer.concat([handshakePacket, requestPacket]));
  });

  client.on("data", (data) => {
    console.log(`[OK] Sunucudan SLP yanıtı alındı! Veri boyutu: ${data.length} byte.`);
    client.destroy();
  });

  client.on("timeout", () => {
    console.log(`[BİLGİ] ${host}:${port} üzerinde aktif Minecraft sunucusu yanıt vermedi (Timeout). Bu beklenen bir durumdur.`);
    client.destroy();
  });

  client.on("error", (err) => {
    console.log(`[BİLGİ] Sunucu henüz çalışmıyor (${err.code}). Sunucu açıldığında canlı ping ve oyuncu sayısı görünecektir.`);
  });
}

runDiagnostics();
