using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Diagnostics;
using SystemMediaFlyouts.Models;
using SystemMediaFlyouts.Services;
using System.Reflection;

namespace SystemMediaFlyouts.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly SettingsService _settingsService;
        private bool _isInitializing = true;

        // ----- YENİ EKLENEN SAYFA VE MENÜ YÖNETİMİ -----
        [ObservableProperty]
        private string _currentPage = "Appearance"; // Varsayılan açılış sayfası

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
            IsMediaModuleEnabled = current.IsMediaModuleEnabled;
            IsVolumeModuleEnabled = current.IsVolumeModuleEnabled;
            IsBrightnessModuleEnabled = current.IsBrightnessModuleEnabled;
            DisplayDuration = current.DisplayDuration;

            _isInitializing = false;
        }

        partial void OnThemeChanged(AppTheme value) => SaveAndBroadcast();
        partial void OnPositionChanged(FlyoutPosition value) => SaveAndBroadcast();
        partial void OnIsMediaModuleEnabledChanged(bool value) => SaveAndBroadcast();
        partial void OnIsVolumeModuleEnabledChanged(bool value) => SaveAndBroadcast();
        partial void OnIsBrightnessModuleEnabledChanged(bool value) => SaveAndBroadcast();

        private void SaveAndBroadcast()
        {
            if (_isInitializing) return;

            var updatedSettings = new AppSettings
            {
                Theme = Theme,
                Position = Position,
                IsMediaModuleEnabled = IsMediaModuleEnabled,
                IsVolumeModuleEnabled = IsVolumeModuleEnabled,
                IsBrightnessModuleEnabled = IsBrightnessModuleEnabled,
                DisplayDuration = DisplayDuration
            };

            _settingsService.SaveSettings(updatedSettings);
        }
    }
}