#region "copyright"

/*
    Copyright © 2024 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.StarMessenger.Processing;
using NINA.StarMessenger.Utils;
using System.IO;
using System.Net.Http;

namespace NINA.StarMessenger.Communication.Pushover
{
    internal sealed class PushoverClient : IStarMessageChannel
    {
        private const string ApiUrl = "https://api.pushover.net/1/messages.json";
        private static readonly HttpClient HttpClient = new();

        private readonly string _sound;
        private readonly string _priority;

        /// <param name="sound">Pushover sound name, e.g. "pushover".</param>
        /// <param name="priority">Pushover priority as number, e.g. "0".</param>
        internal PushoverClient(string sound, string priority)
        {
            _sound = sound;
            _priority = priority;
        }

        /// <summary>Creates a client from the names shown in the sequencer (SoundTypes / PriorityTypes enum names).</summary>
        internal static PushoverClient Create(string? notificationSound, string? priority)
        {
            var parsedSound = SoundTypes.Pushover.ToString();
            if (Enum.TryParse(typeof(SoundTypes), notificationSound, true, out var sound) && sound != null)
            {
                parsedSound = sound.ToString() ?? parsedSound;
            }

            var parsedPriority = ((int)PriorityTypes.Normal).ToString();
            if (Enum.TryParse(typeof(PriorityTypes), priority, true, out var tempPriority) && tempPriority != null)
            {
                parsedPriority = ((int)(PriorityTypes)tempPriority).ToString();
            }

            return new PushoverClient(parsedSound, parsedPriority);
        }

        public string Name => "Pushover";

        public async Task<bool> SendAsync(string message, string? imageFilePath, CancellationToken token)
        {
            if (string.IsNullOrEmpty(message))
            {
                return false;
            }

            try
            {
                using var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(StarMessenger.PushoverAPIKey), "token");
                formData.Add(new StringContent(StarMessenger.PushoverUserKey), "user");
                formData.Add(new StringContent(message), "message");
                formData.Add(new StringContent(_sound.ToLower()), "sound");
                formData.Add(new StringContent(_priority), "priority");

                if (!string.IsNullOrEmpty(imageFilePath) && File.Exists(imageFilePath))
                {
                    var imageBytes = await File.ReadAllBytesAsync(imageFilePath, token);
                    formData.Add(new ByteArrayContent(imageBytes), "attachment", Path.GetFileName(imageFilePath));
                }

                using var response = await HttpClient.PostAsync(ApiUrl, formData, token);
                var responseContent = await response.Content.ReadAsStringAsync(token);
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error($"Pushover rejected the message: {(int)response.StatusCode} {responseContent}");
                    return false;
                }

                var result = PushoverResponse.Parse(responseContent);
                if (result?.IsAccepted != true)
                {
                    Logger.Error($"Pushover did not accept the message: {responseContent}");
                    return false;
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException e)
            {
                Logger.Error("Request error while trying to send a Pushover message.", e);
            }
            catch (Exception e)
            {
                Logger.Error($"Unexpected error while trying to send a Pushover message: {e.Message}", e);
            }

            return false;
        }
    }
}
