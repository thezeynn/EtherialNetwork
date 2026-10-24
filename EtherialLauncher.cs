using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Markup;
using SPath = System.Windows.Shapes.Path;

namespace EtherialClient {
    public class LauncherConfig {
        public string Username { get; set; }
        public string SavedProfile { get; set; }
        public bool RememberProfile { get; set; }
        public List<string> SavedProfiles { get; set; }
        public int MinRamMb { get; set; }
        public int MaxRamMb { get; set; }
        public string ServerHost { get; set; }
        public int ServerPort { get; set; }
        public bool Fullscreen { get; set; }
        public int ResolutionWidth { get; set; }
        public int ResolutionHeight { get; set; }

        public LauncherConfig() {
            Username = "ZeyNN";
            SavedProfile = "ZeyNN";
            RememberProfile = true;
            SavedProfiles = new List<string> { "ZeyNN" };
            MinRamMb = 2048;
            MaxRamMb = 4096;
            ServerHost = "localhost";
            ServerPort = 25565;
            Fullscreen = false;
            ResolutionWidth = 1280;
            ResolutionHeight = 720;
        }
    }

    public class ServerPingResult {
        public bool Online { get; set; }
        public long LatencyMs { get; set; }
        public int PlayersOnline { get; set; }
        public int PlayersMax { get; set; }
        public string Motd { get; set; }
        public string Version { get; set; }

        public ServerPingResult() {
            Online = false;
            LatencyMs = 0;
            PlayersOnline = 0;
            PlayersMax = 0;
            Motd = "Sunucuya bağlanılamıyor...";
            Version = "1.8.9";
        }
    }

    // =========================================================================
    // 1. SPLASH / PRELOADER WINDOW (Modern Dark & Gold Opening Screen)
    // =========================================================================
    public class SplashWindow : Window {
        private ProgressBar splashProgress;
        private TextBlock txtSplashStatus;
        private TextBlock txtSplashPercent;

        public SplashWindow() {
            Title = "Etherial | Başlatılıyor...";
            Width = 480;
            Height = 310;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;

            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
            RenderOptions.SetClearTypeHint(this, ClearTypeHint.Enabled);
            FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI, Roboto, sans-serif");

            string iconFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(iconFile)) {
                try {
                    Icon = BitmapFrame.Create(new Uri(iconFile, UriKind.Absolute));
                } catch { }
            }

            CreateSplashUI();
            StartLoadingSequence();
        }

        private void CreateSplashUI() {
            Grid gutterGrid = new Grid {
                Margin = new Thickness(16),
                Background = Brushes.Transparent
            };

            Border mainBorder = new Border {
                CornerRadius = new CornerRadius(14),
                Background = new LinearGradientBrush(
                    Color.FromRgb(14, 18, 28),
                    Color.FromRgb(8, 10, 16),
                    45
                ),
                BorderBrush = new LinearGradientBrush {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection {
                        new GradientStop(Color.FromRgb(255, 238, 165), 0.0),
                        new GradientStop(Color.FromRgb(225, 185, 75), 0.25),
                        new GradientStop(Color.FromRgb(195, 150, 50), 0.50),
                        new GradientStop(Color.FromRgb(225, 185, 75), 0.75),
                        new GradientStop(Color.FromRgb(255, 238, 165), 1.0)
                    }
                },
                BorderThickness = new Thickness(1.4)
            };

            mainBorder.Effect = new DropShadowEffect {
                Color = Color.FromRgb(0, 0, 0),
                BlurRadius = 22,
                Opacity = 0.9,
                ShadowDepth = 4,
                Direction = 270
            };

            StackPanel rootStack = new StackPanel {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(24)
            };

            // Glowing Crest (Identical to Desktop Logo)
            Border crestBorder = new Border {
                Width = 56,
                Height = 56,
                CornerRadius = new CornerRadius(14),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 14)
            };
            crestBorder.Effect = new DropShadowEffect {
                Color = Color.FromRgb(218, 178, 62),
                BlurRadius = 22,
                Opacity = 0.6,
                ShadowDepth = 0
            };
            string splashLogoFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_logo.png");
            if (File.Exists(splashLogoFile)) {
                try {
                    BitmapImage sBi = new BitmapImage();
                    sBi.BeginInit();
                    sBi.UriSource = new Uri(splashLogoFile, UriKind.Absolute);
                    sBi.CacheOption = BitmapCacheOption.OnLoad;
                    sBi.EndInit();
                    Image sImg = new Image {
                        Source = sBi,
                        Width = 56,
                        Height = 56,
                        Stretch = Stretch.Uniform
                    };
                    RenderOptions.SetBitmapScalingMode(sImg, BitmapScalingMode.HighQuality);
                    crestBorder.Child = sImg;
                } catch { }
            }
            rootStack.Children.Add(crestBorder);

            // Title
            TextBlock title = new TextBlock {
                Text = "ETHERIAL",
                FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI, Roboto, sans-serif"),
                FontWeight = FontWeights.Bold,
                FontSize = 26,
                Foreground = new LinearGradientBrush(Color.FromRgb(255, 250, 235), Color.FromRgb(218, 178, 62), 90),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            rootStack.Children.Add(title);

            // Subtitle
            TextBlock sub = new TextBlock {
                Text = "YÜKSEK PERFORMANSLI OYUN İSTEMCİSİ",
                FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI, Roboto, sans-serif"),
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(197, 160, 89)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 20)
            };
            rootStack.Children.Add(sub);

            // Styled Glowing Progress Bar
            splashProgress = new ProgressBar {
                Width = 340,
                Height = 8,
                Value = 10,
                Background = new SolidColorBrush(Color.FromRgb(10, 14, 22)),
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            ControlTemplate spbTemplate = new ControlTemplate(typeof(ProgressBar));
            FrameworkElementFactory spbBorder = new FrameworkElementFactory(typeof(Border));
            spbBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            spbBorder.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(10, 14, 22)));
            spbBorder.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(55, 43, 26)));
            spbBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            spbBorder.SetValue(Border.ClipToBoundsProperty, true);

            FrameworkElementFactory spbIndicator = new FrameworkElementFactory(typeof(Border));
            spbIndicator.Name = "PART_Indicator";
            spbIndicator.SetValue(Border.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            spbIndicator.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
            spbIndicator.SetValue(Border.BackgroundProperty, new LinearGradientBrush(
                new GradientStopCollection {
                    new GradientStop(Color.FromRgb(218, 178, 62), 0.0),
                    new GradientStop(Color.FromRgb(250, 185, 35), 0.6),
                    new GradientStop(Color.FromRgb(255, 238, 160), 0.95),
                    new GradientStop(Color.FromRgb(255, 255, 255), 1.0)
                },
                new Point(0, 0),
                new Point(1, 0)
            ));
            spbBorder.AppendChild(spbIndicator);
            spbTemplate.VisualTree = spbBorder;
            splashProgress.Template = spbTemplate;

            rootStack.Children.Add(splashProgress);

            // Status and percent info
            Grid statusGrid = new Grid {
                Width = 340,
                Margin = new Thickness(0, 8, 0, 0)
            };
            txtSplashStatus = new TextBlock {
                Text = "İstemci bileşenleri yükleniyor...",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(170, 180, 195)),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            txtSplashPercent = new TextBlock {
                Text = "%15",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(218, 178, 62)),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            statusGrid.Children.Add(txtSplashStatus);
            statusGrid.Children.Add(txtSplashPercent);
            rootStack.Children.Add(statusGrid);

            mainBorder.Child = rootStack;
            gutterGrid.Children.Add(mainBorder);
            Content = gutterGrid;
        }

        private System.Windows.Threading.DispatcherTimer splashTimer;
        private int splashStep = 0;

        private void StartLoadingSequence() {
            splashTimer = new System.Windows.Threading.DispatcherTimer();
            splashTimer.Interval = TimeSpan.FromMilliseconds(250);
            splashTimer.Tick += SplashTimer_Tick;
            splashTimer.Start();
        }

        private void SplashTimer_Tick(object sender, EventArgs e) {
            try {
                splashStep++;
                if (splashStep == 1) {
                    UpdateProgress(35, "Azul Zulu 8 JRE ve G1GC zırhı doğrulanıyor...");
                } else if (splashStep == 2) {
                    UpdateProgress(65, "Oyun optimizasyonları ve Türkçe dil paketi hazırlanıyor...");
                } else if (splashStep == 3) {
                    UpdateProgress(90, "Sunucu durumu ve oyuncu profili eşitleniyor...");
                } else if (splashStep >= 4) {
                    UpdateProgress(100, "Etherial Client açılıyor...");
                    splashTimer.Stop();
                    Close();
                }
            } catch { }
        }

        private void UpdateProgress(int percent, string text) {
            splashProgress.Value = percent;
            txtSplashPercent.Text = "%" + percent;
            txtSplashStatus.Text = text;
        }
    }

    // =========================================================================
    // 2. MAIN APPLICATION WINDOW
    // =========================================================================
    public class MainWindow : Window {
        private LauncherConfig config;
        private string configPath;
        private string gameDir;
        private string nativesDir;
        private string librariesDir;
        private string clientJarPath;

        // Root Window Structure Elements (for dynamic Maximize/Restore transition)
        private Grid windowGutterGrid;
        private Border mainBorder;
        private Border topBarBorder;
        private Border bottomBar;

        // UI Components
        private TextBlock txtPingStatus;
        private TextBlock txtPlayerCount;
        private TextBlock txtMotd;
        private Ellipse statusDot;
        private Image imgAvatar;
        private ImageBrush avatarBrush;
        private TextBox txtUsername;
        private ProgressBar progressBar;
        private TextBlock txtStatus;
        private TextBlock txtProgressDetails;
        private Button btnPlay;
        private Button btnMax;
        private TextBlock txtRamBadge;
        private System.Windows.Threading.DispatcherTimer pingTimer;
        private string skinsCacheDir;
        private System.Windows.Threading.DispatcherTimer avatarDebounceTimer;

        // Theme Colors
        private readonly Color GoldPrimary = Color.FromRgb(218, 178, 62);      // #DAB23E Royal Gold
        private readonly Color GoldLight = Color.FromRgb(250, 185, 35);        // #FAB923 Radiant Amber
        private readonly Color BronzeBorder = Color.FromRgb(197, 160, 89);     // #C5A059 Ancient Bronze
        private readonly Color DarkBronze = Color.FromRgb(85, 68, 42);         // #55442A Muted Bronze
        private readonly Color VoidDark = Color.FromRgb(11, 14, 21);           // #0B0E15 Void Obsidian
        private readonly Color SlatePanel = Color.FromRgb(17, 23, 34);         // #111722 Slate Background

        public MainWindow() {
            try {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                ServicePointManager.DefaultConnectionLimit = 64;
            } catch { }

            InitConfig();
            SetupWindow();
            CreateUI();
            StartServerPingLoop();
        }

        private void InitConfig() {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string etherialDir = System.IO.Path.Combine(appData, ".etherial");
            if (!Directory.Exists(etherialDir)) Directory.CreateDirectory(etherialDir);

            configPath = System.IO.Path.Combine(etherialDir, "launcher_config.json");
            gameDir = System.IO.Path.Combine(etherialDir, "game");
            nativesDir = System.IO.Path.Combine(gameDir, "versions", "1.8.9", "1.8.9-natives");
            librariesDir = System.IO.Path.Combine(gameDir, "libraries");
            clientJarPath = System.IO.Path.Combine(gameDir, "versions", "1.8.9", "1.8.9.jar");
            skinsCacheDir = System.IO.Path.Combine(etherialDir, "cache", "skins");
            if (!Directory.Exists(skinsCacheDir)) Directory.CreateDirectory(skinsCacheDir);

            if (File.Exists(configPath)) {
                try {
                    string json = File.ReadAllText(configPath);
                    config = new JavaScriptSerializer().Deserialize<LauncherConfig>(json) ?? new LauncherConfig();
                } catch {
                    config = new LauncherConfig();
                }
            } else {
                config = new LauncherConfig();
                SaveConfig();
            }

            // Directly restore and display whichever profile was saved
            if (!string.IsNullOrWhiteSpace(config.SavedProfile)) {
                config.Username = config.SavedProfile;
            }

            // Ensure High-FPS Turkish options.txt is primed
            OptimizeGameSettings();
        }

        private void SaveConfig() {
            try {
                string json = new JavaScriptSerializer().Serialize(config);
                File.WriteAllText(configPath, json);
            } catch { }
        }

        private void SetupWindow() {
            Title = "Etherial Client";
            Width = 1032;
            Height = 652;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.CanResize;

            // ABSOLUTE ANTI-BLUR ENGINE FIXES (Razor-Sharp Pixel Snapping & Font Kerning)
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
            RenderOptions.SetClearTypeHint(this, ClearTypeHint.Enabled);
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
            FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI, Roboto, sans-serif");
            FocusVisualStyle = null;
            KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.None);
            KeyboardNavigation.SetDirectionalNavigation(this, KeyboardNavigationMode.None);

            string iconFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(iconFile)) {
                try {
                    Icon = BitmapFrame.Create(new Uri(iconFile, UriKind.Absolute));
                } catch { }
            }

            StateChanged += MainWindow_StateChanged;
            Closed += (s, e) => Application.Current.Shutdown();
        }

        private void SetMaxButtonState(bool isMax) {
            if (btnMax == null) return;
            btnMax.ToolTip = isMax ? "Normal Boyut" : "Tam Ekran";
            Border b = btnMax.Content as Border;
            if (b == null) return;
            if (isMax) {
                b.Child = new SPath {
                    Data = Geometry.Parse("M 3 0 L 11 0 L 11 8 L 8 8 L 8 11 L 0 11 L 0 3 L 3 3 Z M 8 3 L 8 0 M 3 3 L 11 3"),
                    Stroke = new SolidColorBrush(BronzeBorder),
                    StrokeThickness = 1.3,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsHitTestVisible = false
                };
            } else {
                b.Child = new Rectangle {
                    Width = 11,
                    Height = 11,
                    RadiusX = 1.5,
                    RadiusY = 1.5,
                    Stroke = new SolidColorBrush(BronzeBorder),
                    StrokeThickness = 1.4,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsHitTestVisible = false
                };
            }
        }

        private void MainWindow_StateChanged(object sender, EventArgs e) {
            if (WindowState == WindowState.Maximized) {
                SetMaxButtonState(true);
                if (windowGutterGrid != null) {
                    windowGutterGrid.Margin = new Thickness(0);
                }
                if (mainBorder != null) {
                    mainBorder.CornerRadius = new CornerRadius(0);
                    mainBorder.BorderThickness = new Thickness(0);
                    mainBorder.Effect = null;
                }
                if (topBarBorder != null) {
                    topBarBorder.CornerRadius = new CornerRadius(0);
                }
                if (bottomBar != null) {
                    bottomBar.CornerRadius = new CornerRadius(0);
                }
            } else if (WindowState == WindowState.Normal) {
                SetMaxButtonState(false);
                if (windowGutterGrid != null) {
                    windowGutterGrid.Margin = new Thickness(16);
                }
                if (mainBorder != null) {
                    mainBorder.CornerRadius = new CornerRadius(14);
                    mainBorder.BorderThickness = new Thickness(1.2);
                    mainBorder.Effect = new DropShadowEffect {
                        Color = Color.FromRgb(0, 0, 0),
                        BlurRadius = 22,
                        Opacity = 0.85,
                        ShadowDepth = 4,
                        Direction = 270
                    };
                }
                if (topBarBorder != null) {
                    topBarBorder.CornerRadius = new CornerRadius(13, 13, 0, 0);
                }
                if (bottomBar != null) {
                    bottomBar.CornerRadius = new CornerRadius(0, 0, 13, 13);
                }
            }
        }

        private void ToggleMaximize() {
            if (WindowState == WindowState.Maximized) {
                WindowState = WindowState.Normal;
            } else {
                WindowState = WindowState.Maximized;
            }
        }

        private void CreateUI() {
            windowGutterGrid = new Grid {
                Margin = new Thickness(16),
                Background = Brushes.Transparent
            };

            mainBorder = new Border {
                CornerRadius = new CornerRadius(14),
                Background = new LinearGradientBrush(
                    Color.FromRgb(13, 17, 26), 
                    Color.FromRgb(8, 11, 17), 
                    45
                ),
                BorderBrush = new LinearGradientBrush {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection {
                        new GradientStop(Color.FromRgb(255, 238, 165), 0.0),
                        new GradientStop(Color.FromRgb(225, 185, 75), 0.25),
                        new GradientStop(Color.FromRgb(195, 150, 50), 0.50),
                        new GradientStop(Color.FromRgb(225, 185, 75), 0.75),
                        new GradientStop(Color.FromRgb(255, 238, 165), 1.0)
                    }
                },
                BorderThickness = new Thickness(1.4),
                ClipToBounds = false
            };

            mainBorder.Effect = new DropShadowEffect {
                Color = Color.FromRgb(0, 0, 0),
                BlurRadius = 22,
                Opacity = 0.85,
                ShadowDepth = 4,
                Direction = 270
            };

            Grid rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) }); // Top Royal Bar
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Main Body
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(96) }); // Bottom Imperial Action Bar

            // ==========================================
            // 1. TOP ROYAL TITLE BAR (Flush Rounded Top)
            // ==========================================
            topBarBorder = new Border {
                CornerRadius = new CornerRadius(13, 13, 0, 0),
                Background = new LinearGradientBrush(
                    Color.FromRgb(19, 25, 37), 
                    Color.FromRgb(13, 17, 25), 
                    90
                ),
                BorderBrush = new SolidColorBrush(Color.FromRgb(46, 38, 25)),
                BorderThickness = new Thickness(0, 0, 0, 1.2)
            };
            topBarBorder.MouseLeftButtonDown += (s, e) => {
                if (e.ClickCount == 2) {
                    ToggleMaximize();
                } else if (e.LeftButton == MouseButtonState.Pressed) {
                    DragMove();
                }
            };

            Grid topBar = new Grid();

            // Brand Panel (Left) - Displays identical desktop logo
            StackPanel brandPanel = new StackPanel {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(22, 0, 0, 0)
            };

            Border crestBox = new Border {
                Width = 38,
                Height = 38,
                CornerRadius = new CornerRadius(9),
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            crestBox.Effect = new DropShadowEffect {
                Color = GoldPrimary,
                BlurRadius = 16,
                Opacity = 0.55,
                ShadowDepth = 0
            };
            string headerLogoFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_logo.png");
            if (File.Exists(headerLogoFile)) {
                try {
                    BitmapImage hBi = new BitmapImage();
                    hBi.BeginInit();
                    hBi.UriSource = new Uri(headerLogoFile, UriKind.Absolute);
                    hBi.CacheOption = BitmapCacheOption.OnLoad;
                    hBi.EndInit();
                    Image hImg = new Image {
                        Source = hBi,
                        Width = 38,
                        Height = 38,
                        Stretch = Stretch.Uniform
                    };
                    RenderOptions.SetBitmapScalingMode(hImg, BitmapScalingMode.HighQuality);
                    crestBox.Child = hImg;
                } catch { }
            }
            brandPanel.Children.Add(crestBox);

            StackPanel titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            TextBlock titleText = new TextBlock {
                Text = "ETHERIAL",
                FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI, Roboto, sans-serif"),
                FontWeight = FontWeights.Bold,
                FontSize = 22,
                Foreground = new LinearGradientBrush(Color.FromRgb(255, 248, 230), GoldPrimary, 90)
            };
            titleText.Effect = new DropShadowEffect {
                Color = GoldPrimary,
                BlurRadius = 12,
                Opacity = 0.45,
                ShadowDepth = 0
            };
            titleStack.Children.Add(titleText);

            TextBlock subTitleText = new TextBlock {
                Text = "REKABETÇİ İSTEMCİ • 1.8.9",
                FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI, Roboto, sans-serif"),
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(218, 178, 62)),
                Margin = new Thickness(1, 2, 0, 0)
            };
            titleStack.Children.Add(subTitleText);
            brandPanel.Children.Add(titleStack);
            topBar.Children.Add(brandPanel);

            // Center Realm Seal & Latency Badge (Sleek Obsidian Glass Capsule)
            Border centerRealmBadge = new Border {
                Background = new LinearGradientBrush(
                    Color.FromArgb(220, 16, 21, 31),
                    Color.FromArgb(235, 10, 14, 21),
                    90
                ),
                CornerRadius = new CornerRadius(16),
                BorderBrush = new LinearGradientBrush(
                    Color.FromArgb(160, 197, 160, 89),
                    Color.FromArgb(60, 105, 80, 40),
                    45
                ),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 5, 16, 5),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            centerRealmBadge.Effect = new DropShadowEffect {
                Color = Color.FromRgb(0, 0, 0),
                BlurRadius = 14,
                Opacity = 0.5,
                ShadowDepth = 2
            };

            StackPanel centerStack = new StackPanel { 
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center 
            };

            Border dotContainer = new Border {
                Width = 12,
                Height = 12,
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromRgb(8, 11, 16)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(100, 197, 160, 89)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 9, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            statusDot = new Ellipse {
                Width = 6,
                Height = 6,
                Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68)), // Crimson red
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            statusDot.Effect = new DropShadowEffect {
                Color = Color.FromRgb(239, 68, 68),
                BlurRadius = 8,
                Opacity = 0.95,
                ShadowDepth = 0 // Exactly zero offset! No diagonal ghosting!
            };
            dotContainer.Child = statusDot;
            centerStack.Children.Add(dotContainer);

            txtPingStatus = new TextBlock {
                Text = "Etherial Network  •  Çevrimdışı",
                Foreground = new SolidColorBrush(Color.FromRgb(230, 235, 245)),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            centerStack.Children.Add(txtPingStatus);

            centerRealmBadge.Child = centerStack;
            topBar.Children.Add(centerRealmBadge);

            // Window Controls (Right: Settings, Minimize, Fullscreen/Maximize, Close)
            StackPanel controlsPanel = new StackPanel {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 16, 0)
            };

            Button btnSettings = CreateIconButton("settings", "Ayarlar", () => OpenSettingsDialog());
            Button btnMin = CreateIconButton("min", "Simge Durumuna Küçült", () => WindowState = WindowState.Minimized);
            btnMax = CreateIconButton("max", "Tam Ekran", () => ToggleMaximize());
            Button btnClose = CreateIconButton("close", "Kapat", () => Close(), true);

            controlsPanel.Children.Add(btnSettings);
            controlsPanel.Children.Add(btnMin);
            controlsPanel.Children.Add(btnMax);
            controlsPanel.Children.Add(btnClose);
            topBar.Children.Add(controlsPanel);

            topBarBorder.Child = topBar;
            Grid.SetRow(topBarBorder, 0);
            rootGrid.Children.Add(topBarBorder);

            // ==========================================
            // 2. MAIN BODY (Direct Connect Portal + Player Profile)
            // ==========================================
            Grid bodyGrid = new Grid { Margin = new Thickness(24, 18, 24, 18) };
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6.6, GridUnitType.Star) }); // Left Panel
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) }); // Spacing
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4.6, GridUnitType.Star) }); // Right Profile

            // LEFT SIDE: The Server Gateway Card
            Border leftCard = new Border {
                CornerRadius = new CornerRadius(12),
                Background = new LinearGradientBrush(
                    Color.FromRgb(17, 23, 34), 
                    Color.FromRgb(11, 15, 23), 
                    45
                ),
                BorderBrush = new LinearGradientBrush(
                    BronzeBorder, 
                    Color.FromRgb(38, 48, 66), 
                    45
                ),
                BorderThickness = new Thickness(1.2),
                Padding = new Thickness(24)
            };
            leftCard.Effect = new DropShadowEffect {
                Color = Color.FromRgb(0, 0, 0),
                BlurRadius = 18,
                Opacity = 0.5,
                ShadowDepth = 2
            };

            StackPanel leftStack = new StackPanel();

            // Direct Connect Tag (Medieval Gateway Banner)
            Border runicTag = new Border {
                Background = new LinearGradientBrush(
                    Color.FromRgb(46, 36, 18), 
                    Color.FromRgb(22, 17, 9), 
                    45
                ),
                BorderBrush = new SolidColorBrush(Color.FromRgb(218, 178, 62)),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 5, 12, 5),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 12)
            };
            runicTag.Effect = new DropShadowEffect {
                Color = GoldPrimary,
                BlurRadius = 10,
                Opacity = 0.4,
                ShadowDepth = 0
            };
            runicTag.Child = new TextBlock {
                Text = "⚡ DOĞRUDAN BAĞLANTI (AUTO-CONNECT)",
                Foreground = new SolidColorBrush(Color.FromRgb(255, 242, 200)),
                FontSize = 10.5,
                FontWeight = FontWeights.Bold
            };
            leftStack.Children.Add(runicTag);

            TextBlock bannerTitle = new TextBlock {
                Text = "ETHERIAL NETWORK",
                FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI, Roboto, sans-serif"),
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = new LinearGradientBrush(Color.FromRgb(255, 255, 255), Color.FromRgb(235, 212, 154), 90),
                Margin = new Thickness(0, 0, 0, 8)
            };
            bannerTitle.Effect = new DropShadowEffect {
                Color = Color.FromRgb(0, 0, 0),
                BlurRadius = 12,
                Opacity = 0.65,
                ShadowDepth = 2
            };
            leftStack.Children.Add(bannerTitle);

            TextBlock bannerSub = new TextBlock {
                Text = "Minecraft 1.8.9 ana menüsü tamamen atlanarak tek tıkla doğrudan sunucuya bağlanır. Azul Zulu 8 JRE ve Aikar G1GC bellek optimizasyonu ile donatılmıştır.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(168, 182, 202)),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 18,
                Margin = new Thickness(0, 0, 0, 16)
            };
            leftStack.Children.Add(bannerSub);

            // Server Scroll / Realm Status Card
            Border serverCard = new Border {
                Background = new LinearGradientBrush(
                    Color.FromRgb(18, 24, 35),
                    Color.FromRgb(11, 15, 22),
                    90
                ),
                CornerRadius = new CornerRadius(9),
                BorderBrush = new LinearGradientBrush(
                    Color.FromRgb(180, 145, 75),
                    Color.FromRgb(75, 60, 30),
                    45
                ),
                BorderThickness = new Thickness(1.2),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 16)
            };

            StackPanel serverCardStack = new StackPanel();
            StackPanel serverHeader = new StackPanel { 
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock serverIpText = new TextBlock {
                Text = "🎮 SUNUCU: EtherialNetwork",
                FontWeight = FontWeights.Bold,
                FontSize = 15,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 242, 205)),
                VerticalAlignment = VerticalAlignment.Center
            };
            serverHeader.Children.Add(serverIpText);

            txtPlayerCount = new TextBlock {
                Text = "(-- / -- Oyuncu)",
                Foreground = new SolidColorBrush(GoldPrimary),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            serverHeader.Children.Add(txtPlayerCount);
            serverCardStack.Children.Add(serverHeader);

            txtMotd = new TextBlock {
                Text = "Sunucu durumu sorgulanıyor...",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                Margin = new Thickness(0, 6, 0, 0)
            };
            serverCardStack.Children.Add(txtMotd);
            serverCard.Child = serverCardStack;
            leftStack.Children.Add(serverCard);

            // New Section: Competitive Client Modules & Status Indicators
            Border modulesStrip = new Border {
                Background = new LinearGradientBrush(
                    Color.FromArgb(170, 18, 24, 34),
                    Color.FromArgb(170, 11, 15, 23),
                    90
                ),
                CornerRadius = new CornerRadius(8),
                BorderBrush = new LinearGradientBrush(
                    Color.FromArgb(110, 197, 160, 89),
                    Color.FromArgb(50, 85, 68, 42),
                    45
                ),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(0, 0, 0, 12)
            };
            Grid modGrid = new Grid();
            modGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            modGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            modGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            modGrid.Children.Add(CreateModuleTag("✨ Keystrokes & CPS", "Aktif", 0));
            modGrid.Children.Add(CreateModuleTag("🎯 1.8.9 Hit-Reg", "Aktif", 1));
            modGrid.Children.Add(CreateModuleTag("🛡️ Aikar Optimizasyon", "Aktif", 2));
            modulesStrip.Child = modGrid;
            leftStack.Children.Add(modulesStrip);

            // 3 Modern Feature Badges (Stationary & Polished)
            Grid badgeGrid = new Grid();
            badgeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            badgeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            badgeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            badgeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            badgeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            badgeGrid.Children.Add(CreateFeaturePill("⚡ Direkt Bağlan", "Menü Yok, Hızlı Giriş", 0));
            badgeGrid.Children.Add(CreateFeaturePill("🛡️ G1GC Zırhı", "Sıfır Lag & Yüksek FPS", 2));
            badgeGrid.Children.Add(CreateFeaturePill("⚔️ 1.8.9 Rekabetçi", "Saf PvP & LWJGL 2.9", 4));
            leftStack.Children.Add(badgeGrid);

            leftCard.Child = leftStack;
            Grid.SetColumn(leftCard, 0);
            bodyGrid.Children.Add(leftCard);

            // RIGHT SIDE: The Player Profile Card
            Border rightCard = new Border {
                CornerRadius = new CornerRadius(12),
                Background = new LinearGradientBrush(
                    Color.FromRgb(17, 23, 34), 
                    Color.FromRgb(11, 15, 23), 
                    45
                ),
                BorderBrush = new LinearGradientBrush(
                    BronzeBorder, 
                    Color.FromRgb(38, 48, 66), 
                    45
                ),
                BorderThickness = new Thickness(1.2),
                Padding = new Thickness(22)
            };
            rightCard.Effect = new DropShadowEffect {
                Color = Color.FromRgb(0, 0, 0),
                BlurRadius = 18,
                Opacity = 0.5,
                ShadowDepth = 2
            };

            StackPanel rightStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            // Player Profile Header
            TextBlock lblCrown = new TextBlock {
                Text = "OYUNCU PROFİLİ",
                FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI, Roboto, sans-serif"),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(GoldPrimary),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            };
            rightStack.Children.Add(lblCrown);

            // Ornate Gold Avatar Medallion (Vector Anti-Aliased Circular Heraldic Portrait)
            avatarBrush = new ImageBrush {
                Stretch = Stretch.UniformToFill
            };
            RenderOptions.SetBitmapScalingMode(avatarBrush, BitmapScalingMode.HighQuality);

            Ellipse avatarCircle = new Ellipse {
                Width = 96,
                Height = 96,
                Fill = avatarBrush,
                Stroke = new LinearGradientBrush {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection {
                        new GradientStop(Color.FromRgb(255, 238, 165), 0.0),
                        new GradientStop(Color.FromRgb(218, 178, 62), 0.35),
                        new GradientStop(Color.FromRgb(165, 125, 45), 0.7),
                        new GradientStop(Color.FromRgb(255, 238, 165), 1.0)
                    }
                },
                StrokeThickness = 2.8,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16)
            };
            avatarCircle.Effect = new DropShadowEffect {
                Color = GoldPrimary,
                BlurRadius = 16,
                Opacity = 0.55,
                ShadowDepth = 0
            };
            RenderOptions.SetEdgeMode(avatarCircle, EdgeMode.Unspecified);

            imgAvatar = new Image();
            UpdateAvatar(config.Username);
            rightStack.Children.Add(avatarCircle);

            TextBlock lblUser = new TextBlock {
                Text = "KULLANICI ADI",
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(BronzeBorder),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 7)
            };
            rightStack.Children.Add(lblUser);

            // Gold-Trimmed Username Input Box
            Border inputBorder = new Border {
                Background = new SolidColorBrush(Color.FromRgb(10, 14, 21)),
                CornerRadius = new CornerRadius(8),
                BorderBrush = new LinearGradientBrush(
                    Color.FromRgb(197, 160, 89),
                    Color.FromRgb(85, 68, 42),
                    45
                ),
                BorderThickness = new Thickness(1.2),
                Padding = new Thickness(12, 7, 12, 7),
                Margin = new Thickness(0, 0, 0, 14)
            };
            txtUsername = new TextBox {
                Text = config.Username,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 246, 228)),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                CaretBrush = new SolidColorBrush(Color.FromRgb(255, 235, 175)),
                Cursor = Cursors.IBeam
            };
            txtUsername.GotFocus += (s, e) => {
                inputBorder.BorderBrush = new SolidColorBrush(GoldPrimary);
            };
            txtUsername.LostFocus += (s, e) => {
                inputBorder.BorderBrush = new LinearGradientBrush(
                    Color.FromRgb(197, 160, 89),
                    Color.FromRgb(85, 68, 42),
                    45
                );
            };
            txtUsername.TextChanged += (s, e) => {
                config.Username = txtUsername.Text.Trim();
                if (config.RememberProfile) {
                    config.SavedProfile = config.Username;
                }
                SaveConfig();
                if (avatarDebounceTimer != null) avatarDebounceTimer.Stop();
                avatarDebounceTimer = new System.Windows.Threading.DispatcherTimer();
                avatarDebounceTimer.Interval = TimeSpan.FromMilliseconds(350);
                avatarDebounceTimer.Tick += (s2, e2) => {
                    avatarDebounceTimer.Stop();
                    UpdateAvatar(config.Username);
                };
                avatarDebounceTimer.Start();
            };
            inputBorder.Child = txtUsername;
            rightStack.Children.Add(inputBorder);

            // Save Profile Button
            Border btnSaveProfile = new Border {
                Height = 32,
                CornerRadius = new CornerRadius(6),
                Background = new LinearGradientBrush(
                    Color.FromArgb(230, 24, 32, 45),
                    Color.FromArgb(230, 15, 20, 29),
                    90
                ),
                BorderBrush = new LinearGradientBrush(
                    Color.FromRgb(197, 160, 89),
                    Color.FromRgb(115, 88, 48),
                    45
                ),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 0, 12)
            };
            TextBlock txtSaveProfile = new TextBlock {
                Text = "💾 Profili Kaydet",
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 235, 175)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            btnSaveProfile.Child = txtSaveProfile;

            btnSaveProfile.MouseEnter += (s, e) => {
                btnSaveProfile.Background = new LinearGradientBrush(
                    Color.FromArgb(255, 38, 50, 72),
                    Color.FromArgb(255, 24, 32, 46),
                    90
                );
                btnSaveProfile.BorderBrush = new SolidColorBrush(GoldPrimary);
            };
            btnSaveProfile.MouseLeave += (s, e) => {
                btnSaveProfile.Background = new LinearGradientBrush(
                    Color.FromArgb(230, 24, 32, 45),
                    Color.FromArgb(230, 15, 20, 29),
                    90
                );
                btnSaveProfile.BorderBrush = new LinearGradientBrush(
                    Color.FromRgb(197, 160, 89),
                    Color.FromRgb(115, 88, 48),
                    45
                );
            };

            btnSaveProfile.MouseLeftButtonDown += (s, e) => {
                string name = txtUsername.Text.Trim();
                if (string.IsNullOrWhiteSpace(name)) name = "Oyuncu";
                config.Username = name;
                config.SavedProfile = name;
                config.RememberProfile = true;
                if (config.SavedProfiles == null) config.SavedProfiles = new List<string>();
                if (!config.SavedProfiles.Contains(name)) config.SavedProfiles.Add(name);
                SaveConfig();

                btnSaveProfile.Background = new SolidColorBrush(Color.FromRgb(28, 64, 38));
                btnSaveProfile.BorderBrush = new SolidColorBrush(Color.FromRgb(90, 210, 125));
                txtSaveProfile.Text = "✓ Profil Kaydedildi (" + name + ")";
                txtSaveProfile.Foreground = new SolidColorBrush(Color.FromRgb(175, 255, 195));

                var revertTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2.2) };
                revertTimer.Tick += (s2, e2) => {
                    revertTimer.Stop();
                    btnSaveProfile.Background = new LinearGradientBrush(
                        Color.FromArgb(230, 24, 32, 45),
                        Color.FromArgb(230, 15, 20, 29),
                        90
                    );
                    btnSaveProfile.BorderBrush = new LinearGradientBrush(
                        Color.FromRgb(197, 160, 89),
                        Color.FromRgb(115, 88, 48),
                        45
                    );
                    txtSaveProfile.Text = "💾 Profili Kaydet";
                    txtSaveProfile.Foreground = new SolidColorBrush(Color.FromRgb(255, 235, 175));
                };
                revertTimer.Start();
            };
            rightStack.Children.Add(btnSaveProfile);

            // RAM Badge Pill
            Border ramPill = new Border {
                Background = new SolidColorBrush(Color.FromRgb(13, 18, 27)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(125, 98, 52)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 5, 10, 5),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            txtRamBadge = new TextBlock {
                Text = "🛡️ " + (config.MaxRamMb / 1024) + " GB RAM • G1GC Zırhı",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 235, 175)),
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeights.SemiBold
            };
            ramPill.Child = txtRamBadge;
            rightStack.Children.Add(ramPill);

            // New Section: Real-time Connection Readiness Pill
            Border readinessPill = new Border {
                Background = new SolidColorBrush(Color.FromArgb(130, 10, 14, 22)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(70, 125, 98, 52)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 4, 10, 4),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            };
            StackPanel readyRow = new StackPanel { Orientation = Orientation.Horizontal };
            Ellipse readyDot = new Ellipse { 
                Width = 6, 
                Height = 6, 
                Fill = new SolidColorBrush(Color.FromRgb(46, 204, 113)), 
                Margin = new Thickness(0, 0, 6, 0), 
                VerticalAlignment = VerticalAlignment.Center 
            };
            readyDot.Effect = new DropShadowEffect {
                Color = Color.FromRgb(46, 204, 113),
                BlurRadius = 6,
                Opacity = 0.8,
                ShadowDepth = 0
            };
            TextBlock txtReady = new TextBlock { 
                Text = "Savaşa Hazır • İstemci Kararlı", 
                FontSize = 9.5, 
                FontWeight = FontWeights.SemiBold, 
                Foreground = new SolidColorBrush(Color.FromRgb(180, 195, 215)), 
                VerticalAlignment = VerticalAlignment.Center 
            };
            readyRow.Children.Add(readyDot);
            readyRow.Children.Add(txtReady);
            readinessPill.Child = readyRow;
            rightStack.Children.Add(readinessPill);

            rightCard.Child = rightStack;
            Grid.SetColumn(rightCard, 2);
            bodyGrid.Children.Add(rightCard);

            Grid.SetRow(bodyGrid, 1);
            rootGrid.Children.Add(bodyGrid);

            // ==========================================
            // 3. BOTTOM IMPERIAL ACTION BAR (Flush Rounded Bottom)
            // ==========================================
            bottomBar = new Border {
                CornerRadius = new CornerRadius(0, 0, 13, 13),
                Background = new LinearGradientBrush(
                    Color.FromRgb(16, 21, 31), 
                    Color.FromRgb(10, 13, 19), 
                    90
                ),
                BorderBrush = new SolidColorBrush(Color.FromRgb(46, 38, 25)),
                BorderThickness = new Thickness(0, 1.2, 0, 0),
                Padding = new Thickness(24, 0, 24, 0)
            };

            Grid bottomGrid = new Grid();
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Status & Progress
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(310) }); // Play Button

            // Progress & Status Stack (Left)
            StackPanel progressStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 28, 0) };

            txtStatus = new TextBlock {
                Text = "⚔️ Oynamaya Hazır • Etherial Network'e Bağlanın",
                Foreground = new SolidColorBrush(Color.FromRgb(245, 235, 215)),
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            progressStack.Children.Add(txtStatus);

            progressBar = new ProgressBar {
                Height = 10,
                Value = 0,
                Minimum = 0,
                Maximum = 100,
                Background = new SolidColorBrush(Color.FromRgb(9, 13, 19)),
                BorderThickness = new Thickness(0)
            };

            string pbXaml = @"<ControlTemplate TargetType=""ProgressBar"" xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Border Name=""PART_Track"" CornerRadius=""5"" Background=""#0A0E16"" BorderBrush=""#3D2E16"" BorderThickness=""1"" ClipToBounds=""True"">
        <Border Name=""PART_Indicator"" HorizontalAlignment=""Left"" CornerRadius=""4"">
            <Border.Background>
                <LinearGradientBrush StartPoint=""0,0"" EndPoint=""1,0"">
                    <GradientStop Color=""#FFDAB23E"" Offset=""0.0""/>
                    <GradientStop Color=""#FFFAB923"" Offset=""0.5""/>
                    <GradientStop Color=""#FFFFEEA0"" Offset=""0.95""/>
                    <GradientStop Color=""#FFFFFFFF"" Offset=""1.0""/>
                </LinearGradientBrush>
            </Border.Background>
            <Border.Effect>
                <DropShadowEffect Color=""#FFDAB23E"" BlurRadius=""12"" Opacity=""0.8"" ShadowDepth=""0""/>
            </Border.Effect>
        </Border>
    </Border>
</ControlTemplate>";
            try {
                progressBar.Template = (ControlTemplate)XamlReader.Parse(pbXaml);
            } catch { }
            progressStack.Children.Add(progressBar);

            txtProgressDetails = new TextBlock {
                Text = "%0 • Etherial 1.8.9 Core & LWJGL 2.9 Hazır",
                Foreground = new SolidColorBrush(Color.FromRgb(165, 155, 135)),
                FontSize = 11,
                Margin = new Thickness(0, 5, 0, 0)
            };
            progressStack.Children.Add(txtProgressDetails);

            Grid.SetColumn(progressStack, 0);
            bottomGrid.Children.Add(progressStack);

            // The Magnificent Royal "OYNA" Button (Right) - Pure Stationary Illumination (Zero Distortion)
            btnPlay = new Button {
                Height = 60,
                Background = new LinearGradientBrush(GoldPrimary, Color.FromRgb(185, 115, 18), 45),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FocusVisualStyle = null,
                Focusable = false,
                IsTabStop = false
            };

            DropShadowEffect playShadow = new DropShadowEffect {
                Color = GoldPrimary,
                BlurRadius = 18,
                Opacity = 0.55,
                ShadowDepth = 1
            };
            btnPlay.Effect = playShadow;

            ControlTemplate btnTemplate = new ControlTemplate(typeof(Button));
            FrameworkElementFactory btnBorder = new FrameworkElementFactory(typeof(Border));
            btnBorder.Name = "playBtnBorder";
            btnBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(12));
            btnBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            btnBorder.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(255, 238, 175)));
            btnBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1.2));

            FrameworkElementFactory btnContent = new FrameworkElementFactory(typeof(ContentPresenter));
            btnContent.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            btnContent.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            btnBorder.AppendChild(btnContent);
            btnTemplate.VisualTree = btnBorder;
            btnPlay.Template = btnTemplate;

            btnPlay.MouseEnter += (s, e) => {
                btnPlay.Background = new LinearGradientBrush(
                    Color.FromRgb(250, 205, 80),
                    Color.FromRgb(205, 130, 20),
                    45
                );
                DoubleAnimation animBlur = new DoubleAnimation(28, TimeSpan.FromMilliseconds(180));
                DoubleAnimation animOpacity = new DoubleAnimation(0.85, TimeSpan.FromMilliseconds(180));
                playShadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, animBlur);
                playShadow.BeginAnimation(DropShadowEffect.OpacityProperty, animOpacity);
            };

            btnPlay.MouseLeave += (s, e) => {
                btnPlay.Background = new LinearGradientBrush(GoldPrimary, Color.FromRgb(185, 115, 18), 45);
                DoubleAnimation animBlur = new DoubleAnimation(18, TimeSpan.FromMilliseconds(220));
                DoubleAnimation animOpacity = new DoubleAnimation(0.55, TimeSpan.FromMilliseconds(220));
                playShadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, animBlur);
                playShadow.BeginAnimation(DropShadowEffect.OpacityProperty, animOpacity);
            };

            StackPanel btnPlayContent = new StackPanel { Orientation = Orientation.Horizontal };
            TextBlock playIcon = new TextBlock {
                Text = "⚔️",
                FontSize = 22,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };
            btnPlayContent.Children.Add(playIcon);

            StackPanel playTextStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            TextBlock playTitle = new TextBlock {
                Text = "OYNA",
                FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI, Roboto, sans-serif"),
                FontWeight = FontWeights.Bold,
                FontSize = 21,
                Foreground = Brushes.White
            };
            playTextStack.Children.Add(playTitle);

            TextBlock playSub = new TextBlock {
                Text = "DOĞRUDAN SUNUCUYA BAĞLAN",
                FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI, Roboto, sans-serif"),
                FontWeight = FontWeights.SemiBold,
                FontSize = 9.5,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 242, 205))
            };
            playTextStack.Children.Add(playSub);

            btnPlayContent.Children.Add(playTextStack);
            btnPlay.Content = btnPlayContent;

            btnPlay.Click += async (s, e) => await StartDirectConnectFlow();

            Grid.SetColumn(btnPlay, 1);
            bottomGrid.Children.Add(btnPlay);

            bottomBar.Child = bottomGrid;
            Grid.SetRow(bottomBar, 2);
            rootGrid.Children.Add(bottomBar);

            mainBorder.Child = rootGrid;
            windowGutterGrid.Children.Add(mainBorder);
            Content = windowGutterGrid;
        }

        private UIElement CreateModuleTag(string name, string status, int col) {
            Border tagBorder = new Border {
                Background = new LinearGradientBrush(
                    Color.FromArgb(170, 20, 27, 39),
                    Color.FromArgb(170, 13, 17, 25),
                    90
                ),
                BorderBrush = new LinearGradientBrush(
                    Color.FromArgb(90, 197, 160, 89),
                    Color.FromArgb(50, 105, 80, 42),
                    45
                ),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(col == 0 ? 0 : 4, 0, col == 2 ? 0 : 4, 0)
            };

            StackPanel sp = new StackPanel { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Ellipse dot = new Ellipse { 
                Width = 6, 
                Height = 6, 
                Fill = new SolidColorBrush(Color.FromRgb(46, 204, 113)), 
                Margin = new Thickness(0, 0, 6, 0), 
                VerticalAlignment = VerticalAlignment.Center 
            };
            dot.Effect = new DropShadowEffect {
                Color = Color.FromRgb(46, 204, 113),
                BlurRadius = 6,
                Opacity = 0.8,
                ShadowDepth = 0
            };
            TextBlock txtName = new TextBlock { 
                Text = name, 
                FontSize = 10, 
                FontWeight = FontWeights.SemiBold, 
                Foreground = new SolidColorBrush(Color.FromRgb(220, 230, 245)), 
                VerticalAlignment = VerticalAlignment.Center 
            };
            sp.Children.Add(dot);
            sp.Children.Add(txtName);
            tagBorder.Child = sp;
            Grid.SetColumn(tagBorder, col);
            return tagBorder;
        }

        private Border CreateFeaturePill(string title, string desc, int col) {
            Border b = new Border {
                Background = new LinearGradientBrush(
                    Color.FromRgb(22, 29, 42), 
                    Color.FromRgb(14, 19, 28), 
                    90
                ),
                BorderBrush = new LinearGradientBrush(
                    Color.FromRgb(185, 150, 85), 
                    Color.FromRgb(95, 75, 40), 
                    45
                ),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(11, 9, 11, 9),
                Cursor = Cursors.Hand
            };

            DropShadowEffect pillShadow = new DropShadowEffect {
                Color = Color.FromRgb(0, 0, 0),
                BlurRadius = 8,
                Opacity = 0.4,
                ShadowDepth = 1
            };
            b.Effect = pillShadow;

            StackPanel sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            TextBlock t = new TextBlock {
                Text = title,
                FontSize = 12.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 245, 215))
            };
            TextBlock d = new TextBlock {
                Text = desc,
                FontSize = 11,
                FontWeight = FontWeights.Normal,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 212, 228)),
                Margin = new Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            sp.Children.Add(t);
            sp.Children.Add(d);
            b.Child = sp;

            // Smooth stationary hover: ZERO movement, ZERO scaling!
            b.MouseEnter += (s, e) => {
                b.BorderBrush = new SolidColorBrush(GoldPrimary);
                b.Background = new LinearGradientBrush(
                    Color.FromRgb(30, 40, 58),
                    Color.FromRgb(18, 25, 37),
                    90
                );
                pillShadow.Opacity = 0.65;
                pillShadow.BlurRadius = 14;
            };

            b.MouseLeave += (s, e) => {
                b.Background = new LinearGradientBrush(
                    Color.FromRgb(22, 29, 42), 
                    Color.FromRgb(14, 19, 28), 
                    90
                );
                b.BorderBrush = new LinearGradientBrush(
                    Color.FromRgb(185, 150, 85), 
                    Color.FromRgb(95, 75, 40), 
                    45
                );
                pillShadow.Opacity = 0.4;
                pillShadow.BlurRadius = 8;
            };

            Grid.SetColumn(b, col);
            return b;
        }

        private Button CreateIconButton(string iconType, string tooltip, Action onClick, bool isClose = false) {
            Button btn = new Button {
                ToolTip = tooltip,
                Width = 34,
                Height = 34,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(BronzeBorder),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Margin = new Thickness(4, 0, 0, 0),
                FocusVisualStyle = null,
                Focusable = false,
                IsTabStop = false
            };

            UIElement iconVisual = null;
            Shape hoverShape = null;

            if (iconType == "close") {
                SPath p = new SPath {
                    Data = Geometry.Parse("M 0 0 L 10 10 M 10 0 L 0 10"),
                    Stroke = new SolidColorBrush(BronzeBorder),
                    StrokeThickness = 1.6,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsHitTestVisible = false
                };
                iconVisual = p;
                hoverShape = p;
            } else if (iconType == "min") {
                Rectangle r = new Rectangle {
                    Width = 11,
                    Height = 2,
                    RadiusX = 1,
                    RadiusY = 1,
                    Fill = new SolidColorBrush(BronzeBorder),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsHitTestVisible = false
                };
                iconVisual = r;
                hoverShape = r;
            } else if (iconType == "max") {
                Rectangle r = new Rectangle {
                    Width = 11,
                    Height = 11,
                    RadiusX = 1.5,
                    RadiusY = 1.5,
                    Stroke = new SolidColorBrush(BronzeBorder),
                    StrokeThickness = 1.4,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsHitTestVisible = false
                };
                iconVisual = r;
                hoverShape = r;
            } else if (iconType == "settings") {
                SPath p = new SPath {
                    Data = Geometry.Parse("M 6.2 0 C 6.5 0 6.8 0.4 6.8 0.8 L 7.0 1.7 C 7.5 1.9 7.9 2.2 8.3 2.5 L 9.2 2.1 C 9.6 1.9 10.0 2.1 10.2 2.5 L 11.0 3.8 C 11.2 4.2 11.1 4.6 10.7 4.9 L 9.9 5.5 C 10.0 5.8 10.0 6.2 9.9 6.5 L 10.7 7.1 C 11.1 7.4 11.2 7.8 11.0 8.2 L 10.2 9.5 C 10.0 9.9 9.6 10.1 9.2 9.9 L 8.3 9.5 C 7.9 9.8 7.5 10.1 7.0 10.3 L 6.8 11.2 C 6.8 11.6 6.5 12 6.2 12 L 4.8 12 C 4.5 12 4.2 11.6 4.2 11.2 L 4.0 10.3 C 3.5 10.1 3.1 9.8 2.7 9.5 L 1.8 9.9 C 1.4 10.1 1.0 9.9 0.8 9.5 L 0.0 8.2 C -0.2 7.8 -0.1 7.4 0.3 7.1 L 1.1 6.5 C 1.0 6.2 1.0 5.8 1.1 5.5 L 0.3 4.9 C -0.1 4.6 -0.2 4.2 0.0 3.8 L 0.8 2.5 C 1.0 2.1 1.4 1.9 1.8 2.1 L 2.7 2.5 C 3.1 2.2 3.5 1.9 4.0 1.7 L 4.2 0.8 C 4.2 0.4 4.5 0 4.8 0 Z M 5.5 4.2 C 4.5 4.2 3.7 5.0 3.7 6.0 C 3.7 7.0 4.5 7.8 5.5 7.8 C 6.5 7.8 7.3 7.0 7.3 6.0 C 7.3 5.0 6.5 4.2 5.5 4.2 Z"),
                    Fill = new SolidColorBrush(BronzeBorder),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsHitTestVisible = false
                };
                iconVisual = p;
                hoverShape = p;
            }

            Border border = new Border {
                CornerRadius = new CornerRadius(7),
                Background = Brushes.Transparent,
                Width = 34,
                Height = 34,
                Child = iconVisual
            };
            btn.Content = border;

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            template.VisualTree = cp;
            btn.Template = template;

            btn.Click += (s, e) => onClick();

            btn.MouseEnter += (s, e) => {
                if (isClose) {
                    border.Background = new SolidColorBrush(Color.FromRgb(190, 35, 45));
                    if (hoverShape != null) hoverShape.Stroke = Brushes.White;
                } else {
                    border.Background = new SolidColorBrush(Color.FromRgb(28, 38, 54));
                    if (hoverShape != null) {
                        if (hoverShape.Stroke != null) hoverShape.Stroke = new SolidColorBrush(Color.FromRgb(255, 235, 175));
                        if (hoverShape.Fill != null && hoverShape.Fill != Brushes.Transparent) hoverShape.Fill = new SolidColorBrush(Color.FromRgb(255, 235, 175));
                    }
                }
            };

            btn.MouseLeave += (s, e) => {
                border.Background = Brushes.Transparent;
                if (hoverShape != null) {
                    if (hoverShape.Stroke != null) hoverShape.Stroke = new SolidColorBrush(BronzeBorder);
                    if (hoverShape.Fill != null && hoverShape.Fill != Brushes.Transparent) hoverShape.Fill = new SolidColorBrush(BronzeBorder);
                }
            };

            return btn;
        }

        private void UpdateAvatar(string username) {
            try {
                string name = string.IsNullOrEmpty(username) ? "MHF_Steve" : username.Trim();
                string cacheFile = System.IO.Path.Combine(skinsCacheDir, name.ToLower() + ".png");

                // 1. Instant Cache Hit: Load in 0ms synchronously
                if (File.Exists(cacheFile)) {
                    try {
                        BitmapImage cachedBmp = new BitmapImage();
                        cachedBmp.BeginInit();
                        cachedBmp.CacheOption = BitmapCacheOption.OnLoad;
                        cachedBmp.UriSource = new Uri(cacheFile, UriKind.Absolute);
                        cachedBmp.EndInit();
                        cachedBmp.Freeze();
                        if (avatarBrush != null) avatarBrush.ImageSource = cachedBmp;
                        if (imgAvatar != null) imgAvatar.Source = cachedBmp;
                        return;
                    } catch { }
                }

                // 2. Fallback Steve if not cached yet (guarantees image is NEVER blank)
                if (avatarBrush == null || avatarBrush.ImageSource == null) {
                    string defaultSteveFile = System.IO.Path.Combine(skinsCacheDir, "_default_steve.png");
                    if (File.Exists(defaultSteveFile)) {
                        try {
                            BitmapImage steveBmp = new BitmapImage();
                            steveBmp.BeginInit();
                            steveBmp.CacheOption = BitmapCacheOption.OnLoad;
                            steveBmp.UriSource = new Uri(defaultSteveFile, UriKind.Absolute);
                            steveBmp.EndInit();
                            steveBmp.Freeze();
                            if (avatarBrush != null) avatarBrush.ImageSource = steveBmp;
                            if (imgAvatar != null) imgAvatar.Source = steveBmp;
                        } catch { }
                    }
                }

                // 3. Background Download from Minotar / Crafatar with Timeout & Cache Write
                Task.Run(async () => {
                    try {
                        string[] urls = new string[] {
                            "https://minotar.net/helm/" + name + "/128.png",
                            "https://crafatar.com/avatars/" + name + "?size=128&overlay"
                        };

                        byte[] data = null;
                        using (WebClient wc = CreateWebClient()) {
                            foreach (string u in urls) {
                                try {
                                    var downloadTask = wc.DownloadDataTaskAsync(new Uri(u));
                                    if (await Task.WhenAny(downloadTask, Task.Delay(2500)) == downloadTask) {
                                        data = await downloadTask;
                                        if (data != null && data.Length > 200) break;
                                    }
                                } catch { }
                            }
                        }

                        if (data != null && data.Length > 200) {
                            try {
                                File.WriteAllBytes(cacheFile, data);
                            } catch { }

                            Dispatcher.Invoke(() => {
                                if (string.Equals(config.Username, name, StringComparison.OrdinalIgnoreCase)) {
                                    try {
                                        BitmapImage newBmp = new BitmapImage();
                                        newBmp.BeginInit();
                                        newBmp.CacheOption = BitmapCacheOption.OnLoad;
                                        newBmp.StreamSource = new MemoryStream(data);
                                        newBmp.EndInit();
                                        newBmp.Freeze();
                                        if (avatarBrush != null) avatarBrush.ImageSource = newBmp;
                                        if (imgAvatar != null) imgAvatar.Source = newBmp;
                                    } catch { }
                                }
                            });
                        }
                    } catch { }
                });
            } catch { }
        }

        private void StartServerPingLoop() {
            DoPing();
            pingTimer = new System.Windows.Threading.DispatcherTimer();
            pingTimer.Interval = TimeSpan.FromSeconds(4);
            pingTimer.Tick += (s, e) => DoPing();
            pingTimer.Start();
        }

        private async void DoPing() {
            try {
                ServerPingResult result = await Task.Run(() => PingServer(config.ServerHost, config.ServerPort));
                Dispatcher.Invoke(() => {
                    if (result.Online) {
                        statusDot.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Vibrant Emerald Green
                        statusDot.Effect = new DropShadowEffect {
                            Color = Color.FromRgb(16, 185, 129),
                            BlurRadius = 10,
                            Opacity = 0.95,
                            ShadowDepth = 0
                        };
                        txtPingStatus.Text = "Etherial Network  •  " + result.LatencyMs + " ms";
                        txtPingStatus.Foreground = new SolidColorBrush(Color.FromRgb(235, 245, 255));
                        txtPlayerCount.Text = "(" + result.PlayersOnline + " / " + result.PlayersMax + " Oyuncu)";
                        txtMotd.Text = result.Motd;
                    } else {
                        statusDot.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Crimson Red
                        statusDot.Effect = new DropShadowEffect {
                            Color = Color.FromRgb(239, 68, 68),
                            BlurRadius = 8,
                            Opacity = 0.9,
                            ShadowDepth = 0
                        };
                        txtPingStatus.Text = "Etherial Network  •  Çevrimdışı";
                        txtPingStatus.Foreground = new SolidColorBrush(Color.FromRgb(220, 225, 235));
                        txtPlayerCount.Text = "(-- / -- Oyuncu)";
                        txtMotd.Text = "Sunucuya şu anda ulaşılamıyor. Sunucu açıldığında otomatik bağlanacaktır.";
                    }
                });
            } catch { }
        }

        // Native TCP Minecraft Server List Ping (SLP) Implementation
        private ServerPingResult PingServer(string host, int port) {
            ServerPingResult res = new ServerPingResult();
            try {
                Stopwatch sw = Stopwatch.StartNew();
                using (TcpClient client = new TcpClient()) {
                    var connectTask = client.ConnectAsync(host, port);
                    if (!connectTask.Wait(2000)) return res;

                    using (NetworkStream stream = client.GetStream()) {
                        stream.ReadTimeout = 2000;
                        stream.WriteTimeout = 2000;

                        // Handshake Packet: 0x00, Protocol: 47 (1.8.x), Host, Port, NextState: 1
                        byte[] hostBytes = Encoding.UTF8.GetBytes(host);
                        List<byte> handshakeData = new List<byte>();
                        WriteVarInt(handshakeData, 0x00);
                        WriteVarInt(handshakeData, 47);
                        WriteVarInt(handshakeData, hostBytes.Length);
                        handshakeData.AddRange(hostBytes);
                        handshakeData.Add((byte)((port >> 8) & 0xFF));
                        handshakeData.Add((byte)(port & 0xFF));
                        WriteVarInt(handshakeData, 1);

                        List<byte> handshakePacket = new List<byte>();
                        WriteVarInt(handshakePacket, handshakeData.Count);
                        handshakePacket.AddRange(handshakeData);

                        byte[] requestPacket = new byte[] { 0x01, 0x00 };

                        byte[] fullSend = new byte[handshakePacket.Count + requestPacket.Length];
                        handshakePacket.CopyTo(fullSend, 0);
                        requestPacket.CopyTo(fullSend, handshakePacket.Count);

                        stream.Write(fullSend, 0, fullSend.Length);

                        sw.Stop();
                        res.LatencyMs = sw.ElapsedMilliseconds;

                        // Read response
                        int packetLen = ReadVarInt(stream);
                        int packetId = ReadVarInt(stream);
                        if (packetId == 0x00) {
                            int stringLen = ReadVarInt(stream);
                            byte[] buffer = new byte[stringLen];
                            int readTotal = 0;
                            while (readTotal < stringLen) {
                                int r = stream.Read(buffer, readTotal, stringLen - readTotal);
                                if (r <= 0) break;
                                readTotal += r;
                            }

                            string jsonStr = Encoding.UTF8.GetString(buffer, 0, readTotal);
                            JavaScriptSerializer js = new JavaScriptSerializer();
                            Dictionary<string, object> dict = js.Deserialize<Dictionary<string, object>>(jsonStr);

                            res.Online = true;
                            if (dict.ContainsKey("players")) {
                                var players = dict["players"] as Dictionary<string, object>;
                                if (players != null) {
                                    if (players.ContainsKey("online")) res.PlayersOnline = Convert.ToInt32(players["online"]);
                                    if (players.ContainsKey("max")) res.PlayersMax = Convert.ToInt32(players["max"]);
                                }
                            }
                            if (dict.ContainsKey("description")) {
                                if (dict["description"] is string) {
                                    res.Motd = dict["description"].ToString();
                                } else if (dict["description"] is Dictionary<string, object>) {
                                    var d = dict["description"] as Dictionary<string, object>;
                                    if (d != null && d.ContainsKey("text")) res.Motd = d["text"].ToString();
                                }
                                res.Motd = Regex.Replace(res.Motd ?? "", "§[0-9a-fk-or]", "").Trim();
                            }
                        }
                    }
                }
            } catch { }
            return res;
        }

        private void WriteVarInt(List<byte> buffer, int value) {
            while (true) {
                if ((value & ~0x7F) == 0) {
                    buffer.Add((byte)value);
                    return;
                }
                buffer.Add((byte)((value & 0x7F) | 0x80));
                value = (int)((uint)value >> 7);
            }
        }

        private int ReadVarInt(NetworkStream stream) {
            int numRead = 0;
            int result = 0;
            while (true) {
                int read = stream.ReadByte();
                if (read == -1) throw new EndOfStreamException();
                int value = (read & 0x7F);
                result |= (value << (7 * numRead));
                numRead++;
                if (numRead > 5) throw new InvalidOperationException("VarInt too big");
                if ((read & 0x80) == 0) break;
            }
            return result;
        }

        private WebClient CreateWebClient() {
            WebClient wc = new WebClient();
            wc.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            return wc;
        }

        // ==========================================
        // AUTO-OPTIMIZATION & TURKISH LANGUAGE INJECTOR
        // ==========================================
        private void OptimizeGameSettings() {
            try {
                if (!Directory.Exists(gameDir)) Directory.CreateDirectory(gameDir);
                string optionsFile = System.IO.Path.Combine(gameDir, "options.txt");

                Dictionary<string, string> settings = new Dictionary<string, string>();
                if (File.Exists(optionsFile)) {
                    string[] lines = File.ReadAllLines(optionsFile);
                    foreach (string line in lines) {
                        int colon = line.IndexOf(':');
                        if (colon > 0) {
                            string key = line.Substring(0, colon).Trim();
                            string val = line.Substring(colon + 1).Trim();
                            settings[key] = val;
                        }
                    }
                }

                // 1. Mandatory Turkish Language
                settings["lang"] = "tr_TR";

                // 2. High-Performance Competitive Graphics Tweaks (Zero-Stutter)
                settings["fancyGraphics"] = "false";      // Fast Graphics
                settings["ao"] = "0";                     // Smooth Lighting OFF (Eliminates lighting calculation lag)
                settings["renderClouds"] = "false";       // Clouds OFF
                settings["particles"] = "2";              // Minimal Particles (High FPS in PvP)
                settings["renderDistance"] = "8";         // 8 chunks (Sweet spot for 1.8.9 memory)
                settings["enableVsync"] = "false";        // VSync OFF (Uncapped)
                settings["maxFps"] = "260";               // 260 FPS or uncapped
                settings["mipmapLevels"] = "0";           // 0 Mipmaps (Eliminates texture stall)
                settings["entityShadows"] = "false";      // Entity Shadows OFF
                settings["anisotropicFiltering"] = "1";   // OFF
                settings["fboEnable"] = "true";

                StringBuilder sb = new StringBuilder();
                foreach (var kvp in settings) {
                    sb.AppendLine(kvp.Key + ":" + kvp.Value);
                }
                File.WriteAllText(optionsFile, sb.ToString());
            } catch { }
        }

        // ==========================================
        // LAUNCH & DIRECT CONNECT EXECUTION ENGINE
        // ==========================================
        private bool isLaunching = false;

        private async Task StartDirectConnectFlow() {
            if (isLaunching) return;
            isLaunching = true;
            btnPlay.IsEnabled = false;
            btnPlay.Opacity = 0.65;

            try {
                SetProgress("Oyun varlıkları ve kütüphaneler taranıyor...", 15);
                await Task.Delay(250);

                OptimizeGameSettings();

                if (!Directory.Exists(gameDir)) Directory.CreateDirectory(gameDir);
                if (!Directory.Exists(nativesDir)) Directory.CreateDirectory(nativesDir);
                if (!Directory.Exists(librariesDir)) Directory.CreateDirectory(librariesDir);

                SetProgress("Mojang 1.8.9 sürüm manifesti ve çekirdeği doğrulanıyor...", 35);
                await Task.Delay(250);

                string manifestFile = System.IO.Path.Combine(gameDir, "versions", "1.8.9", "1.8.9.json");
                string manifestJson = "";

                if (File.Exists(manifestFile)) {
                    manifestJson = File.ReadAllText(manifestFile);
                } else {
                    string manifestUrl = "https://piston-meta.mojang.com/v1/packages/d546f1707a3f2b7d034eece5ea2e311eda875787/1.8.9.json";
                    using (WebClient wc = CreateWebClient()) {
                        manifestJson = await wc.DownloadStringTaskAsync(manifestUrl);
                    }
                    string vDir = System.IO.Path.GetDirectoryName(manifestFile);
                    if (!Directory.Exists(vDir)) Directory.CreateDirectory(vDir);
                    File.WriteAllText(manifestFile, manifestJson);
                }

                JavaScriptSerializer js = new JavaScriptSerializer();
                Dictionary<string, object> manifest = js.Deserialize<Dictionary<string, object>>(manifestJson);

                // 1. Client JAR
                var downloads = manifest["downloads"] as Dictionary<string, object>;
                var clientInfo = downloads["client"] as Dictionary<string, object>;
                string clientUrl = clientInfo["url"].ToString();
                string clientSha1 = clientInfo["sha1"].ToString();

                await DownloadIfMissingAsync(clientUrl, clientJarPath, clientSha1);

                // 2. Libraries & Natives
                SetProgress("Kütüphaneler ve LWJGL sürücüleri eşitleniyor...", 55);
                await Task.Delay(250);

                var libraries = manifest["libraries"] as System.Collections.ArrayList;
                List<string> classpathJars = new List<string>();
                List<string> nativeZips = new List<string>();

                foreach (Dictionary<string, object> lib in libraries) {
                    bool allowed = true;
                    if (lib.ContainsKey("rules")) {
                        allowed = false;
                        var rules = lib["rules"] as System.Collections.ArrayList;
                        foreach (Dictionary<string, object> rule in rules) {
                            if (!rule.ContainsKey("os") || ((rule["os"] as Dictionary<string, object>)["name"].ToString() == "windows")) {
                                allowed = (rule["action"].ToString() == "allow");
                            }
                        }
                    }
                    if (!allowed) continue;

                    if (lib.ContainsKey("downloads")) {
                        var libDownloads = lib["downloads"] as Dictionary<string, object>;
                        if (libDownloads.ContainsKey("artifact")) {
                            var art = libDownloads["artifact"] as Dictionary<string, object>;
                            string url = art["url"].ToString();
                            string sha1 = art.ContainsKey("sha1") ? art["sha1"].ToString() : null;
                            string path = art["path"].ToString();
                            string dest = System.IO.Path.Combine(librariesDir, path);

                            await DownloadIfMissingAsync(url, dest, sha1);
                            classpathJars.Add(dest);
                        }

                        // Natives
                        if (lib.ContainsKey("natives")) {
                            var natives = lib["natives"] as Dictionary<string, object>;
                            if (natives.ContainsKey("windows") && libDownloads.ContainsKey("classifiers")) {
                                string classifierKey = natives["windows"].ToString();
                                var classifiers = libDownloads["classifiers"] as Dictionary<string, object>;
                                if (classifiers.ContainsKey(classifierKey)) {
                                    var natArt = classifiers[classifierKey] as Dictionary<string, object>;
                                    string natUrl = natArt["url"].ToString();
                                    string natSha1 = natArt.ContainsKey("sha1") ? natArt["sha1"].ToString() : null;
                                    string natPath = natArt["path"].ToString();
                                    string natDest = System.IO.Path.Combine(librariesDir, natPath);

                                    await DownloadIfMissingAsync(natUrl, natDest, natSha1);
                                    nativeZips.Add(natDest);
                                }
                            }
                        }
                    }
                }

                classpathJars.Add(clientJarPath);

                // 3. Extract Natives
                SetProgress("LWJGL 2.9.x Windows DLL kütüphaneleri ayıklanıyor...", 75);
                await Task.Delay(200);

                foreach (string zip in nativeZips) {
                    if (File.Exists(zip)) {
                        try {
                            using (ZipArchive archive = ZipFile.OpenRead(zip)) {
                                foreach (ZipArchiveEntry entry in archive.Entries) {
                                    if (entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) {
                                        string destFile = System.IO.Path.Combine(nativesDir, entry.Name);
                                        entry.ExtractToFile(destFile, true);
                                    }
                                }
                            }
                        } catch { }
                    }
                }

                // 3.5 Asset Index (1.8.json) & Turkish Language Asset
                SetProgress("Türkçe dil paketi ve ses varlıkları doğrulanıyor...", 88);
                await Task.Delay(200);

                string assetIndexDir = System.IO.Path.Combine(gameDir, "assets", "indexes");
                string assetIndexFile = System.IO.Path.Combine(assetIndexDir, "1.8.json");
                if (!File.Exists(assetIndexFile)) {
                    if (!Directory.Exists(assetIndexDir)) Directory.CreateDirectory(assetIndexDir);
                    if (manifest.ContainsKey("assetIndex")) {
                        var aIdx = manifest["assetIndex"] as Dictionary<string, object>;
                        if (aIdx != null && aIdx.ContainsKey("url")) {
                            string idxUrl = aIdx["url"].ToString();
                            string idxSha1 = aIdx.ContainsKey("sha1") ? aIdx["sha1"].ToString() : null;
                            await DownloadIfMissingAsync(idxUrl, assetIndexFile, idxSha1);
                        }
                    }
                }

                // Download tr_TR.lang asset object explicitly
                string trHash = "2862fb39b282f3ae2bbee03ab91c8941b403494c";
                string trDest = System.IO.Path.Combine(gameDir, "assets", "objects", "28", trHash);
                string trUrl = "https://resources.download.minecraft.net/28/" + trHash;
                await DownloadIfMissingAsync(trUrl, trDest, trHash);

                // 4. Launch JVM Process
                SetProgress("Aikar G1GC zırhı ve " + (config.MaxRamMb / 1024) + " GB bellek tahsis ediliyor...", 96);
                await Task.Delay(250);

                SetProgress("⚔️ Sunucuya Bağlanılıyor! Minecraft Açılıyor...", 100);
                await Task.Delay(350);

                string cpString = string.Join(";", classpathJars.ToArray());
                string uuid = GenerateOfflineUuid(config.Username);

                // Locate JavaAgent (Prioritize gameDir which has zero spaces)
                string agentPath = System.IO.Path.Combine(gameDir, "etherial-agent.jar");
                if (!File.Exists(agentPath)) {
                    agentPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "etherial-agent.jar");
                }

                List<string> args = new List<string>();
                args.Add("-Xms" + config.MinRamMb + "M");
                args.Add("-Xmx" + config.MaxRamMb + "M");
                args.Add("-XX:+UseG1GC");
                args.Add("-XX:+ParallelRefProcEnabled");
                args.Add("-XX:MaxGCPauseMillis=20");
                args.Add("-XX:+UnlockExperimentalVMOptions");
                args.Add("-XX:+DisableExplicitGC");
                args.Add("-XX:+AlwaysPreTouch");
                args.Add("-XX:G1NewSizePercent=30");
                args.Add("-XX:G1MaxNewSizePercent=40");
                args.Add("-XX:G1ReservePercent=15");
                args.Add("-XX:G1HeapWastePercent=5");
                args.Add("-XX:G1MixedGCCountTarget=4");
                args.Add("-XX:InitiatingHeapOccupancyPercent=15");
                args.Add("-XX:G1MixedGCLiveThresholdPercent=90");
                args.Add("-XX:G1RSetUpdatingPauseTimePercent=5");
                args.Add("-XX:SurvivorRatio=32");
                args.Add("-XX:+PerfDisableSharedMem");
                args.Add("-XX:MaxTenuringThreshold=1");
                args.Add("-XX:+UseStringDeduplication");
                args.Add("-XX:+AggressiveOpts");
                args.Add("-XX:+UseFastAccessorMethods");
                args.Add("-Dsun.rmi.dgc.server.gcInterval=2147483646");
                args.Add("-Djava.library.path=" + nativesDir);
                args.Add("-Detherial.server.host=" + config.ServerHost);
                args.Add("-Detherial.server.port=" + config.ServerPort);

                // Attach JavaAgent (Enforces Title="Etherial", auto-connect, Disconnect exit, and in-game skins)
                if (File.Exists(agentPath)) {
                    args.Add("-javaagent:" + agentPath + "=" + config.ServerHost + ":" + config.ServerPort);
                }

                args.Add("-cp");
                args.Add("\"" + cpString + "\"");
                args.Add("net.minecraft.client.main.Main");

                args.Add("--username"); args.Add(config.Username);
                args.Add("--version"); args.Add("Etherial");
                args.Add("--gameDir"); args.Add("\"" + gameDir + "\"");
                args.Add("--assetsDir"); args.Add("\"" + System.IO.Path.Combine(gameDir, "assets") + "\"");
                args.Add("--assetIndex"); args.Add("1.8");
                args.Add("--uuid"); args.Add(uuid);
                args.Add("--accessToken"); args.Add("00000000-0000-0000-0000-000000000000");
                args.Add("--userProperties"); args.Add("{}");
                args.Add("--userType"); args.Add("legacy");
                args.Add("--width"); args.Add(config.ResolutionWidth.ToString());
                args.Add("--height"); args.Add(config.ResolutionHeight.ToString());

                if (config.Fullscreen) args.Add("--fullscreen");

                // Direct Connect parameters
                args.Add("--server"); args.Add(config.ServerHost);
                args.Add("--port"); args.Add(config.ServerPort.ToString());

                ProcessStartInfo psi = new ProcessStartInfo {
                    FileName = "javaw.exe",
                    Arguments = string.Join(" ", args.ToArray()),
                    WorkingDirectory = gameDir,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process mcProc = Process.Start(psi);
                if (mcProc != null) {
                    SetProgress("⚔️ Sunucuya bağlanılıyor! Minecraft devrede.", 100);
                    await Task.Delay(1000);
                    WindowState = WindowState.Minimized;

                    mcProc.EnableRaisingEvents = true;
                    mcProc.Exited += (s, e) => {
                        Dispatcher.Invoke(() => {
                            WindowState = WindowState.Normal;
                            Activate();
                            ResetToIdleState();
                        });
                    };
                } else {
                    ResetToIdleState();
                }

            } catch (Exception ex) {
                MessageBox.Show("Başlatma hatası: " + ex.Message, "Etherial Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                ResetToIdleState();
            }
        }

        private void ResetToIdleState() {
            isLaunching = false;
            btnPlay.IsEnabled = true;
            btnPlay.Opacity = 1.0;
            SetProgress("⚔️ Oynamaya Hazır • Etherial Network'e Bağlanın", 0);
            if (txtProgressDetails != null) {
                txtProgressDetails.Text = "%0 • Etherial 1.8.9 Core & LWJGL 2.9 Hazır";
            }
        }

        private async Task DownloadIfMissingAsync(string url, string dest, string expectedSha1) {
            if (File.Exists(dest)) {
                if (string.IsNullOrEmpty(expectedSha1)) return;
                string localSha1 = ComputeSha1(dest);
                if (string.Equals(localSha1, expectedSha1, StringComparison.OrdinalIgnoreCase)) return;
            }

            string dir = System.IO.Path.GetDirectoryName(dest);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using (WebClient wc = CreateWebClient()) {
                await wc.DownloadFileTaskAsync(new Uri(url), dest);
            }
        }

        private string ComputeSha1(string filePath) {
            using (FileStream fs = File.OpenRead(filePath))
            using (SHA1Managed sha1 = new SHA1Managed()) {
                byte[] hash = sha1.ComputeHash(fs);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private string GenerateOfflineUuid(string username) {
            using (MD5 md5 = MD5.Create()) {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes("OfflinePlayer:" + username));
                hash[6] = (byte)((hash[6] & 0x0F) | 0x30); // v3
                hash[8] = (byte)((hash[8] & 0x3F) | 0x80); // RFC 4122
                Guid guid = new Guid(hash);
                return guid.ToString();
            }
        }

        private void SetProgress(string statusText, int percentage) {
            Dispatcher.Invoke(() => {
                txtStatus.Text = statusText;
                txtProgressDetails.Text = "%" + percentage + " Tamamlandı";

                DoubleAnimation anim = new DoubleAnimation {
                    To = percentage,
                    Duration = TimeSpan.FromMilliseconds(260),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                progressBar.BeginAnimation(RangeBase.ValueProperty, anim);
            });
        }

        // Settings Dialog Modal with Pixel-Perfect Crisp Rendering
        private void OpenSettingsDialog() {
            Window settingsWin = new Window {
                Title = "Ayarlar",
                Width = 490,
                Height = 340,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true,
                FocusVisualStyle = null,
                FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI, Roboto, sans-serif")
            };
            KeyboardNavigation.SetTabNavigation(settingsWin, KeyboardNavigationMode.None);
            TextOptions.SetTextFormattingMode(settingsWin, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(settingsWin, TextRenderingMode.ClearType);
            RenderOptions.SetClearTypeHint(settingsWin, ClearTypeHint.Enabled);

            string iconFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(iconFile)) {
                try {
                    settingsWin.Icon = BitmapFrame.Create(new Uri(iconFile, UriKind.Absolute));
                } catch { }
            }

            Grid modalGutter = new Grid {
                Margin = new Thickness(16),
                Background = Brushes.Transparent
            };

            Border sBorder = new Border {
                CornerRadius = new CornerRadius(12),
                Background = new LinearGradientBrush(
                    Color.FromRgb(18, 24, 35), 
                    Color.FromRgb(11, 15, 23), 
                    45
                ),
                BorderBrush = new SolidColorBrush(GoldPrimary),
                BorderThickness = new Thickness(1.4),
                Padding = new Thickness(24)
            };
            sBorder.Effect = new DropShadowEffect {
                Color = Color.FromRgb(0, 0, 0),
                BlurRadius = 20,
                Opacity = 0.8,
                ShadowDepth = 3
            };

            StackPanel sStack = new StackPanel();

            // Header
            Grid sHeader = new Grid();
            sHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            sHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock sTitle = new TextBlock {
                Text = "⚙ CLIENT AYARLARI",
                FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI, Roboto, sans-serif"),
                FontWeight = FontWeights.Bold,
                FontSize = 17,
                Foreground = new SolidColorBrush(GoldPrimary),
                VerticalAlignment = VerticalAlignment.Center
            };
            sHeader.Children.Add(sTitle);

            Button btnSClose = CreateIconButton("close", "Kapat", () => settingsWin.Close(), true);
            btnSClose.Margin = new Thickness(0);
            btnSClose.HorizontalAlignment = HorizontalAlignment.Right;
            Grid.SetColumn(btnSClose, 1);
            sHeader.Children.Add(btnSClose);
            sStack.Children.Add(sHeader);

            // RAM Slider Label
            TextBlock lblRam = new TextBlock {
                Text = "🛡️ RAM Tahsisi: " + (config.MaxRamMb / 1024) + " GB (Aikar G1GC Boost)",
                Foreground = new SolidColorBrush(Color.FromRgb(255, 238, 175)),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 18, 0, 8)
            };
            sStack.Children.Add(lblRam);

            // Custom Styled RAM Slider
            Slider sldRam = new Slider {
                Minimum = 1,
                Maximum = 16,
                Value = config.MaxRamMb / 1024,
                IsSnapToTickEnabled = true,
                TickFrequency = 1,
                Margin = new Thickness(0, 0, 0, 8),
                Cursor = Cursors.Hand
            };

            string sldXaml = @"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Slider'>
                <Grid Height='26'>
                    <Border Height='6' CornerRadius='3' Background='#090D14' BorderBrush='#4B3B24' BorderThickness='1' VerticalAlignment='Center'/>
                    <Track x:Name='PART_Track'>
                        <Track.Thumb>
                            <Thumb Cursor='Hand'>
                                <Thumb.Template>
                                    <ControlTemplate TargetType='Thumb'>
                                        <Grid Width='22' Height='22'>
                                            <Border CornerRadius='11' Background='#2A1F0D' BorderBrush='#FFE58F' BorderThickness='2'>
                                                <Border.Effect>
                                                    <DropShadowEffect Color='#DAB23E' BlurRadius='10' Opacity='0.85' ShadowDepth='0'/>
                                                </Border.Effect>
                                                <Ellipse Width='6' Height='6' Fill='#FAB923' HorizontalAlignment='Center' VerticalAlignment='Center'/>
                                            </Border>
                                        </Grid>
                                    </ControlTemplate>
                                </Thumb.Template>
                            </Thumb>
                        </Track.Thumb>
                    </Track>
                </Grid>
            </ControlTemplate>";
            try {
                sldRam.Template = (ControlTemplate)XamlReader.Parse(sldXaml);
            } catch { }

            sldRam.ValueChanged += (s, e) => {
                int gb = (int)sldRam.Value;
                lblRam.Text = "🛡️ RAM Tahsisi: " + gb + " GB (Aikar G1GC Boost)";
            };
            sStack.Children.Add(sldRam);

            // RAM Quick Preset Buttons
            StackPanel ramPresets = new StackPanel { 
                Orientation = Orientation.Horizontal, 
                Margin = new Thickness(0, 0, 0, 16) 
            };
            int[] ramValues = new int[] { 2, 4, 6, 8, 12, 16 };
            foreach (int rv in ramValues) {
                Button btnPreset = new Button {
                    Content = rv + " GB",
                    Width = 60,
                    Height = 26,
                    Margin = new Thickness(0, 0, 8, 0),
                    Background = new SolidColorBrush(Color.FromRgb(14, 19, 29)),
                    Foreground = new SolidColorBrush(Color.FromRgb(210, 220, 235)),
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Cursor = Cursors.Hand,
                    BorderThickness = new Thickness(0),
                    FocusVisualStyle = null,
                    Focusable = false,
                    IsTabStop = false
                };
                ControlTemplate pTemp = new ControlTemplate(typeof(Button));
                FrameworkElementFactory pBrd = new FrameworkElementFactory(typeof(Border));
                pBrd.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
                pBrd.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
                pBrd.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(55, 45, 30)));
                pBrd.SetValue(Border.BorderThicknessProperty, new Thickness(1));
                FrameworkElementFactory pCp = new FrameworkElementFactory(typeof(ContentPresenter));
                pCp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                pCp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                pBrd.AppendChild(pCp);
                pTemp.VisualTree = pBrd;
                btnPreset.Template = pTemp;

                int capturedRv = rv;
                btnPreset.Click += (s, e) => {
                    sldRam.Value = capturedRv;
                };
                btnPreset.MouseEnter += (s, e) => {
                    btnPreset.Background = new SolidColorBrush(Color.FromRgb(30, 40, 60));
                };
                btnPreset.MouseLeave += (s, e) => {
                    btnPreset.Background = new SolidColorBrush(Color.FromRgb(14, 19, 29));
                };
                ramPresets.Children.Add(btnPreset);
            }
            sStack.Children.Add(ramPresets);

            // Custom Styled Fullscreen Checkbox
            CheckBox chkFull = new CheckBox {
                Content = "Tam Ekran Başlat (Fullscreen)",
                IsChecked = config.Fullscreen,
                Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 245)),
                FontSize = 12.5,
                Margin = new Thickness(0, 0, 0, 22),
                Cursor = Cursors.Hand,
                FocusVisualStyle = null
            };

            string chkXaml = @"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='CheckBox'>
                <StackPanel Orientation='Horizontal' VerticalAlignment='Center'>
                    <Border x:Name='chkBoxBorder' Width='22' Height='22' CornerRadius='6' Background='#0A0E15' BorderBrush='#65502A' BorderThickness='1.4' Margin='0,0,10,0'>
                        <Path x:Name='chkMark' Data='M 4 8 L 8 12 L 15 4' Stroke='White' StrokeThickness='2' StrokeStartLineCap='Round' StrokeEndLineCap='Round' HorizontalAlignment='Center' VerticalAlignment='Center' Visibility='Collapsed'/>
                    </Border>
                    <ContentPresenter VerticalAlignment='Center'/>
                </StackPanel>
                <ControlTemplate.Triggers>
                    <Trigger Property='IsChecked' Value='True'>
                        <Setter TargetName='chkBoxBorder' Property='Background'>
                            <Setter.Value>
                                <LinearGradientBrush StartPoint='0,0' EndPoint='1,1'>
                                    <GradientStop Color='#DAB23E' Offset='0.0'/>
                                    <GradientStop Color='#B8860B' Offset='1.0'/>
                                </LinearGradientBrush>
                            </Setter.Value>
                        </Setter>
                        <Setter TargetName='chkBoxBorder' Property='BorderBrush' Value='#FFEAA7'/>
                        <Setter TargetName='chkMark' Property='Visibility' Value='Visible'/>
                        <Setter TargetName='chkBoxBorder' Property='Effect'>
                            <Setter.Value>
                                <DropShadowEffect Color='#DAB23E' BlurRadius='8' Opacity='0.75' ShadowDepth='0'/>
                            </Setter.Value>
                        </Setter>
                    </Trigger>
                    <Trigger Property='IsMouseOver' Value='True'>
                        <Setter TargetName='chkBoxBorder' Property='BorderBrush' Value='#FAB923'/>
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>";
            try {
                chkFull.Template = (ControlTemplate)XamlReader.Parse(chkXaml);
            } catch { }

            sStack.Children.Add(chkFull);

            // Save Button (Custom Styled ControlTemplate to prevent Aero2 baby-blue hover!)
            Button btnSave = new Button {
                Content = "AYARLARI KAYDET",
                FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI, Roboto, sans-serif"),
                Height = 46,
                Background = new LinearGradientBrush(GoldPrimary, Color.FromRgb(185, 115, 18), 45),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FocusVisualStyle = null,
                Focusable = false,
                IsTabStop = false
            };

            ControlTemplate saveTemplate = new ControlTemplate(typeof(Button));
            FrameworkElementFactory saveBorderFactory = new FrameworkElementFactory(typeof(Border));
            saveBorderFactory.Name = "saveBtnBorder";
            saveBorderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(9));
            saveBorderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            saveBorderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(255, 238, 175)));
            saveBorderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1.2));

            FrameworkElementFactory saveContentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            saveContentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            saveContentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            saveBorderFactory.AppendChild(saveContentFactory);
            saveTemplate.VisualTree = saveBorderFactory;
            btnSave.Template = saveTemplate;

            DropShadowEffect saveShadow = new DropShadowEffect {
                Color = GoldPrimary,
                BlurRadius = 14,
                Opacity = 0.45,
                ShadowDepth = 1
            };
            btnSave.Effect = saveShadow;

            btnSave.MouseEnter += (s, e) => {
                btnSave.Background = new LinearGradientBrush(GoldLight, GoldPrimary, 45);
                DoubleAnimation animShadow = new DoubleAnimation(0.85, TimeSpan.FromMilliseconds(160));
                DoubleAnimation animBlur = new DoubleAnimation(24, TimeSpan.FromMilliseconds(160));
                saveShadow.BeginAnimation(DropShadowEffect.OpacityProperty, animShadow);
                saveShadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, animBlur);
            };

            btnSave.MouseLeave += (s, e) => {
                btnSave.Background = new LinearGradientBrush(GoldPrimary, Color.FromRgb(185, 115, 18), 45);
                DoubleAnimation animShadow = new DoubleAnimation(0.45, TimeSpan.FromMilliseconds(180));
                DoubleAnimation animBlur = new DoubleAnimation(14, TimeSpan.FromMilliseconds(180));
                saveShadow.BeginAnimation(DropShadowEffect.OpacityProperty, animShadow);
                saveShadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, animBlur);
            };

            btnSave.Click += (s, e) => {
                config.MaxRamMb = (int)sldRam.Value * 1024;
                config.Fullscreen = chkFull.IsChecked ?? false;

                SaveConfig();
                txtRamBadge.Text = "🛡️ " + (config.MaxRamMb / 1024) + " GB RAM • G1GC Zırhı";
                DoPing();
                settingsWin.Close();
            };
            sStack.Children.Add(btnSave);

            sBorder.Child = sStack;
            modalGutter.Children.Add(sBorder);
            settingsWin.Content = modalGutter;
            settingsWin.ShowDialog();
        }
    }

    public class App : Application {
        [STAThread]
        public static void Main() {
            try {
                App app = new App();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                SplashWindow splash = new SplashWindow();
                splash.ShowDialog();

                MainWindow main = new MainWindow();
                app.MainWindow = main;
                app.ShutdownMode = ShutdownMode.OnMainWindowClose;
                app.Run(main);
            } catch { }
        }
    }
}
