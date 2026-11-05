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
    /// Immutable set of values of one saved image. Values are keyed by the property names used in the
    /// settings (e.g. nameof(Settings.Default.HFR)). A missing key means the value is not available for this image.
    /// </summary>
    internal sealed class ImageSnapshot
    {
        private readonly IReadOnlyDictionary<string, object> _values;

        public ImageSnapshot(int imageId, string imageType, string? localPath, IReadOnlyDictionary<string, object> values)
        {
            ImageId = imageId;
            ImageType = imageType;
            LocalPath = localPath;
            _values = values;
        }

        public int ImageId { get; }

        public string ImageType { get; }

        public string? LocalPath { get; }

        public object? GetValue(string? propertyName)
        {
            if (propertyName == null)
            {
                return null;
            }

            return _values.TryGetValue(propertyName, out var value) ? value : null;
        }
    }
}
