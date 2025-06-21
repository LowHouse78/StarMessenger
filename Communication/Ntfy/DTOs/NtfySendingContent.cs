#region "copyright"

/*
    Copyright © 2024 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion


namespace NINA.StarMessenger.Communication.Ntfy.DTOs
{
    public class NtfySendingContent
    {

        public string Title { get; }
        public string Message { get; }
        public string ImagePath { get; }
        public string Priority { get; }


        public NtfySendingContent(string title,
                                       string message,
                                       string imagePath,
                                       string priority)
        {
            Title = title;
            Message = message;
            ImagePath = imagePath;
            Priority = priority;
        }
    }
}
