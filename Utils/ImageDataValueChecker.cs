#region "copyright"

/*
    Copyright © 2024 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.WPF.Base.Model;

namespace NINA.StarMessenger.Utils
{
    public static class ImageDataValueChecker
    {
        private static readonly object LockObject = new();
        private static bool _breakProcessing;

        public static void BreakProcessing()
        {
            _breakProcessing = true;
        }

        public static ImageHistoryPoint? IsImageWithValidDataFound(Func<ImageHistoryPoint?> targetValueFunc,
                                                                   int timeoutInMilliSec)
        {
            lock (LockObject)
            {
                ImageHistoryPoint? valueToCheck = null;
                using var cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(timeoutInMilliSec);

                try
                {
                    while (string.IsNullOrEmpty(valueToCheck?.LocalPath))
                    {
                        if (_breakProcessing || cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            _breakProcessing = false;
                            Logger.Error("Timeout: IsImageWithValidDataFound cancelled");
                            return null;
                        }
                        Task.Delay(150, cancellationTokenSource.Token);
                        valueToCheck = targetValueFunc.Invoke();
                    }
                }
                catch (TaskCanceledException)
                {
                    return null;
                }
                catch (Exception e)
                {
                    Logger.Error("Unexpected exception during IsImageWithValidDataFound", e);
                }

                Logger.Debug("Valid data found during IsImageWithValidDataFound");
                return valueToCheck;
            }
        }


    }
}