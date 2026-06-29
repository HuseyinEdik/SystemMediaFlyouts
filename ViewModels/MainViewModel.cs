using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;
using SystemMediaFlyouts.Messages;
using SystemMediaFlyouts.Services;

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
        private readonly SettingsService _settingsService; // Ayarlar servisini saklamak için ekledik
        private bool _isUpdatingFromOs = false; // Sonsuz döngü engelleyici

        [ObservableProperty] private bool _isControlBarEnabled;
        [ObservableProperty] private bool _isMediaModuleEnabled;
        [ObservableProperty] private bool _isVolumeModuleEnabled;
        [ObservableProperty] private bool _isBrightnessModuleEnabled;

        // Medya Zaman Çizgisi Değişkenleri
        [ObservableProperty] private string _totalDurationString = "00:00"; // Sağdaki toplam süre
        [ObservableProperty] private string _currentPositionString = "00:00"; // Soldaki anlık süre
        [ObservableProperty] private double _totalDurationSeconds = 1; // Sürgünün maksimum değeri (0'a bölme hatasını önlemek için 1)
        [ObservableProperty] private double _currentPositionSeconds = 0; // Sürgünün anlık konumu

        // Arka planda her saniye çalışacak saatimiz
        private System.Windows.Threading.DispatcherTimer _timelineTimer;

        [ObservableProperty] private int _currentVolume;
        [ObservableProperty] private int _currentBrightness;
        [ObservableProperty] private string _mediaTitle = "Bekleniyor...";
        [ObservableProperty] private string _mediaArtist = "-";
        [ObservableProperty] private bool _isMediaPlaying; 
        [ObservableProperty] private BitmapImage? _mediaThumbnail; 
        [ObservableProperty] private double _mediaProgress; // Zaman çizgisi için
        [ObservableProperty] private double _panelScale; // Panel ölçeklendirme

        public MainViewModel(SettingsService settingsService, HardwareService hardwareService)
        {
            _hardwareService = hardwareService; // Servisi aldık
            _settingsService = settingsService;

            var initialSettings = settingsService.CurrentSettings;
            IsControlBarEnabled = initialSettings.IsControlBarEnabled;
            IsMediaModuleEnabled = initialSettings.IsMediaModuleEnabled;
            IsVolumeModuleEnabled = initialSettings.IsVolumeModuleEnabled;
            IsBrightnessModuleEnabled = initialSettings.IsBrightnessModuleEnabled;
            PanelScale = initialSettings.PanelScale;

            WeakReferenceMessenger.Default.RegisterAll(this);

            // 1 saniye yerine 250 milisaniyede bir tetiklenecek şekilde hızlandırıyoruz
            _timelineTimer = new System.Windows.Threading.DispatcherTimer();
            _timelineTimer.Interval = TimeSpan.FromMilliseconds(250);
            _timelineTimer.Tick += TimelineTimer_Tick;
            _timelineTimer.Start();
        }

        // --- BUTON KOMUTLARI ---
        [RelayCommand] private async Task TogglePlayPause() => await _hardwareService.TogglePlayPauseAsync();
        [RelayCommand] private async Task SkipNext() => await _hardwareService.SkipNextAsync();
        [RelayCommand] private async Task SkipPrevious() => await _hardwareService.SkipPreviousAsync();

        // Sabitleme durumunu tutan değişken (Kaynak oluşturucu hatasını önlemek için manuel yazıldı)
        private bool _isPinned;
        public bool IsPinned
        {
            get => _isPinned;
            set => SetProperty(ref _isPinned, value);
        }

        // 1. Sabitleme (Pin) Butonu Komutu
        [RelayCommand]
        private void TogglePin()
        {
            IsPinned = !IsPinned;
        }

        // 2. Paneli Gizle (Hide) Butonu Komutu
        [RelayCommand]
        private void HidePanel()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Sabitleme ne olursa olsun, FlyoutWindow'u bul ve anında gizle
                var flyout = Application.Current.Windows.OfType<Views.FlyoutWindow>().FirstOrDefault();
                flyout?.Hide();
            });
        }

        // --- GÜNCELLENEN AYARLARI AÇ KOMUTU ---
        [RelayCommand]
        private void OpenSettings()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    // App.xaml.cs tarafından arka planda oluşturulmuş, DataContext'i dolu o pencereyi bul
                    var settingsWin = Application.Current.Windows.OfType<Views.SettingsWindow>().FirstOrDefault();

                    if (settingsWin != null)
                    {
                        // Gizli pencereyi ekrana çizmek ve Mesajı (SettingsOpenedMessage) tetiklemek için Show ŞART!
                        settingsWin.Show();
                        settingsWin.WindowState = WindowState.Normal;
                        settingsWin.Activate();
                    }
                    else
                    {
                        // Pencere bir şekilde tamamen kapatılmış/yok edilmişse, 
                        // Dependency Injection (DI) üzerinden her şeyiyle hazır yeni bir tane talep et
                        var newSettingsWin = App.Current.Services.GetService(typeof(Views.SettingsWindow)) as Views.SettingsWindow;
                        newSettingsWin?.Show();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ayarlar penceresi açılırken hata oluştu: {ex.Message}");
                }
            });
        }

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
                IsControlBarEnabled = message.Value.IsControlBarEnabled;
                IsMediaModuleEnabled = message.Value.IsMediaModuleEnabled;
                IsVolumeModuleEnabled = message.Value.IsVolumeModuleEnabled;
                IsBrightnessModuleEnabled = message.Value.IsBrightnessModuleEnabled;
                PanelScale = message.Value.PanelScale;
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

        // Medya servisi her ilerlediğinde (veya yeni şarkı açıldığında) bu metodu çağıracaksın
        public void UpdateMediaTimeline(TimeSpan currentPosition, TimeSpan totalDuration)
        {
            // Eğer süreler geçersizse (örneğin YouTube'da reklam çıkmışsa) çökmemesi için güvenlik kontrolü
            if (totalDuration.TotalSeconds <= 0) return;

            // 1. Sürgü (Slider) için matematiksel saniye değerlerini ata
            TotalDurationSeconds = totalDuration.TotalSeconds;
            CurrentPositionSeconds = currentPosition.TotalSeconds;

            // 2. Metinler (TextBlock) için saniyeleri "03:45" gibi şık bir formata çevir
            // Eğer şarkı 1 saatten uzunsa saat kısmını da eklemek için: @"hh\:mm\:ss" kullanabilirsin
            TotalDurationString = totalDuration.ToString(@"mm\:ss");
            CurrentPositionString = currentPosition.ToString(@"mm\:ss");
        }
        private void TimelineTimer_Tick(object? sender, EventArgs e)
        {
            var session = _hardwareService?.GetCurrentSession();

            if (session != null)
            {
                // 1. Oynat/Duraklat Butonunun Durumunu Güncelle
                var playbackInfo = session.GetPlaybackInfo();
                IsMediaPlaying = playbackInfo?.PlaybackStatus == Windows.Media.Control.GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

                // 2. Zaman Çizgisi ve Süre Metinlerini Güncelle
                var timelineProperties = session.GetTimelineProperties();
                if (timelineProperties != null && timelineProperties.EndTime.TotalSeconds > 0)
                {
                    TimeSpan currentPosition = timelineProperties.Position;

                    // Windows süre bilgisini geç günceller. Eğer şarkı oynatılıyorsa, 
                    // en son güncellemeden bu yana geçen süreyi ekleyerek slider'ı yağ gibi akıtıyoruz.
                    if (IsMediaPlaying)
                    {
                        TimeSpan timeSinceLastUpdate = DateTimeOffset.Now - timelineProperties.LastUpdatedTime;
                        currentPosition += timeSinceLastUpdate;
                    }

                    // Sürenin taşmasını veya negatif olmasını engelle (Clamp)
                    if (currentPosition > timelineProperties.EndTime) currentPosition = timelineProperties.EndTime;
                    if (currentPosition < TimeSpan.Zero) currentPosition = TimeSpan.Zero;

                    // Değişkenleri besle
                    TotalDurationSeconds = timelineProperties.EndTime.TotalSeconds;
                    CurrentPositionSeconds = currentPosition.TotalSeconds;

                    TotalDurationString = timelineProperties.EndTime.ToString(@"mm\:ss");
                    CurrentPositionString = currentPosition.ToString(@"mm\:ss");
                }
            }
            else
            {
                // Medya tamamen kapatılmışsa arayüzü sıfırla
                IsMediaPlaying = false;
                CurrentPositionSeconds = 0;
                CurrentPositionString = "00:00";
                TotalDurationString = "00:00";
            }
        }
    }
}