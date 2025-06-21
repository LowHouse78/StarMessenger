#region "copyright"

/*
    Copyright © 2024 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Drawing;
using NINA.Core.Utility;

namespace NINA.StarMessenger.Utils
{
    internal static class Utilities
    {
        internal static Func<double, double, bool> LessThan = (x, y) => x < y;
        internal static Func<double, double, bool> GreaterThan = (x, y) => x > y;
        internal static Func<double, double, bool> EqualTo = (x, y) => Math.Abs(x - y) < 0.01;

        public static Bitmap? ResizeImage(Bitmap? bitmap)
        {
            try
            {
                if (bitmap == null)
                {
                    return null;
                }

                var newWidth = bitmap.Width / 5;
                var newHeight = bitmap.Height / 5;

                var resizedBitmap = new Bitmap(newWidth, newHeight);

                using (Graphics graphics = Graphics.FromImage(resizedBitmap))
                {
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                    graphics.DrawImage(bitmap, 0, 0, newWidth, newHeight);
                }

                return resizedBitmap;
            }
            catch (Exception e)
            {
                Logger.Error($"Exception during ResizeImage: {e.Message}", e);
                return null;
            }
        }
    }
}

