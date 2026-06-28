using System;

namespace SystemMediaFlyouts.Models
{
    public enum AppTheme
    {
        System,
        Light,
        Dark
    }

    public enum FlyoutPosition
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        TopCenter,
        BottomCenter
    }

    public class AppSettings
    {
        // Tema ve Konum Ayarları
        public AppTheme Theme { get; set; } = AppTheme.System;
        public FlyoutPosition Position { get; set; } = FlyoutPosition.TopCenter;

        // Modül Görünürlük Ayarları (Lego yapısı için)
        public bool IsControlBarEnabled { get; set; } = true; 
        public bool IsMediaModuleEnabled { get; set; } = true;
        public bool IsVolumeModuleEnabled { get; set; } = true;
        public bool IsBrightnessModuleEnabled { get; set; } = true;

        // Panelin ekranda kalacağı süre (saniye cinsinden)
        public int DisplayDuration { get; set; } = 3;
    }
}