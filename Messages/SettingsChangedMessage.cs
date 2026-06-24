using CommunityToolkit.Mvvm.Messaging.Messages;
using SystemMediaFlyouts.Models;

namespace SystemMediaFlyouts.Messages
{
    /// <summary>
    /// Ayarlar güncellendiğinde tüm sisteme (Event Bus üzerinden) fırlatılacak mesaj.
    /// Alıcılar bu mesajı dinleyerek UI'ı anında (Instant Apply) güncelleyecektir.
    /// </summary>
    public class SettingsChangedMessage : ValueChangedMessage<AppSettings>
    {
        public SettingsChangedMessage(AppSettings value) : base(value)
        {
        }
    }
}