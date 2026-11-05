#region "copyright"

/*
    Copyright © 2024 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Text.Json;
using System.Text.Json.Serialization;

namespace NINA.StarMessenger.Communication.Pushover
{
    /// <summary>
    /// Response of the Pushover API, e.g. {"status":1,"request":"..."}. The property names are lower case.
    /// </summary>
    internal class PushoverResponse
    {
        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("request")]
        public string? Request { get; set; }

        [JsonPropertyName("errors")]
        public List<string>? Errors { get; set; }

        [JsonIgnore]
        public bool IsAccepted => Status == 1;

        /// <returns>The parsed response or null if the content is no valid JSON.</returns>
        public static PushoverResponse? Parse(string responseContent)
        {
            try
            {
                return JsonSerializer.Deserialize<PushoverResponse>(responseContent);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
