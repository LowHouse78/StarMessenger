#region "copyright"

/*
    Copyright © 2024 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.IO;
using NINA.StarMessenger.Processing;
using NINA.StarMessenger.Utils;
using System.Net.Sockets;
using MailKit.Security;
using AuthenticationException = System.Security.Authentication.AuthenticationException;
using MailKit.Net.Smtp;
using MimeKit;
using NINA.Core.Utility;

namespace NINA.StarMessenger.Communication.Email
{
    internal sealed class EMailClient : IStarMessageChannel
    {
        public string Name => "Email";

        public async Task<bool> SendAsync(string message, string? imageFilePath, CancellationToken token)
        {
            if (string.IsNullOrEmpty(message))
            {
                return false;
            }

            // Settings are read on every send, so changes in the options apply immediately. Credentials are decrypted exactly once here.
            var smtpHost = Settings.Default.EMailSMTPHost;
            var smtpPort = Settings.Default.EMailSMTPPort;

            try
            {
                int.TryParse(smtpPort, out var port);

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(smtpHost, port, SecureSocketOptions.Auto, token);

                var user = DataProtector.DecryptSetting(Settings.Default.EMailSMTPUsername);
                var password = DataProtector.DecryptSetting(Settings.Default.EMailPassword);
                if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(password))
                {
                    await smtp.AuthenticateAsync(user, password, token);
                }

                using var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(MailboxAddress.Parse(Settings.Default.EMailSenderAddress));
                mimeMessage.To.Add(MailboxAddress.Parse(Settings.Default.EMailRecipientAddress));
                mimeMessage.Subject = "N.I.N.A.";

                var multipart = new Multipart("mixed")
                {
                    new TextPart("plain") { Text = message }
                };

                if (!string.IsNullOrEmpty(imageFilePath) && File.Exists(imageFilePath))
                {
                    // Read into memory, so the temporary file is not locked and can be deleted after sending.
                    var imageStream = new MemoryStream(await File.ReadAllBytesAsync(imageFilePath, token));
                    multipart.Add(new MimePart("image", "jpeg")
                    {
                        Content = new MimeContent(imageStream),
                        ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                        ContentTransferEncoding = ContentEncoding.Base64,
                        FileName = Path.GetFileName(imageFilePath)
                    });
                }

                mimeMessage.Body = multipart;

                await smtp.SendAsync(mimeMessage, token);
                await smtp.DisconnectAsync(true, token);

                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SocketException e)
            {
                Logger.Error($"Error while trying to send an EMail. Host: {smtpHost} Port:{smtpPort}. Error: {e.Message}");
            }
            catch (AuthenticationException e)
            {
                Logger.Error($"Error while trying to authenticate on smtp server. Host: {smtpHost} Port:{smtpPort}. Error: {e.Message}");
            }
            catch (Exception e)
            {
                Logger.Error($"Unexpected error during SendEMailAsync. Host: {smtpHost} Port:{smtpPort}. Error: {e.Message}", e);
            }

            return false;
        }
    }
}
