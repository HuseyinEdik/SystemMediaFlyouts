using System;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Messaging;
using SystemMediaFlyouts.Messages;
using SystemMediaFlyouts.Models;
using SystemMediaFlyouts.Services;
using Wpf.Ui.Appearance;

namespace SystemMediaFlyouts.Core
{
    public class WindowManager :
        IRecipient<SettingsChangedMessage>,
        IRecipient<VolumeChangedMessage>,
        IRecipient<BrightnessChangedMessage>,
        IRecipient<MediaChangedMessage>,
        IRecipient<SettingsOpenedMessage> // YENİ EKLENDİ
    {
        private Window? _flyoutWindow;
        private readonly DispatcherTimer _hideTimer;
        private int _displayDuration;

        // Kurucu metot: Başlangıçta ayarları alıp zamanlayıcıyı hazırlarız
        public WindowManager(SettingsService settingsService)
        {
            _displayDuration = settingsService.CurrentSettings.DisplayDuration;

            _hideTimer = new DispatcherTimer();
            _hideTimer.Tick += OnHideTimerTick;
        }

        public void RegisterFlyoutWindow(Window window)
        {
            _flyoutWindow = window;

            // Tüm mesajları dinlemesi için RegisterAll kullanıyoruz
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        // --- GİZLEME MANTIĞI ---
        private void OnHideTimerTick(object? sender, EventArgs e)
        {
            if (_flyoutWindow == null) return;

            // Kullanıcı mouse ile panelin üzerindeyse gizlemeyi iptal et
            if (_flyoutWindow.IsMouseOver) return;

            // 2. YENİ: Ayarlar penceresi ekranda görünür durumdaysa gizlemeyi iptal et
            foreach (Window window in Application.Current.Windows)
            {
                if (window.GetType().Name == "SettingsWindow" && window.IsVisible)
                {
                    return; // Ayarlar açıksa timer çalışmaya devam eder ama paneli gizlemez
                }
            }

            _flyoutWindow.Hide();
            _hideTimer.Stop();
        }

        // --- GÖSTERME MANTIĞI ---
        private void ShowPanel()
        {
            if (_flyoutWindow == null) return;

            // UI Thread'ine geçiş yapıyoruz ki çökmesin
            Application.Current.Dispatcher.Invoke(() =>
            {
                _flyoutWindow.Show();
                _flyoutWindow.Topmost = true;

                // Süreyi sıfırlayıp baştan başlat
                _hideTimer.Stop();
                _hideTimer.Interval = TimeSpan.FromSeconds(_displayDuration);
                _hideTimer.Start();
            });
        }

        // --- OLAY MESAJLARINI DİNLEME ---
        public void Receive(VolumeChangedMessage message) => ShowPanel();
        public void Receive(BrightnessChangedMessage message) => ShowPanel();
        public void Receive(MediaChangedMessage message) => ShowPanel();

        // YENİ EKLENDİ: Ayarlar açıldığı an paneli göster!
        public void Receive(SettingsOpenedMessage message) => ShowPanel();

        // --- AYAR DEĞİŞİM MESAJI ---
        public void Receive(SettingsChangedMessage message)
        {
            if (_flyoutWindow == null) return;

            // Süre ayarı değişmiş olabilir, onu güncelliyoruz
            _displayDuration = message.Value.DisplayDuration;

            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateWindowPosition(message.Value.Position);
                UpdateTheme(message.Value.Theme);
            });

            ShowPanel();
        }

        // --- TEMA VE KONUM METOTLARI (Senin Kodların) ---
        public void UpdateTheme(AppTheme theme)
        {
            if (theme == AppTheme.System)
            {
                ApplicationThemeManager.ApplySystemTheme();
            }
            else
            {
                ApplicationThemeManager.Apply(
                    theme == AppTheme.Dark ?
                    ApplicationTheme.Dark :
                    ApplicationTheme.Light);
            }
        }

        public void UpdateWindowPosition(FlyoutPosition position)
        {
            if (_flyoutWindow == null) return;

            var workArea = SystemParameters.WorkArea;
            double windowWidth = _flyoutWindow.Width;
            double windowHeight = _flyoutWindow.Height;
            double padding = 24;

            switch (position)
            {
                case FlyoutPosition.TopLeft:
                    _flyoutWindow.Left = workArea.Left + padding;
                    _flyoutWindow.Top = workArea.Top + padding;
                    break;
                case FlyoutPosition.TopRight:
                    _flyoutWindow.Left = workArea.Right - windowWidth - padding;
                    _flyoutWindow.Top = workArea.Top + padding;
                    break;
                case FlyoutPosition.BottomLeft:
                    _flyoutWindow.Left = workArea.Left + padding;
                    _flyoutWindow.Top = workArea.Bottom - windowHeight - padding;
                    break;
                case FlyoutPosition.BottomRight:
                    _flyoutWindow.Left = workArea.Right - windowWidth - padding;
                    _flyoutWindow.Top = workArea.Bottom - windowHeight - padding;
                    break;
                case FlyoutPosition.TopCenter:
                    _flyoutWindow.Left = workArea.Left + (workArea.Width - windowWidth) / 2;
                    _flyoutWindow.Top = workArea.Top + padding;
                    break;
                case FlyoutPosition.BottomCenter:
                    _flyoutWindow.Left = workArea.Left + (workArea.Width - windowWidth) / 2;
                    _flyoutWindow.Top = workArea.Bottom - windowHeight - padding;
                    break;
            }
        }
    }
}