#region "copyright"

/*
    Copyright © 2024 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"
namespace NINA.StarMessenger.Utils
{
    internal static class DataParser
    {
        internal static (bool IsSuccessfullyParsed, object? firstValueParsed, object? secondValueParsed)
            TryParseAccordingToDataType(Type? dataType, string? actualValue, string? parsedValue)
        {
            if (dataType == typeof(double))
            {
                if (double.TryParse(actualValue, out var actualValueDouble) &&
                    double.TryParse(parsedValue, out var parsedValueDouble))
                {
                    return new ValueTuple<bool, object, object>(true, actualValueDouble, parsedValueDouble);
                }
            }

            if (dataType == typeof(int))
            {
                if (int.TryParse(actualValue, out var actualValueInt) &&
                    int.TryParse(parsedValue, out var parsedValueInt))
                {
                    return new ValueTuple<bool, object, object>(true, actualValueInt, parsedValueInt);
                }
            }

            if (dataType == typeof(DateTime))
            {
                if (DateTime.TryParse(actualValue, out var actualValueDateTime) &&
                    DateTime.TryParse(parsedValue, out var parsedValueDateTime))
                {
                    return new ValueTuple<bool, object, object>(true, actualValueDateTime, parsedValueDateTime);
                }
            }

            if (dataType == typeof(string))
            {
                if (!string.IsNullOrWhiteSpace(actualValue) && !string.IsNullOrWhiteSpace(parsedValue))
                {
                    return new ValueTuple<bool, object?, object?>(true, actualValue, parsedValue);
                }
            }

            return (false, null, null);
        }

        internal static double ConvertDateTimeToDouble(DateTime dateTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var timeSinceEpoch = dateTime.ToUniversalTime() - epoch;
            return timeSinceEpoch.TotalSeconds;
        }
    }

}
