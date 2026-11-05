#region "copyright"

/*
    Copyright © 2026 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.WPF.Base.Interfaces.Mediator;

namespace NINA.StarMessenger.Processing
{
    /// <summary>
    /// Builds an <see cref="ImageSnapshot"/> directly from N.I.N.A.'s ImageSaved event, so the values belong
    /// to exactly the image that has just been written to disk.
    /// </summary>
    internal static class ImageSnapshotFactory
    {
        public static ImageSnapshot Create(ImageSavedEventArgs e)
        {
            var values = new Dictionary<string, object>();
            var metaData = e.MetaData;
            var image = metaData?.Image;

            AddValue(values, nameof(Settings.Default.Time), GetTimestamp(image?.ExposureStart, image?.ExposureTime));

            var rms = image?.RecordedRMS;
            if (rms != null)
            {
                AddRounded(values, nameof(Settings.Default.RMSTotal), rms.Total * rms.Scale);
                AddRounded(values, nameof(Settings.Default.RMSRa), rms.RA * rms.Scale);
                AddRounded(values, nameof(Settings.Default.RMSDec), rms.Dec * rms.Scale);
            }

            if (e.StarDetectionAnalysis != null)
            {
                AddRounded(values, nameof(Settings.Default.HFR), e.StarDetectionAnalysis.HFR);
                AddValue(values, nameof(Settings.Default.Stars), e.StarDetectionAnalysis.DetectedStars);
            }

            var statistics = e.Statistics;
            if (statistics != null)
            {
                AddRounded(values, nameof(Settings.Default.Mean), statistics.Mean);
                AddRounded(values, nameof(Settings.Default.Median), statistics.Median);
                AddRounded(values, nameof(Settings.Default.Min), statistics.Min);
                AddRounded(values, nameof(Settings.Default.Max), statistics.Max);
                AddRounded(values, nameof(Settings.Default.StDev), statistics.StDev);
                AddNumber(values, nameof(Settings.Default.MAD), statistics.MedianAbsoluteDeviation);
            }

            var temperature = metaData?.Focuser?.Temperature ?? double.NaN;
            if (double.IsNaN(temperature))
            {
                temperature = metaData?.WeatherData?.Temperature ?? double.NaN;
            }
            AddRounded(values, nameof(Settings.Default.Temperature), temperature);

            // Sky quality of the weather device in N.I.N.A., e.g. the EYE sensor of a PrimaLuceLab EAGLE
            var skyQuality = metaData?.WeatherData?.SkyQuality ?? double.NaN;
            if (!double.IsNaN(skyQuality) && !double.IsInfinity(skyQuality))
            {
                AddRounded(values, nameof(Settings.Default.SkyQuality), skyQuality);
                AddValue(values, nameof(Settings.Default.Bortle), BortleScale.FromSkyQuality(skyQuality));
            }

            if (metaData?.Focuser?.Position is { } focuserPosition)
            {
                AddValue(values, nameof(Settings.Default.FocuserPosition), focuserPosition);
            }

            if (metaData?.Rotator != null)
            {
                AddRounded(values, nameof(Settings.Default.RotatorPosition), metaData.Rotator.Position);
                AddRounded(values, nameof(Settings.Default.RotatorMechanicalPosition), metaData.Rotator.MechanicalPosition);
            }

            if (!string.IsNullOrWhiteSpace(e.Filter))
            {
                AddValue(values, nameof(Settings.Default.Filter), e.Filter);
            }

            AddNumber(values, nameof(Settings.Default.Duration), e.Duration);

            var target = metaData?.Target;
            if (target != null &&
                (!string.IsNullOrWhiteSpace(target.Name) ||
                 (!string.IsNullOrWhiteSpace(target.Coordinates?.DecString) && !string.IsNullOrWhiteSpace(target.Coordinates?.RAString))))
            {
                AddValue(values, nameof(Settings.Default.Target),
                    $"{target.Name}  DEC: {target.Coordinates?.DecString} RA: {target.Coordinates?.RAString}");
            }

            return new ImageSnapshot(image?.Id ?? -1,
                                     image?.ImageType ?? string.Empty,
                                     e.PathToImage?.LocalPath,
                                     values);
        }

        private static DateTime GetTimestamp(DateTime? exposureStart, double? exposureTime)
        {
            if (exposureStart == null || exposureStart.Value == DateTime.MinValue)
            {
                return DateTime.Now;
            }

            var start = exposureStart.Value.Kind == DateTimeKind.Utc ? exposureStart.Value.ToLocalTime() : exposureStart.Value;
            return exposureTime is { } seconds && !double.IsNaN(seconds) ? start.AddSeconds(seconds) : start;
        }

        private static void AddValue(IDictionary<string, object> values, string name, object? value)
        {
            if (value != null)
            {
                values[name] = value;
            }
        }

        private static void AddNumber(IDictionary<string, object> values, string name, double value)
        {
            if (!double.IsNaN(value) && !double.IsInfinity(value))
            {
                values[name] = value;
            }
        }

        private static void AddRounded(IDictionary<string, object> values, string name, double value)
        {
            if (!double.IsNaN(value) && !double.IsInfinity(value))
            {
                values[name] = Math.Round(value, 2);
            }
        }
    }
}
