#region "copyright"

/*
    Copyright © 2024 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.ComponentModel.Composition;
using System.ComponentModel;
using NINA.Core.Utility;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;
using System.Runtime.CompilerServices;
using NINA.Image.Interfaces;
using NINA.StarMessenger.Providers;
using NINA.StarMessenger.Utils;
using NINA.Profile.Interfaces;
using System.Windows.Input;
using NINA.StarMessenger.Communication.Email;
using NINA.StarMessenger.Communication.Ntfy;
using NINA.StarMessenger.Communication.Pushover;
using PriorityTypes = NINA.StarMessenger.Communication.Ntfy.PriorityTypes;

namespace NINA.StarMessenger
{
    [Export(typeof(IPluginManifest))]

    public class StarMessenger : PluginBase, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        [ImportingConstructor]
        public StarMessenger(IImageHistoryVM imageHistory,
                             IImageDataFactory imageDataFactory,
                             IProfileService profileService)
        {
            try
            {
                SendPushoverTestMessageCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() =>
                {
                    const int timeoutSendStarMessagePushoverInMilliSeconds = 30000;
                    using var cancellationTokenSource = new CancellationTokenSource();
                    cancellationTokenSource.CancelAfter(timeoutSendStarMessagePushoverInMilliSeconds);

                    var pushoverClient = new PushoverClient();
                    _ = pushoverClient.SendMessageAsync(SoundTypes.Pushover.ToString().ToLower(), "0", TriggerSourceTypes.Default, cancellationTokenSource.Token, true);

                });

                SendTestEMailCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() =>
                {
                    var eMailClient = new EMailClient();
                    _ = eMailClient.SendEMailAsync(TriggerSourceTypes.Default, true);

                });

                SendNtfyTestMessageCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() =>
                {
                    var ntfyClient = new NtfyClient();
                    _ = ntfyClient.SendNtfyMessageAsync(NtfyPrioritization.GetValueByEnum(PriorityTypes.Normal), TriggerSourceTypes.Default, true);

                });

                DataProvider.Configure(imageHistory, imageDataFactory, profileService);
            }
            catch (TaskCanceledException)
            {
                Logger.Error($"Error during instantiation of StarMessenger plugin");
            }
            catch (Exception e)
            {
                Logger.Error($"Error during instantiation of StarMessenger plugin", e, nameof(StarMessenger));
                throw;
            }
        }


        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public static bool HFR
        {
            get => Settings.Default.HFR;
            set
            {
                Settings.Default.HFR = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public static bool Stars
        {
            get => Settings.Default.Stars;
            set
            {
                Settings.Default.Stars = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public static bool RMSTotal
        {
            get => Settings.Default.RMSTotal;
            set
            {
                Settings.Default.RMSTotal = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public static bool RMSRa
        {
            get => Settings.Default.RMSRa;
            set
            {
                Settings.Default.RMSRa = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public static bool RMSDec
        {
            get => Settings.Default.RMSDec;
            set
            {
                Settings.Default.RMSDec = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public static bool FocuserPosition
        {
            get => Settings.Default.FocuserPosition;
            set
            {
                Settings.Default.FocuserPosition = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public static bool Filter
        {
            get => Settings.Default.Filter;
            set
            {
                Settings.Default.Filter = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public static bool Temperature
        {
            get => Settings.Default.Temperature;
            set
            {
                Settings.Default.Temperature = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public static bool Target
        {
            get => Settings.Default.Target;
            set
            {
                Settings.Default.Target = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public static bool RotatorPosition
        {
            get => Settings.Default.RotatorPosition;
            set
            {
                Settings.Default.RotatorPosition = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public static bool RotatorMechanicalPosition
        {
            get => Settings.Default.RotatorMechanicalPosition;
            set
            {
                Settings.Default.RotatorMechanicalPosition = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }
        public static bool Time
        {
            get => Settings.Default.Time;
            set
            {
                Settings.Default.Time = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public static bool Duration
        {
            get => Settings.Default.Duration;
            set
            {
                Settings.Default.Duration = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public static bool MAD
        {
            get => Settings.Default.MAD;
            set
            {
                Settings.Default.MAD = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }
        public static bool StDev
        {
            get => Settings.Default.StDev;
            set
            {
                Settings.Default.StDev = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public static bool Mean
        {
            get => Settings.Default.Mean;
            set
            {
                Settings.Default.Mean = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public static bool Median
        {
            get => Settings.Default.Median;
            set
            {
                Settings.Default.Median = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public static bool Min
        {
            get => Settings.Default.Min;
            set
            {
                Settings.Default.Min = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public static bool Max
        {
            get => Settings.Default.Max;
            set
            {
                Settings.Default.Max = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public static bool Image
        {
            get => Settings.Default.Image;
            set
            {
                Settings.Default.Image = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        #region Pushover Options
        public static string PushoverAPIKey
        {
            get => !string.IsNullOrEmpty(Settings.Default.PushoverApiKey) ? DataProtector.DecryptString(Settings.Default.PushoverApiKey) : string.Empty;

            set
            {
                Settings.Default.PushoverApiKey = string.IsNullOrEmpty(value) ? string.Empty : DataProtector.EncryptString(value);
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public static string PushoverUserKey
        {
            get => !string.IsNullOrEmpty(Settings.Default.PushoverUserKey) ? DataProtector.DecryptString(Settings.Default.PushoverUserKey) : string.Empty;

            set
            {
                Settings.Default.PushoverUserKey = string.IsNullOrEmpty(value) ? string.Empty : DataProtector.EncryptString(value);
                CoreUtil.SaveSettings(Settings.Default);
            }
        }
        #endregion

        #region Email Options
        public static string EMailSenderAddress
        {
            get => Settings.Default.EMailSenderAddress;
            set
            {
                Settings.Default.EMailSenderAddress = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public static string EMailRecipientAddress
        {
            get => Settings.Default.EMailRecipientAddress;
            set
            {
                Settings.Default.EMailRecipientAddress = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public static string EMailSMTPHost
        {
            get => Settings.Default.EMailSMTPHost;
            set
            {
                Settings.Default.EMailSMTPHost = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public static string EMailSMTPPort
        {
            get => Settings.Default.EMailSMTPPort;
            set
            {
                Settings.Default.EMailSMTPPort = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public static string EMailSMTPUsername
        {
            get => !string.IsNullOrEmpty(Settings.Default.EMailSMTPUsername) ? DataProtector.DecryptString(Settings.Default.EMailSMTPUsername) : string.Empty;
            set
            {
                Settings.Default.EMailSMTPUsername = string.IsNullOrEmpty(value) ? string.Empty : DataProtector.EncryptString(value);
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public string EMailPassword
        {
            get => !string.IsNullOrEmpty(Settings.Default.EMailPassword) ? DataProtector.DecryptString(Settings.Default.EMailPassword) : string.Empty;

            set
            {
                Settings.Default.EMailPassword = string.IsNullOrEmpty(value) ? string.Empty : DataProtector.EncryptString(value);
                CoreUtil.SaveSettings(Settings.Default);
            }
        }
        #endregion

        #region Ntfy Options

        public static string NtfyTopic
        {
            get => !string.IsNullOrEmpty(Settings.Default.NtfyTopic) ? Settings.Default.NtfyTopic : string.Empty;

            set
            {
                Settings.Default.NtfyTopic = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public static string NtfyUser
        {
            get => !string.IsNullOrEmpty(Settings.Default.NtfyUser) ? DataProtector.DecryptString(Settings.Default.NtfyUser) : string.Empty;

            set
            {
                Settings.Default.NtfyUser = string.IsNullOrEmpty(value) ? string.Empty : DataProtector.EncryptString(value);
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public string NtfyPassword
        {
            get => !string.IsNullOrEmpty(Settings.Default.NtfyPassword) ? DataProtector.DecryptString(Settings.Default.NtfyPassword) : string.Empty;

            set
            {
                Settings.Default.NtfyPassword = string.IsNullOrEmpty(value) ? string.Empty : DataProtector.EncryptString(value);
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public static string NtfyToken
        {
            get => !string.IsNullOrEmpty(Settings.Default.NtfyToken) ? DataProtector.DecryptString(Settings.Default.NtfyToken) : string.Empty;

            set
            {
                Settings.Default.NtfyToken = string.IsNullOrEmpty(value) ? string.Empty : DataProtector.EncryptString(value);
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public static string NtfyServer
        {
            get => !string.IsNullOrEmpty(Settings.Default.NtfyServer) ? Settings.Default.NtfyServer : string.Empty;

            set
            {
                Settings.Default.NtfyServer = string.IsNullOrEmpty(value) ? string.Empty : value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        #endregion
        public ICommand SendPushoverTestMessageCommand { get; set; }
        public ICommand SendTestEMailCommand { get; set; }

        public ICommand SendNtfyTestMessageCommand { get; set; }

    }
}

