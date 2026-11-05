#region "copyright"

/*
    Copyright © 2026 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Globalization;
using NINA.Core.Utility;
using NINA.StarMessenger.Communication.Trigger;
using NINA.StarMessenger.Utils;

namespace NINA.StarMessenger.Processing
{
    /// <summary>
    /// Evaluates the conditions of a 'by condition' trigger against one image. Conditions are OR-combined.
    /// A condition only counts as fulfilled once it has been met for the aimed consecutive count of images.
    /// </summary>
    internal static class ConditionEvaluator
    {
        /// <returns>The property names of all conditions that are fulfilled for this image.</returns>
        public static IReadOnlyCollection<string> Evaluate<T>(IEnumerable<Condition<T>> conditions,
                                                              ImageSnapshot snapshot,
                                                              Func<string, bool> isPropertyEnabled)
        {
            var fulfilledProperties = new List<string>();

            foreach (var condition in conditions.ToList())
            {
                var property = condition.SelectedPropertyForCondition;
                if (property?.PropertyName == null || condition.ValueForCondition == null ||
                    string.IsNullOrEmpty(condition.SelectedOperatorForCondition))
                {
                    continue;
                }

                if (!isPropertyEnabled(property.PropertyName))
                {
                    Logger.Info($"Selected property: {property.PropertyUserFriendlyName} for condition is not enabled in configuration.");
                    continue;
                }

                var actualValue = snapshot.GetValue(property.PropertyName);
                if (actualValue == null)
                {
                    Logger.Debug($"No value for {property.PropertyUserFriendlyName} available for image {snapshot.ImageId}. Condition skipped.");
                    continue;
                }

                if (!TryParseExpectedValue(property.DataType, condition.ValueForCondition, out var expectedValue))
                {
                    Logger.Error($"Condition value '{condition.ValueForCondition}' for {property.PropertyUserFriendlyName} could not be parsed.");
                    continue;
                }

                Logger.Debug($"Condition for image {snapshot.ImageId}: actualValue = {actualValue} {condition.SelectedOperatorForCondition} conditionValue = {expectedValue}");

                if (IsFulfilledByOperator(property.DataType, actualValue, expectedValue, condition.SelectedOperatorForCondition))
                {
                    if (IsAimedConsecutiveCountReached(condition))
                    {
                        fulfilledProperties.Add(property.PropertyName);
                    }
                }
                else
                {
                    condition.ConsecutiveCountStatusLevel = ConsecutiveCountStatusLevelType.Ok;
                    condition.CurrentConsecutiveCount = 0;
                }
            }

            return fulfilledProperties;
        }

        internal static bool TryParseExpectedValue(Type? dataType, string? value, out object expectedValue)
        {
            expectedValue = string.Empty;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (dataType == typeof(double) || dataType == typeof(int))
            {
                if (double.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
                        CultureInfo.CurrentCulture, out var number))
                {
                    expectedValue = number;
                    return true;
                }

                return false;
            }

            if (dataType == typeof(DateTime))
            {
                if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dateTime))
                {
                    expectedValue = dateTime;
                    return true;
                }

                return false;
            }

            expectedValue = value;
            return true;
        }

        private static bool IsFulfilledByOperator(Type? dataType, object actualValue, object expectedValue, string selectedOperator)
        {
            if (dataType == typeof(DateTime))
            {
                if (actualValue is not DateTime actualDateTime || expectedValue is not DateTime expectedDateTime)
                {
                    return false;
                }

                return Compare(DataParser.ConvertDateTimeToDouble(actualDateTime),
                               DataParser.ConvertDateTimeToDouble(expectedDateTime),
                               selectedOperator);
            }

            if (dataType == typeof(double) || dataType == typeof(int))
            {
                if (expectedValue is not double expectedNumber)
                {
                    return false;
                }

                double actualNumber;
                try
                {
                    actualNumber = Convert.ToDouble(actualValue, CultureInfo.CurrentCulture);
                }
                catch (Exception)
                {
                    return false;
                }

                return Compare(actualNumber, expectedNumber, selectedOperator);
            }

            var actualText = actualValue.ToString();
            var expectedText = expectedValue.ToString();
            return selectedOperator == "=" ? actualText == expectedText : actualText != expectedText;
        }

        private static bool Compare(double actual, double expected, string selectedOperator)
        {
            return selectedOperator switch
            {
                "<" => Utilities.LessThan(actual, expected),
                ">" => Utilities.GreaterThan(actual, expected),
                "=" => Utilities.EqualTo(actual, expected),
                _ => false
            };
        }

        private static bool IsAimedConsecutiveCountReached<T>(Condition<T> condition)
        {
            var aimedConsecutiveCount = 1;
            if (!string.IsNullOrWhiteSpace(condition.AimedConsecutiveCount) &&
                int.TryParse(condition.AimedConsecutiveCount, out var parsed) && parsed > 0)
            {
                aimedConsecutiveCount = parsed;
            }

            // Once the aimed count is reached, the counter stays there. This avoids an overflow during long sessions.
            condition.CurrentConsecutiveCount = Math.Min(condition.CurrentConsecutiveCount + 1, aimedConsecutiveCount);

            condition.ConsecutiveCountStatusLevel = condition.CurrentConsecutiveCount < aimedConsecutiveCount
                ? ConsecutiveCountStatusLevelType.Warning
                : ConsecutiveCountStatusLevelType.Error;

            return condition.CurrentConsecutiveCount >= aimedConsecutiveCount;
        }
    }
}
