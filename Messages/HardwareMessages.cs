using CommunityToolkit.Mvvm.Messaging.Messages;

namespace SystemMediaFlyouts.Messages
{
    // Medya bilgileri için basit bir Record (Veri taşıyıcı)
    // Sonuna ProgressPercentage eklendi
    public record MediaInfo(string Title, string Artist, bool IsPlaying, byte[]? ThumbnailData, double ProgressPercentage);

    public class MediaChangedMessage : ValueChangedMessage<MediaInfo>
    {
        public MediaChangedMessage(MediaInfo value) : base(value) { }
    }

    public class VolumeChangedMessage : ValueChangedMessage<int>
    {
        public VolumeChangedMessage(int value) : base(value) { }
    }

    public class BrightnessChangedMessage : ValueChangedMessage<int>
    {
        public BrightnessChangedMessage(int value) : base(value) { }
    }
    public class MediaProgressChangedMessage : ValueChangedMessage<double>
    {
        public MediaProgressChangedMessage(double value) : base(value) { }
    }
}