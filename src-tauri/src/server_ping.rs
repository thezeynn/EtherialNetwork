use serde::{Deserialize, Serialize};
use std::io::{Read, Write};
use std::net::TcpStream;
use std::time::{Duration, Instant};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ServerStatusResponse {
    pub online: bool,
    pub latency_ms: u64,
    pub players_online: u32,
    pub players_max: u32,
    pub motd: String,
    pub version: String,
}

fn write_varint(buffer: &mut Vec<u8>, mut val: i32) {
    loop {
        let mut temp = (val & 0b0111_1111) as u8;
        val = ((val as u32) >> 7) as i32;
        if val != 0 {
            temp |= 0b1000_0000;
        }
        buffer.push(temp);
        if val == 0 {
            break;
        }
    }
}

fn read_varint<R: Read>(reader: &mut R) -> Result<i32, String> {
    let mut num_read = 0;
    let mut result = 0i32;
    let mut byte = [0u8; 1];

    loop {
        reader.read_exact(&mut byte).map_err(|e| e.to_string())?;
        let value = (byte[0] & 0b0111_1111) as i32;
        result |= value << (7 * num_read);

        num_read += 1;
        if num_read > 5 {
            return Err("VarInt is too big".to_string());
        }

        if (byte[0] & 0b1000_0000) == 0 {
            break;
        }
    }

    Ok(result)
}

pub fn ping_minecraft_server(host: &str, port: u16) -> ServerStatusResponse {
    let start_time = Instant::now();
    let addr = format!("{}:{}", host, port);

    let stream = match TcpStream::connect_timeout(
        &addr.parse().unwrap_or_else(|_| {
            use std::net::ToSocketAddrs;
            addr.to_socket_addrs()
                .ok()
                .and_then(|mut iter| iter.next())
                .unwrap_or_else(|| "127.0.0.1:25565".parse().unwrap())
        }),
        Duration::from_millis(2500),
    ) {
        Ok(s) => s,
        Err(_) => {
            return ServerStatusResponse {
                online: false,
                latency_ms: 0,
                players_online: 0,
                players_max: 0,
                motd: "Sunucuya bağlanılamadı (Çevrimdışı)".to_string(),
                version: "1.8.9".to_string(),
            };
        }
    };

    let _ = stream.set_read_timeout(Some(Duration::from_millis(3000)));
    let _ = stream.set_write_timeout(Some(Duration::from_millis(3000)));
    let mut stream = stream;

    // 1. Handshake Packet (ID: 0x00)
    // Protocol: 47 (Minecraft 1.8.x)
    let mut handshake = Vec::new();
    write_varint(&mut handshake, 0x00); // Packet ID
    write_varint(&mut handshake, 47);   // Protocol Version 1.8.x
    // Hostname string: length + bytes
    write_varint(&mut handshake, host.len() as i32);
    handshake.extend_from_slice(host.as_bytes());
    // Port: unsigned short big-endian
    handshake.extend_from_slice(&port.to_be_bytes());
    // Next State: 1 (Status)
    write_varint(&mut handshake, 1);

    // Frame handshake packet with its length
    let mut packet = Vec::new();
    write_varint(&mut packet, handshake.len() as i32);
    packet.extend(handshake);

    if stream.write_all(&packet).is_err() {
        return ServerStatusResponse {
            online: false,
            latency_ms: 0,
            players_online: 0,
            players_max: 0,
            motd: "Handshake gönderilemedi".to_string(),
            version: "1.8.9".to_string(),
        };
    }

    // 2. Status Request Packet (Length 1, ID 0x00)
    let status_request = [0x01, 0x00];
    if stream.write_all(&status_request).is_err() {
        return ServerStatusResponse {
            online: false,
            latency_ms: 0,
            players_online: 0,
            players_max: 0,
            motd: "Durum isteği gönderilemedi".to_string(),
            version: "1.8.9".to_string(),
        };
    }

    let latency = start_time.elapsed().as_millis() as u64;

    // 3. Status Response
    let _packet_len = match read_varint(&mut stream) {
        Ok(len) => len,
        Err(_) => return offline_fallback(),
    };

    let packet_id = match read_varint(&mut stream) {
        Ok(id) => id,
        Err(_) => return offline_fallback(),
    };

    if packet_id != 0x00 {
        return offline_fallback();
    }

    let string_len = match read_varint(&mut stream) {
        Ok(len) => len as usize,
        Err(_) => return offline_fallback(),
    };

    let mut json_bytes = vec![0u8; string_len];
    if stream.read_exact(&mut json_bytes).is_err() {
        return offline_fallback();
    }

    let json_str = match String::from_utf8(json_bytes) {
        Ok(s) => s,
        Err(_) => return offline_fallback(),
    };

    // Parse JSON
    if let Ok(val) = serde_json::from_str::<serde_json::Value>(&json_str) {
        let players_online = val["players"]["online"].as_u64().unwrap_or(0) as u32;
        let players_max = val["players"]["max"].as_u64().unwrap_or(0) as u32;
        let version_name = val["version"]["name"].as_str().unwrap_or("1.8.9").to_string();

        let motd = if let Some(m) = val["description"].as_str() {
            m.to_string()
        } else if let Some(text) = val["description"]["text"].as_str() {
            text.to_string()
        } else {
            "Etherial Network 1.8.9".to_string()
        };

        // Strip minecraft formatting codes (§a, §c, etc.) for clean UI display
        let clean_motd = regex::Regex::new(r"§[0-9a-fk-or]")
            .unwrap()
            .replace_all(&motd, "")
            .trim()
            .to_string();

        return ServerStatusResponse {
            online: true,
            latency_ms: latency,
            players_online,
            players_max,
            motd: if clean_motd.is_empty() { "Etherial Competitive Server".to_string() } else { clean_motd },
            version: version_name,
        };
    }

    offline_fallback()
}

fn offline_fallback() -> ServerStatusResponse {
    ServerStatusResponse {
        online: false,
        latency_ms: 0,
        players_online: 0,
        players_max: 0,
        motd: "Sunucu Kapalı".to_string(),
        version: "1.8.9".to_string(),
    }
}
