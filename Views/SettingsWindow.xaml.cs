using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SystemMediaFlyouts.Messages;
using Wpf.Ui.Controls;

namespace SystemMediaFlyouts.Views
{
    /// <summary>
    /// SettingsWindow.xaml etkileşim mantığı
    /// </summary>
    public partial class SettingsWindow : FluentWindow
    {
        public SettingsWindow()
        {
            InitializeComponent();

            // Pencerenin görünürlüğü her değiştiğinde tetiklenir
            this.IsVisibleChanged += (s, e) =>
            {
                if (this.IsVisible) // Eğer pencere görünür hale geldiyse
                {
                    // Medya panelini uyandırmak için sisteme mesaj yolla
                    WeakReferenceMessenger.Default.Send(new SettingsOpenedMessage());
                }
            };
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true; // Kapanmayı iptal et
            this.Hide();     // Pencereyi arka plana gizle
        }
    }
}
