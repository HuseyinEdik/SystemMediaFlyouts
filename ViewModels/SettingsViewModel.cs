using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Reflection;
using SystemMediaFlyouts.Models;
using SystemMediaFlyouts.Services;
using Windows.Globalization;
using IWshRuntimeLibrary;
using System.IO;

namespace SystemMediaFlyouts.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly SettingsService _settingsService;
        private bool _isInitializing = true;
        private bool _isApplyingLanguage;
        private string _appliedLanguage = "tr-TR";

        // Değişkenler
        [ObservableProperty]
        private string _currentPage = "General"; // Varsayılan açılış sayfası
        [ObservableProperty]
        private bool _isRunAtStartupEnabled;  // Başlangıçta çalıştırma ayarı
        [ObservableProperty]
        private double _panelScale; // Panel ölçeklendirme ayarı 
        [ObservableProperty]
        private string _language; // Dil ayarı

        [ObservableProperty] private AppTheme _theme;                       // Tema ayarı
        [ObservableProperty] private FlyoutPosition _position;              // Panel konumu
        [ObservableProperty] private bool _isControlBarEnabled;             // Kontrol çubuğu görünürlüğü
        [ObservableProperty] private bool _isMediaModuleEnabled;            // Medya modülü ayarı
        [ObservableProperty] private bool _isVolumeModuleEnabled;           // Ses modülü ayarı
        [ObservableProperty] private bool _isBrightnessModuleEnabled;       // Parlaklık modülü ayarı

        private readonly ObservableCollection<LocalizedOption<AppTheme>> _availableThemes = new();
        private readonly ObservableCollection<LocalizedOption<FlyoutPosition>> _availablePositions = new();

        public ObservableCollection<LocalizedOption<AppTheme>> AvailableThemes => _availableThemes;
        public ObservableCollection<LocalizedOption<FlyoutPosition>> AvailablePositions => _availablePositions;

        // Dil listesi her zaman o dilin kendi orijinal adıyla kalmalı (Dinamik çeviri yapılmaz)
        public Dictionary<string, string> AvailableLanguages { get; } = new()
        {
            { "tr-TR", "Türkçe" },
            { "en-US", "English" }
        };

        private static string NormalizeCultureCode(string? cultureCode)
        {
            return string.Equals(cultureCode, "en-US", StringComparison.OrdinalIgnoreCase)
                ? "en-US"
                : "tr-TR";
        }

        // Otomatik sürüm numarasını çeken özellik
        public string AppVersion => $"Versiyon {Assembly.GetExecutingAssembly().GetName().Version}";

        private static string GetLocalizedString(string key, string fallback) =>
            System.Windows.Application.Current?.TryFindResource(key) as string ?? fallback;

        private void InitializeLocalizedOptions()
        {
            if (_availableThemes.Count == 0)
            {
                _availableThemes.Add(new LocalizedOption<AppTheme>(AppTheme.System, string.Empty));
                _availableThemes.Add(new LocalizedOption<AppTheme>(AppTheme.Light, string.Empty));
                _availableThemes.Add(new LocalizedOption<AppTheme>(AppTheme.Dark, string.Empty));
            }

            if (_availablePositions.Count == 0)
            {
                _availablePositions.Add(new LocalizedOption<FlyoutPosition>(FlyoutPosition.TopLeft, string.Empty));
                _availablePositions.Add(new LocalizedOption<FlyoutPosition>(FlyoutPosition.TopCenter, string.Empty));
                _availablePositions.Add(new LocalizedOption<FlyoutPosition>(FlyoutPosition.TopRight, string.Empty));
                _availablePositions.Add(new LocalizedOption<FlyoutPosition>(FlyoutPosition.BottomLeft, string.Empty));
                _availablePositions.Add(new LocalizedOption<FlyoutPosition>(FlyoutPosition.BottomCenter, string.Empty));
                _availablePositions.Add(new LocalizedOption<FlyoutPosition>(FlyoutPosition.BottomRight, string.Empty));
            }
        }

        private void RefreshLocalizedOptions()
        {
            InitializeLocalizedOptions();

            _availableThemes[0].DisplayName = GetLocalizedString("Lang_ThemeSystem", "System Theme");
            _availableThemes[1].DisplayName = GetLocalizedString("Lang_ThemeLight", "Light Theme");
            _availableThemes[2].DisplayName = GetLocalizedString("Lang_ThemeDark", "Dark Theme");

            _availablePositions[0].DisplayName = GetLocalizedString("Lang_PosTopLeft", "Top Left");
            _availablePositions[1].DisplayName = GetLocalizedString("Lang_PosTopCenter", "Top Center");
            _availablePositions[2].DisplayName = GetLocalizedString("Lang_PosTopRight", "Top Right");
            _availablePositions[3].DisplayName = GetLocalizedString("Lang_PosBottomLeft", "Bottom Left");
            _availablePositions[4].DisplayName = GetLocalizedString("Lang_PosBottomCenter", "Bottom Center");
            _availablePositions[5].DisplayName = GetLocalizedString("Lang_PosBottomRight", "Bottom Right");
        }

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
            _appliedLanguage = NormalizeCultureCode(current.Language);
            Language = _appliedLanguage;
            RefreshLocalizedOptions();
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
                Language = Language,
            };

            _settingsService.SaveSettings(updatedSettings);
        }

        // Değişken tetiklendiğinde Kayıt Defterini günceller
        partial void OnIsRunAtStartupEnabledChanged(bool value)
        {
            if (_isInitializing) return;
            SetStartup(value);
        }

        // Windows Başlangıç Klasörüne kısayol ekleyen KESİN ve GÜVENLİ metot
        private void SetStartup(bool enable)
        {
            try
            {
                string startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                string shortcutPath = Path.Combine(startupPath, "SystemMediaFlyouts.lnk");

                if (enable)
                {
                    string? appPath = Environment.ProcessPath;
                    if (string.IsNullOrEmpty(appPath)) return;

                    // Kütüphaneyi doğrudan kullanarak %100 garantili kısayol oluşturma
                    WshShell shell = new WshShell();
                    IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
                    shortcut.TargetPath = appPath;

                    // Çalışma dizinini belirlemek, uygulamanın doğru klasörden uyanması için çok kritiktir
                    shortcut.WorkingDirectory = Path.GetDirectoryName(appPath);

                    shortcut.Save();
                }
                else
                {
                    // Ayar kapatıldıysa kısayolu siliyoruz
                    if (System.IO.File.Exists(shortcutPath))
                    {
                        System.IO.File.Delete(shortcutPath);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Başlangıç ayarı değiştirilemedi:\n{ex.Message}", "Sistem Hatası", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        // Başlangıçta ayarın açık olup olmadığını kısayol dosyasına bakarak denetleyen metot
        private bool CheckStartup()
        {
            try
            {
                string startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                string shortcutPath = Path.Combine(startupPath, "SystemMediaFlyouts.lnk");

                return System.IO.File.Exists(shortcutPath);
            }
            catch
            {
                return false;
            }
        }

        // Kullanıcı ComboBox'tan dili değiştirdiği an tetiklenir
        partial void OnLanguageChanged(string value)
        {
            if (_isInitializing || _isApplyingLanguage)
            {
                return;
            }

            var cultureCode = NormalizeCultureCode(value);
            if (!ApplyLanguage(cultureCode))
            {
                _isApplyingLanguage = true;
                try
                {
                    Language = _appliedLanguage;
                }
                finally
                {
                    _isApplyingLanguage = false;
                }

                return;
            }

            _appliedLanguage = cultureCode;
            RefreshLocalizedOptions();
            SaveAndBroadcast();
        }

        // Eski dil dosyasını söküp yeni dil dosyasını XAML motoruna enjekte eden metot
        public static bool ApplyLanguage(string cultureCode)
        {
            var normalizedCulture = NormalizeCultureCode(cultureCode);

            try
            {
                var dictionaries = System.Windows.Application.Current.Resources.MergedDictionaries;
                var newDictionary = new System.Windows.ResourceDictionary
                {
                    Source = new Uri($"pack://application:,,,/Languages/StringResources.{normalizedCulture}.xaml", UriKind.Absolute)
                };

                // Mevcut yüklü olan dil dosyasını bul (İçinde 'Languages/StringResources' geçen)
                var langDict = dictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("/Languages/StringResources."));

                if (langDict != null)
                {
                    dictionaries.Remove(langDict); // Eskisini sil
                }

                // Yeni seçilen dili sisteme ekle
                dictionaries.Add(newDictionary);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dil uygulanırken hata oluştu: {ex.Message}");
                return false;
            }
        }

        public sealed class LocalizedOption<TKey> : ObservableObject
        {
            private string _displayName;

            public LocalizedOption(TKey key, string displayName)
            {
                Key = key;
                _displayName = displayName;
            }

            public TKey Key { get; }

            public string DisplayName
            {
                get => _displayName;
                set => SetProperty(ref _displayName, value);
            }
        }
    }
}