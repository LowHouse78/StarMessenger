#region "copyright"

/*
    Copyright © 2024 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion

using NINA.StarMessenger.Communication.Ntfy.DTOs;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using NINA.Core.Utility;


namespace NINA.StarMessenger.Communication.Ntfy
{
    public class NtfyNotifier
    {
        private static readonly HttpClient HttpClient = new();
        private readonly string _ntfyTopic;
        private readonly string _ntfyServer;

        public NtfyNotifier(string ntfyServer, string ntfyTopic)
        {
            _ntfyServer = ntfyServer.TrimEnd('/');
            _ntfyTopic = ntfyTopic;
        }

        /// <param name="ntfySendingCredentials">Plain (already decrypted) credentials.</param>
        /// <returns>true if the text message was accepted.</returns>
        public async Task<bool> SendMessageWithImageAsync(NtfySendingContent ntfySendingContent,
                                                          NtfySendingCredentials ntfySendingCredentials,
                                                          bool broadcastImage,
                                                          CancellationToken token = default)
        {
            var requestUri = $"{_ntfyServer}/{_ntfyTopic}";
            var authorization = CreateAuthorization(ntfySendingCredentials);

            using var textRequest = new HttpRequestMessage(HttpMethod.Post, requestUri);
            textRequest.Headers.Authorization = authorization;
            textRequest.Headers.Add("X-Priority", ntfySendingContent.Priority);
            textRequest.Content = new StringContent(ntfySendingContent.Message, Encoding.UTF8, "text/plain");

            using var responseBroadcastTextMessage = await HttpClient.SendAsync(textRequest, token);
            if (!responseBroadcastTextMessage.IsSuccessStatusCode)
            {
                Logger.Error($"Failed to send NTFY message: {responseBroadcastTextMessage.ReasonPhrase}");
                return false;
            }

            if (!broadcastImage)
            {
                return true;
            }

            if (!File.Exists(ntfySendingContent.ImagePath))
            {
                Logger.Error("Image file not found when to sending a NTFY message.");
                return true;
            }

            using var imageRequest = new HttpRequestMessage(HttpMethod.Post, requestUri);
            imageRequest.Headers.Authorization = authorization;
            imageRequest.Headers.Add("X-Filename", Path.GetFileName(ntfySendingContent.ImagePath));
            imageRequest.Content = new ByteArrayContent(await File.ReadAllBytesAsync(ntfySendingContent.ImagePath, token));

            using var responseBroadcastImage = await HttpClient.SendAsync(imageRequest, token);
            if (!responseBroadcastImage.IsSuccessStatusCode)
            {
                Logger.Error($"Failed to send NTFY image message: {responseBroadcastImage.ReasonPhrase}");
            }

            return true;
        }

        private static AuthenticationHeaderValue? CreateAuthorization(NtfySendingCredentials credentials)
        {
            if (!string.IsNullOrWhiteSpace(credentials.Token))
            {
                return new AuthenticationHeaderValue("Bearer", credentials.Token);
            }

            if (!string.IsNullOrWhiteSpace(credentials.Username) && !string.IsNullOrWhiteSpace(credentials.Password))
            {
                var byteArray = Encoding.UTF8.GetBytes($"{credentials.Username}:{credentials.Password}");
                return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            }

            return null;
        }
    }
}
