#region "copyright"

/*
    Copyright © 2026 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Text;
using NINA.StarMessenger.Providers;

namespace NINA.StarMessenger.Processing
{
    /// <summary>
    /// Builds the text of a StarMessage.
    /// </summary>
    internal static class StarMessageComposer
    {
        public const string NoImageInHistoryText = "StarMessage - no new image in history found";
        public const string ExposuresFulfilledText = "Condition: Send StarMessage after exposures fulfilled!";
        public const string ConditionFulfilledHint = "    -> Condition fulfilled";

        public static string Compose(ImageSnapshot snapshot,
                                     IEnumerable<PropertyItem> enabledProperties,
                                     TriggerSourceTypes triggerSource,
                                     IReadOnlyCollection<string>? fulfilledProperties = null)
        {
            var result = new StringBuilder();

            foreach (var property in enabledProperties)
            {
                var value = snapshot.GetValue(property.PropertyName);
                if (value == null)
                {
                    continue;
                }

                var hint = triggerSource == TriggerSourceTypes.ByCondition &&
                           fulfilledProperties != null &&
                           fulfilledProperties.Contains(property.PropertyName)
                    ? ConditionFulfilledHint
                    : string.Empty;

                result.AppendLine($"{property.PropertyUserFriendlyName}: {value}{hint}");
            }

            if (result.Length == 0)
            {
                result.AppendLine(NoImageInHistoryText);
                result.AppendLine(" ");
            }

            AppendFooter(result, triggerSource);
            return result.ToString();
        }

        /// <summary>
        /// Text for a StarMessage without image data: either there was no light frame yet (<paramref name="saveTimeout"/> is null)
        /// or the image was not saved within <paramref name="saveTimeout"/>.
        /// </summary>
        public static string ComposeWithoutImage(TriggerSourceTypes triggerSource, TimeSpan? saveTimeout = null)
        {
            var result = new StringBuilder();
            result.AppendLine(saveTimeout == null
                ? NoImageInHistoryText
                : GetImageNotSavedText(saveTimeout.Value));
            result.AppendLine(" ");
            AppendFooter(result, triggerSource);
            return result.ToString();
        }

        public static string GetImageNotSavedText(TimeSpan saveTimeout)
        {
            return $"StarMessage - the image was not saved within {saveTimeout.TotalSeconds:0} s";
        }

        private static void AppendFooter(StringBuilder result, TriggerSourceTypes triggerSource)
        {
            if (triggerSource == TriggerSourceTypes.ByExposures)
            {
                result.AppendLine(ExposuresFulfilledText);
                result.AppendLine(" ");
            }
        }
    }
}
