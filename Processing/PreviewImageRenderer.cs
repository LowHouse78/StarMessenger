#region "copyright"

/*
    Copyright © 2026 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Diagnostics.CodeAnalysis;
using System.IO;
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Image.ImageAnalysis;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using NINA.StarMessenger.Utils;

namespace NINA.StarMessenger.Processing
{
    [SuppressMessage("Interoperability", "CA1416", Justification = "N.I.N.A. is Windows-only")]
    internal sealed class PreviewImageRenderer : IPreviewImageProvider
    {
        private readonly IImageDataFactory _imageDataFactory;
        private readonly IProfileService _profileService;

        public PreviewImageRenderer(IImageDataFactory imageDataFactory, IProfileService profileService)
        {
            _imageDataFactory = imageDataFactory;
            _profileService = profileService;
        }

        public async Task<string?> CreatePreviewAsync(string imageFilePath, CancellationToken token)
        {
            if (!Settings.Default.Image || string.IsNullOrEmpty(imageFilePath) || !File.Exists(imageFilePath))
            {
                return null;
            }

            try
            {
                var profile = _profileService.ActiveProfile;
                var imageData = await _imageDataFactory.CreateFromFile(imageFilePath, 16, true, RawConverterEnum.FREEIMAGE, token);
                var renderedImage = imageData.RenderImage();

                renderedImage = await renderedImage.Stretch(profile.ImageSettings.AutoStretchFactor,
                    profile.ImageSettings.BlackClipping, profile.ImageSettings.UnlinkedStretch);
                var reducedImage = ImageUtility.Convert16BppTo8Bpp(renderedImage.Image);

                using var resizedImage = Utilities.ResizeImage(reducedImage);
                if (resizedImage == null)
                {
                    return null;
                }

                var previewFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jpg");
                resizedImage.Save(previewFilePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                return File.Exists(previewFilePath) ? previewFilePath : null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Logger.Error("Exception while creating the StarMessage preview image", e);
                return null;
            }
        }
    }
}
