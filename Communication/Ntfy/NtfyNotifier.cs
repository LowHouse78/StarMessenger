#region "copyright"

/*
    Copyright © 2024 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion

using NINA.StarMessenger.Communication.Ntfy.DTOs;
using NINA.StarMessenger.Utils;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using NINA.Core.Utility;


namespace NINA.StarMessenger.Communication.Ntfy
{
    public class NtfyNotifier
    {
        private readonly HttpClient _httpClient;
        private readonly string _ntfyTopic;
        private readonly string _ntfyServer;

        public NtfyNotifier(string ntfyServer, string ntfyTopic)
        {
            _httpClient = new HttpClient();
            _ntfyServer = ntfyServer.TrimEnd('/');
            _ntfyTopic = ntfyTopic;
        }

        public async Task SendMessageWithImageAsync(NtfySendingContent ntfySendingContent,
                                                    NtfySendingCredentials ntfySendingCredentials,
                                                    bool broadcastImage)
        {

            if (!string.IsNullOrWhiteSpace(ntfySendingCredentials.Token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", DataProtector.DecryptString(ntfySendingCredentials.Token));
            }
            else
            if (!string.IsNullOrWhiteSpace(ntfySendingCredentials.Username) && !string.IsNullOrWhiteSpace(ntfySendingCredentials.Password))
            {
                var byteArray = Encoding.ASCII.GetBytes($@"{DataProtector.DecryptString(ntfySendingCredentials.Username)}:{DataProtector.DecryptString(ntfySendingCredentials.Password)}");

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            }

            var requestUri = $"{_ntfyServer}/{_ntfyTopic}";

            var textContent = new StringContent(ntfySendingContent.Message);

            textContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            textContent.Headers.Add("X-Priority", ntfySendingContent.Priority);

            var responseBroadcastTextMessage = await _httpClient.PostAsync(requestUri, textContent);

            if (!responseBroadcastTextMessage.IsSuccessStatusCode)
            {
                Logger.Error($"Failed to send NTFY message: {responseBroadcastTextMessage.ReasonPhrase}");
            }

            if (broadcastImage && File.Exists(ntfySendingContent.ImagePath))
            {
                var imageBytes = File.ReadAllBytes(ntfySendingContent.ImagePath);
                var imageContent = new ByteArrayContent(imageBytes);
                var responseBroadcastImage = await _httpClient.PostAsync(requestUri, imageContent);
                if (!responseBroadcastImage.IsSuccessStatusCode)
                {
                    Logger.Error($"Failed to send NTFY image message: {responseBroadcastImage.ReasonPhrase}");
                }

            }
            else
            {
                if (broadcastImage)
                {
                    Logger.Error($"Image file not found when to sending a NTFY message.");
                }
            }
        }
    }
}
