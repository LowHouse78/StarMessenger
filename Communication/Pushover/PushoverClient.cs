#region "copyright"

/*
    Copyright © 2024 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.StarMessenger.Providers;
using NINA.StarMessenger.Utils;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.ComponentModel;

namespace NINA.StarMessenger.Communication.Pushover
{

    internal class PushoverClient

    {
        private readonly string _apiUrl = "https://api.pushover.net/1/messages.json";
        private string _token;
        private string _user;

        internal PushoverClient()
        {

            _token = !string.IsNullOrEmpty(Settings.Default.PushoverApiKey)
                ? DataProtector.DecryptString(Settings.Default.PushoverApiKey)
                : string.Empty;

            _user = !string.IsNullOrEmpty(Settings.Default.PushoverUserKey)
                ? DataProtector.DecryptString(Settings.Default.PushoverUserKey)
                : string.Empty;

            Settings.Default.PropertyChanged += SettingsChanged!;
        }

        internal void CancelCurrentProcessing()
        {
            DataProvider.SkipCurrentProcessing();
        }

        internal async Task<bool> SendMessageAsync(string sound,
                                                   string priority,
                                                   TriggerSourceTypes triggerSource,
                                                   CancellationToken token,
                                                   bool testMessage = false)
        {
            return await Task.Run(async () =>
            {
                string messageContent;
                var imageFilePath = string.Empty;
                if (!testMessage)
                {
                    messageContent = await DataProvider.RetrieveImageInformationDataAsync<PushoverClient>(token, triggerSource);
                    imageFilePath = await DataProvider.RetrieveImageAsync();
                }
                else
                {
                    messageContent = "Test message from N.I.N.A";
                }

                if (string.IsNullOrEmpty(messageContent))
                {
                    return false;
                }

                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        using (var formData = new MultipartFormDataContent())
                        {
                            formData.Add(new StringContent(_token), "token");
                            formData.Add(new StringContent(_user), "user");
                            formData.Add(new StringContent(messageContent), "message");
                            formData.Add(new StringContent(sound.ToLower()), "sound");
                            formData.Add(new StringContent(priority), "priority");

                            if (!string.IsNullOrEmpty(imageFilePath) && File.Exists(imageFilePath))
                            {
                                byte[] imageBytes = await File.ReadAllBytesAsync(imageFilePath);
                                formData.Add(new ByteArrayContent(imageBytes, 0, imageBytes.Length), "attachment",
                                    Path.GetFileName(imageFilePath));
                            }

                            if (!string.IsNullOrEmpty(imageFilePath))
                            {
                                File.Delete(imageFilePath);
                            }

                            HttpResponseMessage response = await client.PostAsync(_apiUrl, formData);
                            response.EnsureSuccessStatusCode();

                            var responseContent = await response.Content.ReadAsStringAsync();
                            var result = JsonSerializer.Deserialize<PushoverResponse>(responseContent);

                            return result?.Status == 1;
                        }
                    }
                    catch (HttpRequestException e)
                    {
                        Logger.Error($"Request error while trying to send a Pushover message.", e);
                    }
                    catch (TaskCanceledException e) when (e.InnerException is TimeoutException)
                    {
                        Logger.Error($"Request timed out while trying to send a Pushover message.", e);
                    }
                    catch (TaskCanceledException e)
                    {
                        Logger.Error($"Request was canceled while trying to send a Pushover message.", e);
                    }
                    catch (UriFormatException e)
                    {
                        Console.WriteLine($"Invalid URL while trying to send a Pushover message: {e.Message}", e);
                    }
                    catch (InvalidOperationException e)
                    {
                        Logger.Error($"Invalid operation while trying to send a Pushover message: {e.Message}", e);
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"Unexpected error while trying to send a Pushover message: {e.Message}", e);
                    }
                    return false;
                }
            }, token);
        }

        private void SettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Settings.Default.PushoverApiKey):
                    _token = !string.IsNullOrEmpty(Settings.Default.PushoverApiKey)
                        ? DataProtector.DecryptString(Settings.Default.PushoverApiKey)
                        : string.Empty;
                    break;

                case nameof(Settings.Default.PushoverUserKey):
                    _user = !string.IsNullOrEmpty(Settings.Default.PushoverUserKey)
                            ? DataProtector.DecryptString(Settings.Default.PushoverUserKey)
                            : string.Empty;
                    break;
            }
        }
    }

}