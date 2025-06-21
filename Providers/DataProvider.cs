#region "copyright"

/*
    Copyright © 2024 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.IO;
using System.Text;
using NINA.StarMessenger.Utils;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Model;
using NINA.Image.Interfaces;
using NINA.Core.Enum;
using NINA.Image.ImageAnalysis;
using System.Diagnostics.CodeAnalysis;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using System.ComponentModel;

namespace NINA.StarMessenger.Providers
{
    [SuppressMessage("Interoperability", "CA1416", Justification = "N.I.N.A. is Windows-only")]
    internal static class DataProvider
    {
        private static readonly IList<PropertyItem> Properties = new List<PropertyItem>();
        private static IImageHistoryVM? _imageHistoryVM;
        private static IImageDataFactory? _imageDataFactory;
        private static IProfileService? _profileService;
        private static readonly Dictionary<Type, DateTime> LastSentImageInfo = new Dictionary<Type, DateTime>();
        private static bool _skipCurrentIteration;


        public static IList<PropertyItem> GetProperties()
        {
            return Properties;
        }

        public static void ResetConditionIsFulfilledInformation<T>()
        {
            try
            {
                Properties.ToList().ForEach(s => s.ConditionFulfilled[typeof(T)] = false);
            }
            catch (Exception e)
            {
                Logger.Error($"Error during ResetConditionIsFulfilledInformation Error: {e.Message}", e);
            }
        }

        public static void SetPropertyConditionIsFulfilled<T>(string? propertyName)
        {
            try
            {
                var foundProperty = Properties.FirstOrDefault(s => s.PropertyName == propertyName);
                if (foundProperty != null)
                {
                    foundProperty.ConditionFulfilled[typeof(T)] = true;
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error during SetPropertyConditionIsFulfilled Error: {e.Message}", e);
            }
        }

        public static async Task<string> RetrieveImageInformationDataAsync<T>(CancellationToken token, TriggerSourceTypes triggerSource = TriggerSourceTypes.Default)
        {
            var result = new StringBuilder();
            try
            {
                _skipCurrentIteration = false;
                await Task.Run(() =>
                {

                    foreach (var property in Properties.Where(s => s.IsEnabled))
                    {
                        var hintConditionFulfilled = string.Empty;
                        if (triggerSource == TriggerSourceTypes.ByCondition)
                        {
                            hintConditionFulfilled = string.Empty;
                            if (property.ConditionFulfilled.ContainsKey(typeof(T)))
                            {
                                hintConditionFulfilled = property.ConditionFulfilled[typeof(T)]
                                    ? "    -> Condition fulfilled"
                                    : "";
                            }
                        }

                        var receivedValue = property.GetCurrentValueFunc?.Invoke()?.ToString();
                        if (receivedValue != null)
                        {
                            result.AppendLine(
                                $"{property.PropertyUserFriendlyName}: {receivedValue}{hintConditionFulfilled}");

                        }
                    }

                    if (string.IsNullOrEmpty(result.ToString()))
                    {
                        result.AppendLine($"StarMessage - no new image in history found");
                        result.AppendLine(" ");
                    }

                    if (triggerSource == TriggerSourceTypes.ByExposures)
                    {
                        result.AppendLine("Condition: Send StarMessage after exposures fulfilled!");
                        result.AppendLine(" ");
                    }

                }, token);
            }
            catch (TaskCanceledException e)
            {
                Logger.Error($"Request was canceled while RetrieveImageInformationDataAsync", e);
            }
            catch (Exception e)
            {
                Logger.Error($"Unexpected error during RetrieveImageInformationDataAsync Error: {e.Message}", e);
            }

            return result.ToString();
        }

        public static async Task<bool> HasCurrentImageAlreadyBeenSent<T>(bool doNotUpdate = false)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    var currentImageInfoTimeStamp =
                        await RetrieveSingleImageInformationDataAsync(propertyName: nameof(Settings.Default.Time));

                    if (DateTime.TryParse(currentImageInfoTimeStamp, out var currentImageInfoTimeStampDateTime))
                    {
                        if (!LastSentImageInfo.TryGetValue(typeof(T), out var lastSentDateTime))
                        {
                            if (!doNotUpdate)
                            {
                                LastSentImageInfo[typeof(T)] = currentImageInfoTimeStampDateTime;
                            }

                            return false;
                        }

                        var lastSentDateTimeDouble = DataParser.ConvertDateTimeToDouble(lastSentDateTime);
                        var currentImageInfoTimeStampDouble =
                            DataParser.ConvertDateTimeToDouble(currentImageInfoTimeStampDateTime);
                        if (currentImageInfoTimeStampDouble <= lastSentDateTimeDouble)
                        {
                            return true;
                        }

                        if (!doNotUpdate)
                        {
                            LastSentImageInfo[typeof(T)] = currentImageInfoTimeStampDateTime;
                        }
                    }

                    return false;
                }
                catch (Exception e)
                {
                    Logger.Error($"Unexpected error during HasCurrentImageAlreadyBeenSent Error: {e.Message}", e);
                    return false;
                }
            });
        }

        public static async Task<string> RetrieveSingleImageInformationDataAsync(string? propertyName)
        {
            var result = string.Empty;
            try
            {
                _skipCurrentIteration = false;
                await Task.Run(() =>
                {
                    var property = Properties.FirstOrDefault(s => s.PropertyName == propertyName);
                    if (property != null)
                    {
                        var receivedValue = property.GetCurrentValueFunc?.Invoke()?.ToString();
                        if (receivedValue != null)
                        {
                            result = receivedValue;
                        }
                    }
                });
            }
            catch (Exception e)
            {
                Logger.Error($"Error during RetrieveSingleImageInformationDataAsync Error: {e.Message}", e);
            }

            return result;
        }

        public static async Task<string> RetrieveImageAsync()
        {
            var result = string.Empty;
            await Task.Run(async () =>
            {

                try
                {
                    if (!Settings.Default.Image ||
                        _imageHistoryVM?.ImageHistory == null ||
                        _imageHistoryVM.ImageHistory?.Count <= 0 ||
                        _imageDataFactory == null ||
                        _profileService == null
                       )
                    {
                        result = string.Empty;
                    }
                    else
                    {
                        var profile = _profileService.ActiveProfile;
                        var imageHistoryPoint = _imageHistoryVM.ImageHistory?.Last();
                        var imageData = await _imageDataFactory.CreateFromFile(imageHistoryPoint?.LocalPath, 16, true,
                            RawConverterEnum.FREEIMAGE);
                        var renderedImage = imageData.RenderImage();

                        renderedImage = await renderedImage.Stretch(profile.ImageSettings.AutoStretchFactor,
                            profile.ImageSettings.BlackClipping, profile.ImageSettings.UnlinkedStretch);
                        var reducedImage = ImageUtility.Convert16BppTo8Bpp(renderedImage.Image);

                        var resizedImage = Utilities.ResizeImage(reducedImage);

                        var imageFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jpg");

                        resizedImage?.Save(imageFilePath);
                        if (File.Exists(imageFilePath))
                        {
                            result = imageFilePath;
                        }
                        else
                        {
                            result = string.Empty;
                        }
                    }
                }
                catch (TaskCanceledException e)
                {
                    Logger.Error($"Request was canceled when RetrieveImageAsync", e);
                }
                catch (Exception e)
                {
                    Logger.Error("Exception during RetrieveImageAsync", e);
                    result = string.Empty;
                }

                return result;
            });
            return result;
        }

        public static void SkipCurrentProcessing()
        {
            ImageDataValueChecker.BreakProcessing();
        }

        public static void Configure(IImageHistoryVM imageHistoryVM,
            IImageDataFactory imageDataFactory,
            IProfileService profileService)
        {
            try
            {
                Settings.Default.PropertyChanged += SettingsChanged;

                _imageHistoryVM = imageHistoryVM;
                _imageDataFactory = imageDataFactory;
                _profileService = profileService;

                /* It seems there is a timing issue while retrieving data from ImageHistoryPoint. If timespan between taking image and
                  retrieving data is too short, data aren't complete. For that reason, I try to get property "LocalPath" several times.
                  If successfully retrieved, this ImageHistoryPoint object is used for getting all other properties. If timeout thrown,
                  the previous ImageHistoryPoint object in the list is taken. */


                var lastValidImageFunc = GetLastValidImageInformationFunc();

                AddImageDataProperties(nameof(Settings.Default.Time), "Timestamp", Settings.Default.Time,
                    () => lastValidImageFunc.Invoke()?.dateTime, typeof(DateTime));
                AddImageDataProperties(nameof(Settings.Default.RMSTotal), "RMS Total", Settings.Default.RMSTotal, () =>
                {
                    var rms = lastValidImageFunc.Invoke()?.Rms;
                    return rms != null ? Math.Round((double)rms, 2) : null;
                }, typeof(double));
                AddImageDataProperties(nameof(Settings.Default.RMSDec), "RMS Dec", Settings.Default.RMSDec, () =>
                {
                    var rmsDec = CalculateRMSDecFunc(lastValidImageFunc).Invoke();
                    return rmsDec != null ? Math.Round((double)rmsDec, 2) : null;
                }, typeof(double));
                AddImageDataProperties(nameof(Settings.Default.RMSRa), "RMS RA", Settings.Default.RMSRa, () =>
                {
                    var rmsRa = CalculateRMSRaFunc(lastValidImageFunc).Invoke();
                    return rmsRa != null ? Math.Round((double)rmsRa, 2) : null;
                }, typeof(double));
                AddImageDataProperties(nameof(Settings.Default.HFR), "HFR", Settings.Default.HFR, () =>
                {
                    var hfr = lastValidImageFunc.Invoke()?.HFR;
                    return hfr != null ? Math.Round((double)hfr, 2) : null;
                }, typeof(double));
                AddImageDataProperties(nameof(Settings.Default.Mean), "Mean", Settings.Default.Mean, () =>
                {
                    var mean = lastValidImageFunc.Invoke()?.Mean;
                    return mean != null ? Math.Round((double)mean, 2) : null;
                }, typeof(double));

                AddImageDataProperties(nameof(Settings.Default.Median), "Median", Settings.Default.Median, () =>
                {
                    var median = lastValidImageFunc.Invoke()?.Median;
                    return median != null ? Math.Round((double)median, 2) : null;
                }, typeof(double));


                AddImageDataProperties(nameof(Settings.Default.Min), "Min", Settings.Default.Min, () =>
                {
                    var min = lastValidImageFunc.Invoke()?.Statistics.Min;
                    return min != null ? Math.Round((double)min, 2) : null;
                }, typeof(double));
                AddImageDataProperties(nameof(Settings.Default.Max), "Max", Settings.Default.Max, () =>
                {
                    var max = lastValidImageFunc.Invoke()?.Statistics.Max;
                    return max != null ? Math.Round((double)max, 2) : null;
                }, typeof(double));
                AddImageDataProperties(nameof(Settings.Default.Temperature), "Temperature",
                    Settings.Default.Temperature, () =>
                    {
                        var temperature = lastValidImageFunc.Invoke()?.Temperature;
                        return temperature != null ? Math.Round((double)temperature, 2) : null;
                    }, typeof(double));
                AddImageDataProperties(nameof(Settings.Default.FocuserPosition), "Focus Position",
                    Settings.Default.FocuserPosition, () => lastValidImageFunc.Invoke()?.FocuserPosition, typeof(int));
                AddImageDataProperties(nameof(Settings.Default.Filter), "Filter", Settings.Default.Filter,
                    () => lastValidImageFunc.Invoke()?.Filter, typeof(string));
                AddImageDataProperties(nameof(Settings.Default.RotatorMechanicalPosition), "Rotator mech. pos.",
                    Settings.Default.RotatorMechanicalPosition,
                    () => lastValidImageFunc.Invoke()?.RotatorMechanicalPosition, typeof(int));
                AddImageDataProperties(nameof(Settings.Default.StDev), "Std. Dev.", Settings.Default.StDev, () =>
                {
                    var stdDev = lastValidImageFunc.Invoke()?.StDev;
                    return stdDev != null ? Math.Round((double)stdDev, 2) : null;
                }, typeof(double));
                AddImageDataProperties(nameof(Settings.Default.RotatorPosition), "Rotor Position",
                    Settings.Default.RotatorPosition, () => lastValidImageFunc.Invoke()?.RotatorPosition, typeof(int));
                AddImageDataProperties(nameof(Settings.Default.Stars), "Stars", Settings.Default.Stars,
                    () => lastValidImageFunc.Invoke()?.Stars, typeof(int));
                AddImageDataProperties(nameof(Settings.Default.Duration), "Duration", Settings.Default.Duration,
                    () => lastValidImageFunc.Invoke()?.Duration, typeof(double));
                AddImageDataProperties(nameof(Settings.Default.MAD), "MAD", Settings.Default.MAD,
                    () => lastValidImageFunc.Invoke()?.MAD, typeof(double));

                AddImageDataProperties(nameof(Settings.Default.Target), "Target", Settings.Default.Target,
                    () =>
                    {
                        var target = lastValidImageFunc.Invoke()?.Target;
                        if ((!string.IsNullOrWhiteSpace(target?.Coordinates?.DecString) &&
                            !string.IsNullOrWhiteSpace(target.Coordinates?.RAString)) || 
                            !string.IsNullOrWhiteSpace(target?.Name))
                        {
                            return $"{target.Name}  DEC: {target.Coordinates?.DecString} RA: {target.Coordinates?.RAString}";
                        }
                        return null;
                    }, typeof(string));

            }
            catch (Exception e)
            {
                Logger.Error("Exception during Configure", e);
            }
        }

        private static Func<double?> CalculateRMSDecFunc(Func<ImageHistoryPoint?> lastValidImageFunc)
        {
            return () =>
            {
                var scale = lastValidImageFunc.Invoke()?.RecordedRMS.Scale;
                return scale * lastValidImageFunc.Invoke()?.RecordedRMS.Dec;
            };
        }

        private static Func<double?> CalculateRMSRaFunc(Func<ImageHistoryPoint?> lastValidImageFunc)
        {
            return () =>
            {
                var scale = lastValidImageFunc.Invoke()?.RecordedRMS.Scale;
                return scale * lastValidImageFunc.Invoke()?.RecordedRMS.RA;
            };
        }

        private static Func<ImageHistoryPoint?> GetLastValidImageInformationFunc()
        {
            return () =>
            {
                if (_skipCurrentIteration)
                {
                    return null;
                }

                ImageHistoryPoint? result;

                var validImageData = ImageDataValueChecker.IsImageWithValidDataFound(
                    () =>
                    {
                        var elementsFound = _imageHistoryVM?.ImageHistory != null &&
                                            _imageHistoryVM is { ImageHistory.Count: > 0 };

                        return elementsFound
                            ? _imageHistoryVM?.ImageHistory?.Where(s => s.Type == "LIGHT").Last()
                            : null;
                    }, 15000);


                result = validImageData;

                _skipCurrentIteration = result == null;
                return result;
            };
        }

        private static void AddImageDataProperties(string name, string userFriendlyName, bool isSettingEnabled,
            Func<object?> getValueFunc, Type dataType)
        {

            Properties.Add(new PropertyItem(name, userFriendlyName, isSettingEnabled, dataType,
                new Dictionary<Type, bool>(), getValueFunc));
        }

        private static void SettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                if (sender == null || e.PropertyName == null || e.PropertyName == "Image")
                {
                    return;
                }
                var settingsType = sender.GetType();
                var settingsPropertyInfo = settingsType.GetProperty(e.PropertyName);
                var settingsValue = settingsPropertyInfo?.GetValue(sender);
                if (settingsValue is bool newValue)
                {
                    var appliedProperty = Properties.FirstOrDefault(s => s.PropertyName == e.PropertyName);
                    var appliedPropertyType = appliedProperty?.GetType();
                    var appliedPropertyInfo = appliedPropertyType?.GetProperty(nameof(PropertyItem.IsEnabled));
                    appliedPropertyInfo?.SetValue(appliedProperty, newValue);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while trying to change the settings: {ex.Message}", ex);
            }
        }
    }
}