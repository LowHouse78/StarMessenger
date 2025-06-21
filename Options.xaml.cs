using NINA.Core.Utility;
using NINA.StarMessenger.Utils;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace NINA.StarMessenger
{

    [Export(typeof(ResourceDictionary))]
    partial class Options : ResourceDictionary
    {
        public Options() 
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Logger.Info(e.Uri.AbsoluteUri);
            _ = Process.Start(new ProcessStartInfo(e.Uri.OriginalString) { UseShellExecute = true });
            e.Handled = true;
        }

        private void PasswordBoxEMailPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is not PasswordBox elem)
            {
                return;
            }
            if (elem.DataContext is StarMessenger vm)
            {
                vm.EMailPassword = DataProtector.ConvertSecureString(elem.SecurePassword);
            }
        }

        private void PasswordBoxEMailPasswordLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not PasswordBox { DataContext: StarMessenger vm } elem)
            {
                return;
            }
            elem.Password = vm.EMailPassword;
        }

        private void PasswordBoxNtfyPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is not PasswordBox elem)
            {
                return;
            }
            if (elem.DataContext is StarMessenger vm)
            {
                vm.NtfyPassword = DataProtector.ConvertSecureString(elem.SecurePassword);
            }
        }

        private void PasswordBoxNtfyPasswordLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not PasswordBox { DataContext: StarMessenger vm } elem)
            {
                return;
            }
            elem.Password = vm.NtfyPassword;
        }
    }
}