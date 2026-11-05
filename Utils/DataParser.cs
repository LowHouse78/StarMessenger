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
        internal static double ConvertDateTimeToDouble(DateTime dateTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var timeSinceEpoch = dateTime.ToUniversalTime() - epoch;
            return timeSinceEpoch.TotalSeconds;
        }
    }

}
