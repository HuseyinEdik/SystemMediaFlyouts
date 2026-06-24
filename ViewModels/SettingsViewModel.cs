using CommunityToolkit.Mvvm.ComponentModel;
using System;
using SystemMediaFlyouts.Models;
using SystemMediaFlyouts.Services;

namespace SystemMediaFlyouts.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly SettingsService _settingsService;
        private bool _isInitializing = true; // Yeni Kilit Değişkeni

        // ComboBox'lar için Enum listeleri
        public Array AvailableThemes => Enum.GetValues(typeof(AppTheme));
        public Array AvailablePositions => Enum.GetValues(typeof(FlyoutPosition));

        // MVVM Toolkit arka planda Public özelliklerini otomatik üretecek
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

            // Alt tireli field'lar yerine doğrudan özellikleri tetikliyoruz ki UI uyansın
            Theme = current.Theme;
            Position = current.Position;
            IsMediaModuleEnabled = current.IsMediaModuleEnabled;
            IsVolumeModuleEnabled = current.IsVolumeModuleEnabled;
            IsBrightnessModuleEnabled = current.IsBrightnessModuleEnabled;
            DisplayDuration = current.DisplayDuration;



            _isInitializing = false; // Kilit açıldı, artık kullanıcı değiştirirse kaydolacak
        }

        // Özellikler değiştiği AN tetiklenen otomatik metotlar (XAML'daki UpdateSourceTrigger=PropertyChanged sayesinde anında çalışır)
        partial void OnThemeChanged(AppTheme value) => SaveAndBroadcast();
        partial void OnPositionChanged(FlyoutPosition value) => SaveAndBroadcast();
        partial void OnIsMediaModuleEnabledChanged(bool value) => SaveAndBroadcast();
        partial void OnIsVolumeModuleEnabledChanged(bool value) => SaveAndBroadcast();
        partial void OnIsBrightnessModuleEnabledChanged(bool value) => SaveAndBroadcast();

        private void SaveAndBroadcast()
        {
            if (_isInitializing) return; // Başlangıç aşamasındaysa kaydetmeyi engelle

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