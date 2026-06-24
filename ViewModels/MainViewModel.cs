using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Input;
using SystemMediaFlyouts.Messages;
using SystemMediaFlyouts.Services;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows;

namespace SystemMediaFlyouts.ViewModels
{
    public partial class MainViewModel : ObservableObject,
        IRecipient<SettingsChangedMessage>,
        IRecipient<VolumeChangedMessage>,
        IRecipient<BrightnessChangedMessage>,
        IRecipient<MediaChangedMessage>,
        IRecipient<MediaProgressChangedMessage>
    {
        private readonly HardwareService _hardwareService;
        private bool _isUpdatingFromOs = false; // Sonsuz döngü engelliyici

        [ObservableProperty] private bool _isMediaModuleEnabled;
        [ObservableProperty] private bool _isVolumeModuleEnabled;
        [ObservableProperty] private bool _isBrightnessModuleEnabled;

        [ObservableProperty] private int _currentVolume;
        [ObservableProperty] private int _currentBrightness;
        [ObservableProperty] private string _mediaTitle = "Bekleniyor...";
        [ObservableProperty] private string _mediaArtist = "-";
        [ObservableProperty] private bool _isMediaPlaying;
        [ObservableProperty] private BitmapImage? _mediaThumbnail;
        [ObservableProperty] private double _mediaProgress; // Zaman çizgisi için

        public MainViewModel(SettingsService settingsService, HardwareService hardwareService)
        {
            _hardwareService = hardwareService; // Servisi aldık

            var initialSettings = settingsService.CurrentSettings;
            IsMediaModuleEnabled = initialSettings.IsMediaModuleEnabled;
            IsVolumeModuleEnabled = initialSettings.IsVolumeModuleEnabled;
            IsBrightnessModuleEnabled = initialSettings.IsBrightnessModuleEnabled;

            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        // --- BUTON KOMUTLARI ---
        [RelayCommand] private async Task TogglePlayPause() => await _hardwareService.TogglePlayPauseAsync();
        [RelayCommand] private async Task SkipNext() => await _hardwareService.SkipNextAsync();
        [RelayCommand] private async Task SkipPrevious() => await _hardwareService.SkipPreviousAsync();

        // --- SLIDER DEĞİŞİM TETİKLEYİCİLERİ ---
        // Kullanıcı arayüzden slider'ı kaydırdığında bu metotlar otomatik tetiklenir
        partial void OnCurrentVolumeChanged(int value)
        {
            if (!_isUpdatingFromOs) _hardwareService.SetVolume(value);
        }

        partial void OnCurrentBrightnessChanged(int value)
        {
            if (!_isUpdatingFromOs) _hardwareService.SetBrightness(value);
        }

        // --- İŞLETİM SİSTEMİNDEN GELEN MESAJLAR ---
        public void Receive(SettingsChangedMessage message)
        {
            // UI Thread'ine (Ana iş parçacığına) geçiş yapıyoruz
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsMediaModuleEnabled = message.Value.IsMediaModuleEnabled;
                IsVolumeModuleEnabled = message.Value.IsVolumeModuleEnabled;
                IsBrightnessModuleEnabled = message.Value.IsBrightnessModuleEnabled;
            });
        }


        public void Receive(VolumeChangedMessage message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _isUpdatingFromOs = true;
                CurrentVolume = message.Value;
                _isUpdatingFromOs = false;
            });
        }

        public void Receive(BrightnessChangedMessage message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _isUpdatingFromOs = true;
                CurrentBrightness = message.Value;
                _isUpdatingFromOs = false;
            });
        }

        public void Receive(MediaChangedMessage message)
        {
            // UI Thread'ine (Ana iş parçacığına) geçiş yapıyoruz
            Application.Current.Dispatcher.Invoke(() =>
            {
                MediaTitle = message.Value.Title;
                MediaArtist = message.Value.Artist;
                IsMediaPlaying = message.Value.IsPlaying;

                if (message.Value.ThumbnailData != null)
                {
                    using var ms = new MemoryStream(message.Value.ThumbnailData);
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                    MediaThumbnail = image;
                }
                else
                {
                    MediaThumbnail = null;
                }
            });
        }
        // Yeni Eklenen Metot
        public void Receive(MediaProgressChangedMessage message)
        {
            // Thumbnail vb. ağır işlemleri yapmadan sadece ProgressBar'ı günceller
            Application.Current.Dispatcher.Invoke(() =>
            {
                MediaProgress = message.Value;
            });
        }
    }
}