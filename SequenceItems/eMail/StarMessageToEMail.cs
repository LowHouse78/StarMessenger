#region "copyright"

/*
    Copyright © 2024 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using NINA.StarMessenger.Communication.Email;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Utility;

namespace NINA.StarMessenger.SequenceItems.Email
{
    [ExportMetadata("Name", "Sends a StarMessage via EMail")]
    [ExportMetadata("Description", "Sends a StarMessage via EMail")]
    [ExportMetadata("Icon", "StarMessageEMailIcon")]
    [ExportMetadata("Category", "StarMessenger")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class StarMessageToEMail : SequenceItem, IValidatable
    {
        private readonly EMailClient _eMailClient;
        private readonly IApplicationStatusMediator _applicationStatusMediator;
        private TriggerSourceTypes _triggerSource = TriggerSourceTypes.Default;

        [ImportingConstructor]
        public StarMessageToEMail(IApplicationStatusMediator applicationStatusMediator)
        {
            _eMailClient = new EMailClient();
            _applicationStatusMediator = applicationStatusMediator;
        }

        public StarMessageToEMail(StarMessageToEMail deepCopy) : this(deepCopy._applicationStatusMediator)
        {
            CopyMetaData(deepCopy);
        }

        [JsonIgnore]
        public TriggerSourceTypes TriggerSource
        {
            get => _triggerSource;
            set
            {
                _triggerSource = value;
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        public IList<string> Issues { get; set; } = new ObservableCollection<string>();

        public bool Validate()
        {
            return true;
        }

        public override object Clone()
        {
            return new StarMessageToEMail(this);
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token)
        {
            return SendStarMessageEMail(token);
        }

        private async Task<bool> SendStarMessageEMail(CancellationToken token)
        {
            try
            {
                const int timeoutSendStarMessageEmailInMilliSeconds = 40000;

                var timeoutTask = Task.Delay(timeoutSendStarMessageEmailInMilliSeconds, token);

                _applicationStatusMediator.StatusUpdate(new ApplicationStatus()
                { Status = "Send StarMessage", Source = "StarMessenger" });

                var sendTask = _eMailClient.SendEMailAsync(TriggerSource);

                var completedTask = await Task.WhenAny(sendTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    _eMailClient.CancelCurrentProcessing();

                    var timeoutMessage =
                        $"SendStarMessageEMail exceeded the timeout ({timeoutSendStarMessageEmailInMilliSeconds} ms) period.";

                    throw new TimeoutException(timeoutMessage);
                }

                await sendTask;

            }

            catch (TimeoutException ex)
            {
                _eMailClient.CancelCurrentProcessing();
                Logger.Error(ex.Message, ex);
                throw;
            }
            catch (TaskCanceledException)
            {
                _eMailClient.CancelCurrentProcessing();
                Logger.Error($"Timeout when SendStarMessageEMail");
                throw;
            }
            catch (Exception e)
            {
                Logger.Error($"Unexpected error during SendStarMessageEMail Error: {e.Message}", e);
            }
            finally
            {
                _applicationStatusMediator.StatusUpdate(new ApplicationStatus()
                { Status = string.Empty, Source = "StarMessenger" });
            }

            return true;
        }


        public override string ToString()
        {
            return $"Category: StarMessenger, Item: {nameof(StarMessageToEMail)}";
        }

        public override void AfterParentChanged()
        {
            Validate();
        }
        public override TimeSpan GetEstimatedDuration()
        {
            return TimeSpan.FromSeconds(1);
        }

    }
}