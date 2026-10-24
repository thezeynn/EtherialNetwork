# ETHERIAL CLIENT LAUNCHER
### Next-Gen 1.8.9 Competitive Minecraft Launcher (CraftRise / SonOyuncu / Badlion Architecture)

Etherial Launcher; bellek tüketimini minimumda tutan Rust (Tauri v2) çekirdeği, akıcı 60+ FPS Dark Cyber arayüzü ve Minecraft 1.8.9 istemcisini ana menüyü (GuiMainMenu) atlayarak doğrudan sunucuya bağlayan özel motoruyla sıfır hata toleranslı olarak geliştirilmiştir.

---

## 📁 Proje Dizin Yapısı

```
Etherial Launcher/
├── Cargo.toml                          # Root Rust Workspace tanımı
├── package.json                        # Vite, React 18, Tailwind CSS konfigürasyonu
├── vite.config.ts                      # Frontend derleme ayarları
├── tailwind.config.js                  # Dark Cyber neon tema ve efektleri
├── src/                                # Frontend Kaynak Kodları (React + TypeScript)
│   ├── index.html                      # HTML5 Giriş Noktası
│   ├── main.tsx                        # React Root mount
│   ├── App.tsx                         # Ana Launcher UI ve IPC Event Listener
│   ├── types.ts                        # Rust ile paylaşılan veri tipleri
│   ├── index.css                       # Özel cam efekti (glassmorphism) ve neon glow CSS
│   └── components/
│       ├── TitleBar.tsx                # Çerçevesiz (Frameless) başlık çubuğu & kontroller
│       ├── ServerStatus.tsx            # Canlı SLP ping, oyuncu sayısı ve MOTD kartı
│       ├── PlaySection.tsx             # Oyuncu adı, skin başlığı ve büyük Neon Oyna butonu
│       ├── ProgressBar.tsx             # Gerçek zamanlı indirme hızı (MB/s) ve dosya takibi
│       └── SettingsModal.tsx           # RAM seçici slider, çözünürlük, JRE & IP ayarları
├── src-tauri/                          # Native Rust Arka Yüzü (Tauri v2)
│   ├── Cargo.toml                      # Tokio, reqwest, sha1, zip bağımlılıkları
│   ├── tauri.conf.json                 # Tauri v2 pencere ve güvenlik ayarları
│   ├── build.rs                        # Tauri build script
│   └── src/
│       ├── main.rs                     # Tauri Handler & Plugin başlatıcı
│       ├── commands.rs                 # Frontend <-> Rust köprü komutları
│       ├── config.rs                   # Konfigürasyon okuma / yazma (%APPDATA%/.etherial)
│       ├── server_ping.rs              # Minecraft 1.8.x SLP TCP Ping protokol motoru
│       ├── java_manager.rs             # Azul Zulu 8 JRE otomatik tespiti ve indirmesi
│       ├── mojang/
│       │   ├── mod.rs
│       │   ├── manifest.rs             # 1.8.9 JSON manifest, kütüphaneler ve natives modelleri
│       │   └── downloader.rs           # Multi-threaded (32-worker) SHA-1 hash doğrulayıcı & indirici
│       └── launcher/
│           ├── mod.rs
│           ├── classpath.rs            # LWJGL 2.9.x & 1.8.9 classpath oluşturucu
│           ├── natives.rs              # Windows DLL ayıklama ve doğrulama
│           └── process.rs              # Aikar/G1GC parametreleri & Minecraft başlatma motoru
└── agent/                              # JavaAgent & Bytecode Hook Modülü
    ├── pom.xml                         # Maven ASM 9.6 & Shade Plugin konfigürasyonu
    └── src/main/java/com/etherial/agent/
        ├── EtherialAgent.java          # Premain JavaAgent giriş noktası
        ├── MinecraftTransformer.java   # ASM Bytecode Transformer (displayGuiScreen hook)
        └── AutoConnectBridge.java      # GuiMainMenu tespiti ve GuiConnecting nesnesi yönlendirmesi
```

---

## ⚡ 1. Auto-Connect (Doğrudan Bağlantı) Mekanizması

Minecraft 1.8.9'da `--quickPlayMultiplayer` vanilla parametresi bulunmaz. İki alternatif yöntem mevcuttur:

### Yöntem A: Vanilla CLI Enjeksiyonu (`--server <host> --port <port>`)
- **Nasıl Çalışır:** Minecraft 1.8.9'un `net.minecraft.client.main.Main` sınıfı JOpt-Simple argüman ayrıştırıcısında `--server` ve `--port` argümanlarını destekler. Bu argümanlar verildiğinde `Minecraft.setServer(host, port)` çağrılır ve oyun başlarken `GuiMainMenu` yerine `GuiConnecting` nesnesi açılır.
- **Avantajı:** İstemci JAR dosyasında veya Java kodunda hiçbir değişiklik gerektirmez. SHA-1 doğrulaması %100 orijinal kalır.
- **Dezavantajı:** Oyuncu sunucudan atıldığında veya bağlantı koptuğunda vanilla "Sunucu Listesine Dön" butonu üzerinden tek oyunculu / çok oyunculu menüye düşebilir.

### Yöntem B: Bytecode Companion JavaAgent (CraftRise / SonOyuncu Standartı)
- **Nasıl Çalışır:** JVM başlatılırken `-javaagent:etherial-agent.jar=localhost:25565` parametresi eklenir. `MinecraftTransformer`, `Minecraft` sınıfının `displayGuiScreen(GuiScreen)` metoduna ASM ile hook atar. İlk `GuiMainMenu` ekranı açılacağı an araya girerek doğrudan `new GuiConnecting(null, mc, new ServerAddress(host, port))` nesnesini çalıştırır.
- **Kararlılık Kararı:** En kararlı ve kurumsal yöntem **Yöntem B**'dir; çünkü oyuncunun hiçbir şekilde sunucu listesine veya ana menüye erişmesine izin vermez, tam bir özel istemci (Custom Client) deneyimi sağlar. Projemizde hem Yöntem A hem de Yöntem B tam çalışır şekilde entegre edilmiştir.

---

## 🚀 2. JVM ve G1GC Optimizasyon Parametreleri

1.8.9 ve Java 8 HotSpot için LWJGL bellek sızıntılarını ve GC takılmalarını (stuttering) sıfıra indiren parametreler:

```bash
-Xms2048M -Xmx4096M \
-XX:+UseG1GC \
-XX:+ParallelRefProcEnabled \
-XX:MaxGCPauseMillis=20 \
-XX:+UnlockExperimentalVMOptions \
-XX:+DisableExplicitGC \
-XX:+AlwaysPreTouch \
-XX:G1NewSizePercent=30 \
-XX:G1MaxNewSizePercent=40 \
-XX:G1ReservePercent=15 \
-XX:G1HeapWastePercent=5 \
-XX:G1MixedGCCountTarget=4 \
-XX:InitiatingHeapOccupancyPercent=15 \
-XX:G1MixedGCLiveThresholdPercent=90 \
-XX:G1RSetUpdatingPauseTimePercent=5 \
-XX:SurvivorRatio=32 \
-XX:+PerfDisableSharedMem \
-XX:MaxTenuringThreshold=1 \
-Dsun.rmi.dgc.server.gcInterval=2147483646
```

---

## 🛠️ 3. Derleme ve Çalıştırma

1. **Frontend Bağımlılıkları ve Geliştirme:**
   ```bash
   npm install
   npm run dev
   ```

2. **JavaAgent Derlemesi:**
   ```bash
   cd agent
   mvn clean package
   # Çıktı: agent/target/etherial-agent-1.0.0.jar
   ```

3. **Tauri v2 Uygulamasını Başlatma:**
   ```bash
   npm run tauri dev
   ```
