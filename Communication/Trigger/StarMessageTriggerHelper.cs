#region "copyright"

/*
    Copyright © 2026 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.StarMessenger.Processing;
using NINA.WPF.Base.Interfaces.ViewModel;

namespace NINA.StarMessenger.Communication.Trigger
{
    internal static class StarMessageTriggerHelper
    {
        public const string LightImageType = "LIGHT";

        /// <summary>Id of the latest light frame. N.I.N.A. adds it to the history directly after the download, before it is saved.</summary>
        public static int? GetLatestLightImageId(IImageHistoryVM history)
        {
            var lightImage = history.ImageHistory?.ToList().LastOrDefault(s => s.Type == LightImageType);
            return lightImage?.Id;
        }

        public static int GetLightImageCount(IImageHistoryVM history)
        {
            return history.ImageHistory?.ToList().Count(s => s.Type == LightImageType) ?? 0;
        }

        /// <summary>Hands the job to the dispatcher without waiting for it.</summary>
        public static Task<StarMessageJobResult> Enqueue(StarMessageJob job)
        {
            var dispatcher = StarMessageDispatcher.Default;
            if (dispatcher == null)
            {
                Logger.Error("StarMessenger is not initialized, StarMessage cannot be sent.");
                return Task.FromResult(StarMessageJobResult.Failed);
            }

            return dispatcher.Enqueue(job);
        }

        public static TimeSpan SaveTimeout => StarMessageDispatcher.Default?.SaveTimeout ?? new StarMessageDispatcherOptions().SaveTimeout;
    }
}
