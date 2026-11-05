#region "copyright"

/*
    Copyright © 2024 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion

using NINA.StarMessenger.Processing;
using NINA.StarMessenger.Utils;
using NINA.StarMessenger.Communication.Ntfy.DTOs;
using System.Net.Http;
using NINA.Core.Utility;

namespace NINA.StarMessenger.Communication.Ntfy
{
    internal sealed class NtfyClient : IStarMessageChannel
    {
        private readonly string _priority;

        /// <param name="priority">Ntfy priority value, e.g. "default".</param>
        public NtfyClient(string priority)
        {
            _priority = priority;
        }

        /// <summary>Creates a client from the name shown in the sequencer (PriorityTypes enum name).</summary>
        internal static NtfyClient Create(string? priority)
        {
            var parsedPriority = NtfyPrioritization.GetValueByEnum(PriorityTypes.Normal);
            if (Enum.TryParse(typeof(PriorityTypes), priority, true, out var tempPriority) && tempPriority != null)
            {
                parsedPriority = NtfyPrioritization.GetValueByEnum((PriorityTypes)tempPriority);
            }

            return new NtfyClient(parsedPriority);
        }

        public string Name => "Ntfy";

        public async Task<bool> SendAsync(string message, string? imageFilePath, CancellationToken token)
        {
            try
            {
                // Settings are read on every send, so changes in the options apply immediately. Credentials are decrypted exactly once here.
                var notifier = new NtfyNotifier(Settings.Default.NtfyServer ?? string.Empty, Settings.Default.NtfyTopic ?? string.Empty);
                var credentials = new NtfySendingCredentials(DataProtector.DecryptSetting(Settings.Default.NtfyUser),
                                                             DataProtector.DecryptSetting(Settings.Default.NtfyPassword),
                                                             DataProtector.DecryptSetting(Settings.Default.NtfyToken));
                var content = new NtfySendingContent("StarMessage", message, imageFilePath ?? string.Empty, _priority);

                return await notifier.SendMessageWithImageAsync(content, credentials, !string.IsNullOrWhiteSpace(imageFilePath), token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException e)
            {
                Logger.Error("Request error while trying to send a Ntfy message.", e);
            }
            catch (UriFormatException e)
            {
                Logger.Error($"Invalid URL while trying to send a Ntfy message: {e.Message}", e);
            }
            catch (Exception e)
            {
                Logger.Error($"Unexpected error while trying to send a Ntfy message: {e.Message}", e);
            }

            return false;
        }
    }
}
