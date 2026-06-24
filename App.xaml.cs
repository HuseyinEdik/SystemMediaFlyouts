using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Data;
using System.Windows;
using SystemMediaFlyouts.Core;
using SystemMediaFlyouts.Services;
using SystemMediaFlyouts.ViewModels;
using SystemMediaFlyouts.Views;

namespace SystemMediaFlyouts
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // 1. DEĞİŞKENLER BURADA OLMALI (Metotların dışında, sınıfın hemen içinde)
        private System.Windows.Forms.NotifyIcon? _notifyIcon;
        private Views.SettingsWindow? _settingsWindow;
        public new static App Current => (App)Application.Current;
        public IServiceProvider Services { get; }

        public App()
        {
            Services = ConfigureServices();

            this.DispatcherUnhandledException += (s, e) =>
            {
                System.Windows.MessageBox.Show($"Kritik bir hata oluştu:\n{e.Exception.Message}\n\nDetay:\n{e.Exception.InnerException?.Message}",
                                               "Sistem Hatası",
                                               System.Windows.MessageBoxButton.OK,
                                               System.Windows.MessageBoxImage.Error);
                e.Handled = true; // Uygulamanın anında kapanmasını engelle
            };
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // 1. Core ve Servislerin Kaydı (Uygulama boyunca tekil - Singleton)
            services.AddSingleton<SettingsService>();
            services.AddSingleton<WindowManager>();

            // 2. ViewModel'ların Kaydı (Geçici - Transient veya Singleton)
            // Ayarlar her zaman aynı durumu yansıtacağı için Singleton idealdir.
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<HardwareService>();

            return services.BuildServiceProvider();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Pencere gizlendiğinde uygulamanın kapanmasını engeller. Sadece Tray İkonundan "Çıkış" denilince kapanır.
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // 1. Pencereyi Oluştur ve DataContext'i Bağla
            var flyoutWindow = new Views.FlyoutWindow
            {
                DataContext = Services.GetRequiredService<ViewModels.MainViewModel>()
            };

            // 2. Pencereyi Yöneticimize (WindowManager) Kaydet
            var windowManager = Services.GetRequiredService<Core.WindowManager>();
            windowManager.RegisterFlyoutWindow(flyoutWindow);

            // İlk konumu ayarlamak için manuel tetikleme
            var currentSettings = Services.GetRequiredService<Services.SettingsService>().CurrentSettings;
            windowManager.UpdateWindowPosition(currentSettings.Position);
            // TEMAYI UYGULA
            windowManager.UpdateTheme(currentSettings.Theme);

            // 3. Donanım Servisini Başlat (Eski tanımlamalar silindi, sadece bu kalmalı)
            var hardwareService = Services.GetRequiredService<HardwareService>();
            await hardwareService.InitializeAsync();

            // 1. AYARLAR PENCERESİNİ OLUŞTUR (Fakat Show yapma, gizli başlasın)
            _settingsWindow = new Views.SettingsWindow
            {
                DataContext = Services.GetRequiredService<ViewModels.SettingsViewModel>()
            };

            // 2. TRAY (SİSTEM TEPSİSİ) İKONUNU OLUŞTUR
            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                // Dışarıdan resim istemez, uygulamanın kendi exe ikonunu otomatik çeker
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location),
                Visible = true,
                Text = "System Media Flyouts"
            };

            // Çift tıklayınca ayarları aç
            _notifyIcon.DoubleClick += (s, args) =>
            {
                _settingsWindow.Show();
                _settingsWindow.Activate();
            };

            // Sağ Tık Menüsü Oluştur
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            contextMenu.Items.Add("Ayarlar", null, (s, args) => { _settingsWindow.Show(); _settingsWindow.Activate(); });
            contextMenu.Items.Add("Çıkış", null, (s, args) =>
            {
                _notifyIcon.Visible = false; // Çıkarken ikonu temizle
                Current.Shutdown();
            });
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

    
    }

}
