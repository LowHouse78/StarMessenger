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
    /// A messenger service a StarMessage can be sent with (Pushover, Ntfy, Email).
    /// </summary>
    internal interface IStarMessageChannel
    {
        string Name { get; }

        /// <returns>true if the service accepted the message.</returns>
        Task<bool> SendAsync(string message, string? imageFilePath, CancellationToken token);
    }

    /// <summary>
    /// Creates the stretched and downscaled JPEG preview that is attached to a StarMessage.
    /// </summary>
    internal interface IPreviewImageProvider
    {
        /// <returns>Path of a temporary JPEG file or null if no preview should or could be created.
        /// The caller deletes the file after sending.</returns>
        Task<string?> CreatePreviewAsync(string imageFilePath, CancellationToken token);
    }
}
