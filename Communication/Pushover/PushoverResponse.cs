#region "copyright"

/*
    Copyright © 2024 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace NINA.StarMessenger.Communication.Pushover
{
    internal class PushoverResponse
    {
        public PushoverResponse(string request)
        {
            Request = request;
        }

        public int Status { get; set; }
        public string Request { get; set; }
    }
}