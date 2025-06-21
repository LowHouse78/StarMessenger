#region "copyright"

/*
    Copyright © 2024 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion

using NINA.StarMessenger.Providers;
using NINA.StarMessenger.Utils;
using System.ComponentModel;
using System.IO;
using NINA.StarMessenger.Communication.Ntfy.DTOs;
using System.Net.Http;
using NINA.Core.Utility;

namespace NINA.StarMessenger.Communication.Ntfy
{
    internal class NtfyClient
    {
        private string _ntfyUserName;
        private string _ntfyPassword;
        private string _ntfyToken;
        private string _ntfyServer;
        private string _ntfyTopic;

        public NtfyClient()
        {
            _ntfyUserName = Settings.Default.NtfyUser;
            _ntfyPassword = Settings.Default.NtfyPassword;
            _ntfyToken = Settings.Default.NtfyToken;
            _ntfyServer = Settings.Default.NtfyServer;
            _ntfyTopic = Settings.Default.NtfyTopic;

            Settings.Default.PropertyChanged += SettingsChanged;
        }
        internal void CancelCurrentProcessing()
        {
            DataProvider.SkipCurrentProcessing();
        }

        internal async Task<bool> SendNtfyMessageAsync(string priority,
                                                        TriggerSourceTypes triggerSource,
                                                        bool testMessage = false)
        {

            return await Task.Run(async () =>
            {

                var ntfyNotfier = new NtfyNotifier(_ntfyServer, _ntfyTopic);
                var imageFilePath = string.Empty;
                var timeout = TimeSpan.FromSeconds(45);

                using (var cts = new CancellationTokenSource(timeout))
                {
                    try
                    {
                        string message;

                        if (!testMessage)
                        {
                            message = await DataProvider.RetrieveImageInformationDataAsync<NtfyClient>(cts.Token, triggerSource);
                            imageFilePath = await DataProvider.RetrieveImageAsync();
                        }
                        else
                        {
                            message = "Test message from N.I.N.A";
                        }

                        var sendingParameter = new NtfySendingContent("StarMessage",
                                                                               message,
                                                                               imageFilePath,
                                                                               priority);

                        var ntfySendingCredentials = new NtfySendingCredentials(_ntfyUserName, _ntfyPassword, _ntfyToken);
                        var broadCastImage = !string.IsNullOrWhiteSpace(imageFilePath);

                        await ntfyNotfier.SendMessageWithImageAsync(sendingParameter, ntfySendingCredentials, broadCastImage);

                        if (!string.IsNullOrEmpty(imageFilePath))
                        {
                            File.Delete(imageFilePath);
                        }

                        return true;
                    }
                    catch (HttpRequestException e)
                    {
                        Logger.Error($"Request error while trying to send a Ntfy message.", e);
                    }
                    catch (TaskCanceledException e) when (e.InnerException is TimeoutException)
                    {
                        Logger.Error($"Request timed out while trying to send a Ntfy message.", e);
                    }
                    catch (TaskCanceledException e)
                    {
                        Logger.Error($"Request was canceled while trying to send a Ntfy message.", e);
                    }
                    catch (UriFormatException e)
                    {
                        Console.WriteLine($"Invalid URL while trying to send a Ntfy message: {e.Message}", e);
                    }
                    catch (InvalidOperationException e)
                    {
                        Logger.Error($"Invalid operation while trying to send a Ntfy message: {e.Message}", e);
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"Unexpected error while trying to send a Ntfy message: {e.Message}", e);
                    }

                    return false;
                }
            });
        }


        private void SettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Settings.Default.NtfyUser):
                    _ntfyUserName = !string.IsNullOrEmpty(Settings.Default.NtfyUser)
                        ? DataProtector.DecryptString(Settings.Default.NtfyUser)
                        : string.Empty;
                    break;

                case nameof(Settings.Default.NtfyPassword):
                    _ntfyPassword = !string.IsNullOrEmpty(Settings.Default.NtfyPassword)
                        ? DataProtector.DecryptString(Settings.Default.NtfyPassword)
                        : string.Empty;
                    break;

                case nameof(Settings.Default.NtfyToken):
                    _ntfyToken = !string.IsNullOrEmpty(Settings.Default.NtfyToken)
                        ? DataProtector.DecryptString(Settings.Default.NtfyToken)
                        : string.Empty;
                    break;


                case nameof(Settings.Default.NtfyServer):
                    _ntfyServer = !string.IsNullOrEmpty(Settings.Default.NtfyServer)
                        ? Settings.Default.NtfyServer
                        : string.Empty;
                    break;

                case nameof(Settings.Default.NtfyTopic):
                    _ntfyTopic = Settings.Default.NtfyTopic;
                    break;
            }
        }
    }
}
