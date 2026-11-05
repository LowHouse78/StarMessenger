#region "copyright"

/*
    Copyright © 2026 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace NINA.StarMessenger.Processing
{
    /// <summary>
    /// Converts a sky quality reading (SQM, mag/arcsec²) into the Bortle class. The Bortle scale is not measured,
    /// it is an estimate based on a commonly used SQM table.
    /// </summary>
    internal static class BortleScale
    {
        private static readonly (double MinimumSkyQuality, int BortleClass)[] Classes =
        {
            (21.99, 1),
            (21.89, 2),
            (21.69, 3),
            (20.49, 4),
            (19.50, 5),
            (18.94, 6),
            (18.38, 7),
            (17.80, 8)
        };

        public static int FromSkyQuality(double skyQuality)
        {
            foreach (var (minimumSkyQuality, bortleClass) in Classes)
            {
                if (skyQuality >= minimumSkyQuality)
                {
                    return bortleClass;
                }
            }

            return 9;
        }
    }
}
