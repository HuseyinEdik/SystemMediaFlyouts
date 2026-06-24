using System;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.Messaging;
using SystemMediaFlyouts.Models;
using SystemMediaFlyouts.Messages;

namespace SystemMediaFlyouts.Services
{
    public class SettingsService
    {
        private readonly string _settingsFilePath;
        private AppSettings _currentSettings;

        public SettingsService()
        {
            // İşletim sistemi sınırları: Yönetici izni gerektirmemesi için AppData/Local kullanılıyor.
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appDataPath, "SystemMediaFlyouts");

            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            _settingsFilePath = Path.Combine(appFolder, "appsettings.json");
            _currentSettings = LoadSettings();
        }

        public AppSettings CurrentSettings => _currentSettings;

        /// <summary>
        /// Başlangıçta ayarları diskten okur. Dosya yoksa veya bozuksa varsayılan değerleri döndürür.
        /// </summary>
        public AppSettings LoadSettings()
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new AppSettings();
            }

            try
            {
                string json = File.ReadAllText(_settingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                // Dosya kilitli veya JSON formatı bozuksa varsayılan ayarlara dön
                return new AppSettings();
            }
        }

        /// <summary>
        /// Ayarları diske yazar ve "Anında Uygulama" prensibi gereği Event Bus üzerinden sisteme duyurur.
        /// </summary>
        public void SaveSettings(AppSettings settings)
        {
            _currentSettings = settings;

            try
            {
                // Okunabilir (Indented) JSON formatında kaydet
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_currentSettings, options);

                File.WriteAllText(_settingsFilePath, json);

                // Mimari Şema: Değişiklik sisteme yayınlanıyor (Tüm ViewModel'lar bu yayınla tetiklenecek)
                WeakReferenceMessenger.Default.Send(new SettingsChangedMessage(_currentSettings));
            }
            catch (Exception ex)
            {
                // Burada ileride bir ILogger mekanizması eklenebilir.
                System.Diagnostics.Debug.WriteLine($"Ayarlar kaydedilirken hata oluştu: {ex.Message}");
            }
        }
    }
}