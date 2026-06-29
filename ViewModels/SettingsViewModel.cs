using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Diagnostics;
using SystemMediaFlyouts.Models;
using SystemMediaFlyouts.Services;
using System.Reflection;
using Microsoft.Win32;

namespace SystemMediaFlyouts.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly SettingsService _settingsService;
        private bool _isInitializing = true;

        // ----- YENİ EKLENEN SAYFA VE MENÜ YÖNETİMİ -----
        [ObservableProperty]
        private string _currentPage = "General"; // Varsayılan açılış sayfası
        [ObservableProperty] 
        private bool _isRunAtStartupEnabled;  // Başlangıçta çalıştırma ayarı
        [ObservableProperty] 
        private double _panelScale; // Panel ölçeklendirme ayarı 

        // Otomatik sürüm numarasını çeken özellik
        public string AppVersion => $"Versiyon {Assembly.GetExecutingAssembly().GetName().Version}";

        [RelayCommand]
        private void Navigate(string pageName)
        {
            CurrentPage = pageName;
        }

        //Github sayfası yönlendirmesi
        [RelayCommand]
        private void OpenGitHub()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/HuseyinEdik/SystemMediaFlyouts",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        //Github Hatalar sayfası yönlendirme 
        [RelayCommand]
        private void OpenGitHubIssues()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                { 
                    FileName = "https://github.com/HuseyinEdik/SystemMediaFlyouts/issues",
                    UseShellExecute = true
                });
            }
            catch { }
        }
        // -----------------------------------------------

        public Dictionary<AppTheme, string> AvailableThemes { get; } = new Dictionary<AppTheme, string>
        {
            { AppTheme.System, "Sistem Teması" },
            { AppTheme.Light, "Açık Tema" },
            { AppTheme.Dark, "Koyu Tema" }
        };

        public Dictionary<FlyoutPosition, string> AvailablePositions { get; } = new Dictionary<FlyoutPosition, string>
        {
            { FlyoutPosition.TopLeft, "Sol Üst" },
            { FlyoutPosition.TopCenter, "Üst Orta" },
            { FlyoutPosition.TopRight, "Sağ Üst" },
            { FlyoutPosition.BottomLeft, "Sol Alt" },
            { FlyoutPosition.BottomCenter, "Alt Orta" },
            { FlyoutPosition.BottomRight, "Sağ Alt" }
        };

        [ObservableProperty] private AppTheme _theme;
        [ObservableProperty] private FlyoutPosition _position;
        [ObservableProperty] private bool _isControlBarEnabled;
        [ObservableProperty] private bool _isMediaModuleEnabled;
        [ObservableProperty] private bool _isVolumeModuleEnabled;
        [ObservableProperty] private bool _isBrightnessModuleEnabled;

        private int _displayDuration;
        public int DisplayDuration
        {
            get => _displayDuration;
            set { if (SetProperty(ref _displayDuration, value)) SaveAndBroadcast(); }
        }

        public SettingsViewModel(SettingsService settingsService)
        {
            _settingsService = settingsService;
            var current = _settingsService.CurrentSettings;

            Theme = current.Theme;
            Position = current.Position;
            IsControlBarEnabled = current.IsControlBarEnabled;
            IsMediaModuleEnabled = current.IsMediaModuleEnabled;
            IsVolumeModuleEnabled = current.IsVolumeModuleEnabled;
            IsBrightnessModuleEnabled = current.IsBrightnessModuleEnabled;
            DisplayDuration = current.DisplayDuration;
            PanelScale = current.PanelScale;
            IsRunAtStartupEnabled = CheckStartup();

            _isInitializing = false;
        }

        partial void OnThemeChanged(AppTheme value) => SaveAndBroadcast();
        partial void OnPositionChanged(FlyoutPosition value) => SaveAndBroadcast();
        partial void OnIsControlBarEnabledChanged(bool value) => SaveAndBroadcast();
        partial void OnIsMediaModuleEnabledChanged(bool value) => SaveAndBroadcast();
        partial void OnIsVolumeModuleEnabledChanged(bool value) => SaveAndBroadcast();
        partial void OnIsBrightnessModuleEnabledChanged(bool value) => SaveAndBroadcast();
        partial void OnPanelScaleChanged(double value) => SaveAndBroadcast();



        private void SaveAndBroadcast()
        {
            if (_isInitializing) return;

            var updatedSettings = new AppSettings
            {
                Theme = Theme,
                Position = Position,
                IsControlBarEnabled = IsControlBarEnabled,
                IsMediaModuleEnabled = IsMediaModuleEnabled,
                IsVolumeModuleEnabled = IsVolumeModuleEnabled,
                IsBrightnessModuleEnabled = IsBrightnessModuleEnabled,
                DisplayDuration = DisplayDuration,
                PanelScale = PanelScale,
            };

            _settingsService.SaveSettings(updatedSettings);
        }

        // Değişken tetiklendiğinde Kayıt Defterini günceller
        partial void OnIsRunAtStartupEnabledChanged(bool value)
        {
            if (_isInitializing) return;
            SetStartup(value);
        }

        // Windows Kayıt Defterine uygulamayı ekleyen/çıkaran metot
        private void SetStartup(bool enable)
        {
            try
            {
                string appName = "SystemMediaFlyouts";
                // Uygulamanın .exe yolunu otomatik bul
                string appPath = Process.GetCurrentProcess().MainModule.FileName;
                using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

                if (enable)
                    key.SetValue(appName, $"\"{appPath}\"");
                else
                    key.DeleteValue(appName, false);
            }
            catch { }
        }

        // Başlangıçta ayarın açık olup olmadığını denetleyen metot
        private bool CheckStartup()
        {
            try
            {
                string appName = "SystemMediaFlyouts";
                using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                return key?.GetValue(appName) != null;
            }
            catch
            {
                return false;
            }
        }
    }
}