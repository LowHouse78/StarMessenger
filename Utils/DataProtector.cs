#region "copyright"

/*
    Copyright © 2024 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using NINA.Core.Utility;

namespace NINA.StarMessenger.Utils
{
    [SuppressMessage("Interoperability", "CA1416", Justification = "N.I.N.A. is Windows-only")]
    public class DataProtector
    {
        private static readonly DataProtectionScope Scope = DataProtectionScope.CurrentUser;

        public static string EncryptString(string stringToEncrypt)
        {
            try
            {
                if (stringToEncrypt == null)
                    throw new ArgumentNullException(nameof(stringToEncrypt));

                var plainTextBytes = Encoding.Unicode.GetBytes(stringToEncrypt);

                var encryptedBytes = ProtectedData.Protect(plainTextBytes, null, Scope);

                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception e)
            {
                Logger.Error($"Unexpected error during string encryption: {e.Message}", e);
                throw;
            }

        }

        public static string DecryptString(string stringToDecrypt)
        {
            try
            {
                if (stringToDecrypt == null)
                    throw new ArgumentNullException(nameof(stringToDecrypt));

                var encryptedBytes = Convert.FromBase64String(stringToDecrypt);
                var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, Scope);

                return Encoding.Unicode.GetString(decryptedBytes);
            }
            catch (Exception e)
            {
                Logger.Error($"Unexpected error during string decryption: {e.Message}", e);
                return string.Empty;
            }
        }


        public static string ConvertSecureString(SecureString value)
        {
            var valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                var convertedString = Marshal.PtrToStringUni(valuePtr);
                if (convertedString == null)
                {
                    throw new Exception("Exception during processing secure string");
                }
                return convertedString;
            }
            catch (Exception e)
            {
                Logger.Error($"Unexpected error during converting secure string: {e.Message}", e);
                throw;
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
    }
}