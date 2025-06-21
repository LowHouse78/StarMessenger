#region "copyright"

/*
    Copyright © 2024 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;


namespace NINA.StarMessenger.Communication.Ntfy
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PriorityTypes
    {
        [EnumMember(Value = "min")]
        Lowest,
        [EnumMember(Value = "low")]
        Low,
        [EnumMember(Value = "default")]
        Normal,
        [EnumMember(Value = "high")]
        High,
        [EnumMember(Value = "max")]
        Highest

    }

    public static class NtfyPrioritization
    {
        public static string GetValueByEnum(PriorityTypes priorityType)
        {
            var fieldInfo = priorityType.GetType().GetField(priorityType.ToString());
            var attribute = fieldInfo?.GetCustomAttribute<EnumMemberAttribute>();
            return attribute?.Value ?? "default";
        }
    }
}
