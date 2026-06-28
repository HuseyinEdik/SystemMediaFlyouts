using System;
using System.Management;
using System.Threading.Tasks;
using Windows.Media.Control;
using NAudio.CoreAudioApi;
using CommunityToolkit.Mvvm.Messaging;
using SystemMediaFlyouts.Messages;
using System.IO;
using Windows.Storage.Streams;

namespace SystemMediaFlyouts.Services
{
    public class HardwareService
    {
        private MMDevice? _audioDevice;
        private GlobalSystemMediaTransportControlsSessionManager? _mediaManager;
        private System.Timers.Timer? _progressTimer; // Yeni eklendi


        public async Task InitializeAsync()
        {
            InitAudio();
            InitBrightness();
            await InitMediaAsync();
        }

        #region 1. SES YÖNETİMİ (NAudio)
        private void InitAudio()
        {
            try
            {
                var enumerator = new MMDeviceEnumerator();
                _audioDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

                // BAŞLANGIÇ DEĞERİNİ OKU VE GÖNDER
                int initialVolume = (int)(_audioDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
                WeakReferenceMessenger.Default.Send(new VolumeChangedMessage(initialVolume));

                _audioDevice.AudioEndpointVolume.OnVolumeNotification += (data) =>
                {
                    int volume = (int)(data.MasterVolume * 100);
                    WeakReferenceMessenger.Default.Send(new VolumeChangedMessage(volume));
                };
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Audio Init Hatası: {ex.Message}"); }
        }
        #endregion

        // UI'daki Slider değiştiğinde Windows sesini günceller
        public void SetVolume(int level)
        {
            if (_audioDevice != null)
            {
                _audioDevice.AudioEndpointVolume.MasterVolumeLevelScalar = level / 100f;
            }
        }

        #region 2. PARLAKLIK YÖNETİMİ (WMI)
        private void InitBrightness()
        {
            try
            {
                // BAŞLANGIÇ DEĞERİNİ OKU VE GÖNDER
                using (var searcher = new ManagementObjectSearcher("root\\wmi", "SELECT * FROM WmiMonitorBrightness"))
                {
                    foreach (ManagementObject instance in searcher.Get())
                    {
                        int initialBrightness = Convert.ToInt32(instance["CurrentBrightness"]);
                        WeakReferenceMessenger.Default.Send(new BrightnessChangedMessage(initialBrightness));
                        break; // İlk monitörü almamız yeterli
                    }
                }

                var query = new WqlEventQuery("SELECT * FROM WmiMonitorBrightnessEvent");
                var watcher = new ManagementEventWatcher(new ManagementScope("root\\wmi"), query);

                watcher.EventArrived += (s, e) =>
                {
                    int brightness = Convert.ToInt32(e.NewEvent.Properties["Brightness"].Value);
                    WeakReferenceMessenger.Default.Send(new BrightnessChangedMessage(brightness));
                };
                watcher.Start();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Brightness Init Hatası: {ex.Message}"); }
        }
        #endregion

        // UI'daki Slider değiştiğinde Windows parlaklığını günceller
        public void SetBrightness(int level)
        {
            try
            {
                using var mclass = new System.Management.ManagementClass("root\\wmi", "WmiMonitorBrightnessMethods", null);
                foreach (System.Management.ManagementObject instance in mclass.GetInstances())
                {
                    instance.InvokeMethod("WmiSetBrightness", new object[] { 1, level });
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Brightness Set Hatası: {ex.Message}"); }
        }

        #region 3. MEDYA YÖNETİMİ (GSMTC)
        private GlobalSystemMediaTransportControlsSession? _currentSession; // Aktif seansı takip etmek için eklendi

        private async Task InitMediaAsync()
        {
            try
            {
                // ZAMANLAYICIYI KUR (Saniyede bir çalışır)
                _progressTimer = new System.Timers.Timer(200);
                _progressTimer.Elapsed += (s, e) => SendProgressUpdate();

                _mediaManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                _mediaManager.CurrentSessionChanged += OnMediaSessionChanged;

                // EN ÖNEMLİ DÜZELTME: Uygulama açıldığındaki ilk seansı sisteme "bağla"
                SwitchMediaSession(_mediaManager.GetCurrentSession());
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Media Init Hatası: {ex.Message}"); }
        }

        // ZAMAN ÇİZGİSİNİ HER SANİYE OKUYUP UI'A GÖNDEREN METOT
        private void SendProgressUpdate()
        {
            try
            {
                var session = _mediaManager?.GetCurrentSession();
                if (session == null) return;

                var timeline = session.GetTimelineProperties();
                var playbackInfo = session.GetPlaybackInfo();
                bool isPlaying = playbackInfo?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

                if (timeline != null && timeline.EndTime.TotalSeconds > 0)
                {
                    TimeSpan currentPosition = timeline.Position;

                    if (isPlaying)
                    {
                        TimeSpan timeSinceLastUpdate = DateTimeOffset.Now - timeline.LastUpdatedTime;
                        currentPosition += timeSinceLastUpdate;
                    }

                    if (currentPosition > timeline.EndTime) currentPosition = timeline.EndTime;
                    if (currentPosition < TimeSpan.Zero) currentPosition = TimeSpan.Zero;

                    double progressPercentage = (currentPosition.TotalSeconds / timeline.EndTime.TotalSeconds) * 100;
                    WeakReferenceMessenger.Default.Send(new MediaProgressChangedMessage(progressPercentage));
                }
            }
            catch { /* Okuma hatasını yoksay */ }
        }

        // Uygulama değişirse (Örn: Spotify'dan Chrome'a) çalışır
        private void OnMediaSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        {
            SwitchMediaSession(sender.GetCurrentSession());
        }

        // YENİ METOT: Şarkı değişikliklerini doğru dinlemek ve hafıza kaçağını önlemek için
        private void SwitchMediaSession(GlobalSystemMediaTransportControlsSession? newSession)
        {
            // Eski seans varsa bağlantılarını kopar ki eski şarkılar takılı kalmasın
            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
            }

            _currentSession = newSession;

            // Yeni seansa bağlan (Şarkı değiştiğinde haber verecek kısım)
            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
            }

            // İlk verileri ekrana gönder
            UpdateMediaState(_currentSession);
        }

        // Şarkı, sanatçı veya resim değiştiğinde otomatik tetiklenir
        private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            UpdateMediaState(sender);
        }

        // Duraklat/Oynat yapıldığında otomatik tetiklenir
        private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            UpdateMediaState(sender);
        }

        private async void UpdateMediaState(GlobalSystemMediaTransportControlsSession? session)
        {
            if (session == null) return;

            try
            {
                var properties = await session.TryGetMediaPropertiesAsync();
                var playbackInfo = session.GetPlaybackInfo();

                // NULL KONTROLÜ (ÇÖKMEYİ ENGELLEYEN KISIM)
                bool isPlaying = playbackInfo != null && playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

                // Müzik çalıyorsa zamanlayıcıyı başlat, durduysa durdur
                if (isPlaying) _progressTimer?.Start();
                else _progressTimer?.Stop();

                byte[]? thumbnailData = null;
                if (properties.Thumbnail != null)
                {
                    using var stream = await properties.Thumbnail.OpenReadAsync();
                    using var reader = new DataReader(stream);
                    await reader.LoadAsync((uint)stream.Size);
                    thumbnailData = new byte[stream.Size];
                    reader.ReadBytes(thumbnailData);
                }

                // Zaman çizgisini bu mesajda 0 gönderiyoruz çünkü asıl işi Timer yapacak
                var mediaInfo = new MediaInfo(
                    properties.Title ?? "Bilinmiyor",
                    properties.Artist ?? "Bilinmeyen Sanatçı",
                    isPlaying,
                    thumbnailData,
                    0
                );

                WeakReferenceMessenger.Default.Send(new MediaChangedMessage(mediaInfo));
            }
            catch { }
        }

        // UI'daki Oynat/Duraklat, İleri, Geri butonları için
        public async Task TogglePlayPauseAsync()
        {
            var session = _mediaManager?.GetCurrentSession();
            if (session != null) await session.TryTogglePlayPauseAsync();
        }

        public async Task SkipNextAsync()
        {
            var session = _mediaManager?.GetCurrentSession();
            if (session != null) await session.TrySkipNextAsync();
        }

        public async Task SkipPreviousAsync()
        {
            var session = _mediaManager?.GetCurrentSession();
            if (session != null) await session.TrySkipPreviousAsync();
        }

        // MainViewModel'in aradığı açık bağlantı
        public GlobalSystemMediaTransportControlsSession? GetCurrentSession()
        {
            return _mediaManager?.GetCurrentSession();
        }
        #endregion
    }
}