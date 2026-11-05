#region "copyright"

/*
    Copyright © 2024 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System.ComponentModel;

namespace NINA.StarMessenger.Providers
{
    /// <summary>
    /// Catalog of the image properties a StarMessage can contain. The values themselves come from
    /// <see cref="Processing.ImageSnapshot"/>; here only name, display name, data type and the enabled state are kept.
    /// </summary>
    internal static class DataProvider
    {
        private static readonly object LockObject = new();
        private static readonly List<PropertyItem> Properties = new();

        public static IList<PropertyItem> GetProperties()
        {
            lock (LockObject)
            {
                return Properties.ToList();
            }
        }

        public static IList<PropertyItem> GetEnabledProperties()
        {
            lock (LockObject)
            {
                return Properties.Where(s => s.IsEnabled).ToList();
            }
        }

        public static bool IsPropertyEnabled(string propertyName)
        {
            lock (LockObject)
            {
                return Properties.FirstOrDefault(s => s.PropertyName == propertyName)?.IsEnabled ?? false;
            }
        }

        public static void Configure()
        {
            try
            {
                lock (LockObject)
                {
                    if (Properties.Count > 0)
                    {
                        return;
                    }

                    Settings.Default.PropertyChanged += SettingsChanged;

                    // The order defines the order of the lines in a StarMessage.
                    AddProperty(nameof(Settings.Default.Time), "Timestamp", Settings.Default.Time, typeof(DateTime));
                    AddProperty(nameof(Settings.Default.RMSTotal), "RMS Total", Settings.Default.RMSTotal, typeof(double));
                    AddProperty(nameof(Settings.Default.RMSDec), "RMS Dec", Settings.Default.RMSDec, typeof(double));
                    AddProperty(nameof(Settings.Default.RMSRa), "RMS RA", Settings.Default.RMSRa, typeof(double));
                    AddProperty(nameof(Settings.Default.HFR), "HFR", Settings.Default.HFR, typeof(double));
                    AddProperty(nameof(Settings.Default.Mean), "Mean", Settings.Default.Mean, typeof(double));
                    AddProperty(nameof(Settings.Default.Median), "Median", Settings.Default.Median, typeof(double));
                    AddProperty(nameof(Settings.Default.Min), "Min", Settings.Default.Min, typeof(double));
                    AddProperty(nameof(Settings.Default.Max), "Max", Settings.Default.Max, typeof(double));
                    AddProperty(nameof(Settings.Default.Temperature), "Temperature", Settings.Default.Temperature, typeof(double));
                    AddProperty(nameof(Settings.Default.SkyQuality), "Sky Quality", Settings.Default.SkyQuality, typeof(double));
                    AddProperty(nameof(Settings.Default.Bortle), "Bortle", Settings.Default.Bortle, typeof(int));
                    AddProperty(nameof(Settings.Default.FocuserPosition), "Focus Position", Settings.Default.FocuserPosition, typeof(int));
                    AddProperty(nameof(Settings.Default.Filter), "Filter", Settings.Default.Filter, typeof(string));
                    AddProperty(nameof(Settings.Default.RotatorMechanicalPosition), "Rotator mech. pos.", Settings.Default.RotatorMechanicalPosition, typeof(double));
                    AddProperty(nameof(Settings.Default.StDev), "Std. Dev.", Settings.Default.StDev, typeof(double));
                    AddProperty(nameof(Settings.Default.RotatorPosition), "Rotor Position", Settings.Default.RotatorPosition, typeof(double));
                    AddProperty(nameof(Settings.Default.Stars), "Stars", Settings.Default.Stars, typeof(int));
                    AddProperty(nameof(Settings.Default.Duration), "Duration", Settings.Default.Duration, typeof(double));
                    AddProperty(nameof(Settings.Default.MAD), "MAD", Settings.Default.MAD, typeof(double));
                    AddProperty(nameof(Settings.Default.Target), "Target", Settings.Default.Target, typeof(string));
                }
            }
            catch (Exception e)
            {
                Logger.Error("Exception during Configure", e);
            }
        }

        private static void AddProperty(string name, string userFriendlyName, bool isSettingEnabled, Type dataType)
        {
            Properties.Add(new PropertyItem(name, userFriendlyName, isSettingEnabled, dataType, new Dictionary<Type, bool>()));
        }

        private static void SettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                if (sender == null || e.PropertyName == null || e.PropertyName == nameof(Settings.Default.Image))
                {
                    return;
                }

                if (sender.GetType().GetProperty(e.PropertyName)?.GetValue(sender) is not bool newValue)
                {
                    return;
                }

                lock (LockObject)
                {
                    var appliedProperty = Properties.FirstOrDefault(s => s.PropertyName == e.PropertyName);
                    if (appliedProperty != null)
                    {
                        appliedProperty.IsEnabled = newValue;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while trying to change the settings: {ex.Message}", ex);
            }
        }
    }
}
