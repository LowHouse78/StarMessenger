#region "copyright"

/*
    Copyright © 2024 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.ComponentModel;
using System.IO;
using NINA.StarMessenger.Providers;
using NINA.StarMessenger.Utils;
using System.Net.Sockets;
using MailKit.Security;
using AuthenticationException = System.Security.Authentication.AuthenticationException;
using MailKit.Net.Smtp;
using MimeKit;
using NINA.Core.Utility;

namespace NINA.StarMessenger.Communication.Email
{
    internal class EMailClient
    {
        private string _eMailSmtpUsername;
        private string _eMailPassword;
        private string _eMailSmtpPort;
        private string _eMailSmtpHost;
        private string _eMailRecipientAddress;
        private string _eMailSenderAddress;

        public EMailClient()
        {
            _eMailSmtpUsername = Settings.Default.EMailSMTPUsername;
            _eMailPassword = Settings.Default.EMailPassword;
            _eMailSmtpPort = Settings.Default.EMailSMTPPort;
            _eMailSmtpHost = Settings.Default.EMailSMTPHost;
            _eMailRecipientAddress = Settings.Default.EMailRecipientAddress;
            _eMailSenderAddress = Settings.Default.EMailSenderAddress;

            Settings.Default.PropertyChanged += SettingsChanged;
        }

        internal void CancelCurrentProcessing()
        {
            DataProvider.SkipCurrentProcessing();
        }

        internal async Task<bool> SendEMailAsync(TriggerSourceTypes triggerSource, bool testMessage = false)
        {
            return await Task.Run(async () =>
            {
        
                var imageFilePath = string.Empty;
                var smtp = new SmtpClient();
                var timeout = TimeSpan.FromSeconds(45);

                using (var cts = new CancellationTokenSource(timeout))
                {
                    try
                    {
                        string messageContent;
                        if (!testMessage)
                        {
                            messageContent = await DataProvider.RetrieveImageInformationDataAsync<EMailClient>(cts.Token,triggerSource);
                            imageFilePath = await DataProvider.RetrieveImageAsync();
                        }
                        else
                        {
                            messageContent = "Test message from N.I.N.A";
                        }

                        if (string.IsNullOrEmpty(messageContent))
                        {
                            return false;
                        }

                        int.TryParse(_eMailSmtpPort, out var port);

                        await smtp.ConnectAsync(_eMailSmtpHost, port, SecureSocketOptions.Auto, cts.Token);

                        var user = !string.IsNullOrEmpty(_eMailSmtpUsername)
                             ? DataProtector.DecryptString(_eMailSmtpUsername)
                             : string.Empty;

                        var password = !string.IsNullOrEmpty(_eMailPassword)
                            ? DataProtector.DecryptString(_eMailPassword)
                            : string.Empty;

                        if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(password))
                        {
                            await smtp.AuthenticateAsync(user, password, cts.Token);
                        }

                        var message = new MimeMessage();
                        message.From.Add(MailboxAddress.Parse(_eMailSenderAddress));
                        message.To.Add(MailboxAddress.Parse(_eMailRecipientAddress));
                        message.Subject = "N.I.N.A.";

                        var body = new TextPart("plain")
                        {
                            Text = messageContent
                        };

                        var multipart = new Multipart("mixed")
                        {
                            body
                        };

                        if (!string.IsNullOrEmpty(imageFilePath))
                        {
                            var attachment = new MimePart("image", "jpeg")
                            {
                                Content = new MimeContent(File.OpenRead(imageFilePath)),
                                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                                ContentTransferEncoding = ContentEncoding.Base64,
                                FileName = Path.GetFileName(imageFilePath)
                            };

                            multipart.Add(attachment);
                        }

                        message.Body = multipart;

                        await smtp.SendAsync(message, cts.Token);
                        await smtp.DisconnectAsync(true, cts.Token);

                        return true;
                    }
                    catch (SocketException e)
                    {
                        Logger.Error(
                            $"Error while trying to send an EMail. Host: {_eMailSmtpHost} Port:{_eMailSmtpPort}. Error: {e.Message}");
                        return false;
                    }
                    catch (AuthenticationException e)
                    {
                        Logger.Error(
                            $"Error while trying to authenticate on smtp server. Host: {_eMailSmtpHost} Port:{_eMailSmtpPort}. Error: {e.Message}");
                        return false;
                    }
                    catch (Exception e)
                    {
                        Logger.Error(
                            $"Unexpected error during SendEMailAsync. Host: {_eMailSmtpHost} Port:{_eMailSmtpPort}. Error: {e.Message}",
                            e);
                        return false;
                    }
                }
            });
        }
        private void SettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Settings.Default.EMailSMTPUsername):
                    _eMailSmtpUsername = !string.IsNullOrEmpty(Settings.Default.EMailSMTPUsername)
                        ? DataProtector.DecryptString(Settings.Default.EMailSMTPUsername)
                        : string.Empty;
                    break;

                case nameof(Settings.Default.EMailPassword):
                    _eMailPassword = !string.IsNullOrEmpty(Settings.Default.EMailPassword)
                        ? DataProtector.DecryptString(Settings.Default.EMailPassword)
                        : string.Empty;
                    break;

                case nameof(Settings.Default.EMailSMTPPort):
                    _eMailSmtpPort = Settings.Default.EMailSMTPPort;
                    break;

                case nameof(Settings.Default.EMailSMTPHost):
                    _eMailSmtpHost = Settings.Default.EMailSMTPHost;
                    break;

                case nameof(Settings.Default.EMailRecipientAddress):
                    _eMailRecipientAddress = Settings.Default.EMailRecipientAddress;
                    break;

                case nameof(Settings.Default.EMailSenderAddress):
                    _eMailSenderAddress = Settings.Default.EMailSenderAddress;
                    break;
            }
        }
    }
}