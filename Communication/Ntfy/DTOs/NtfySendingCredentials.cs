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
    public class NtfySendingCredentials
    {

        public string Username { get; }
        public string Password { get; }
        public string Token { get; }


        public NtfySendingCredentials(string username,
                                       string password,
                                       string token)
        {
            Username = username;
            Password = password;
            Token = token;
        }
    }
}
