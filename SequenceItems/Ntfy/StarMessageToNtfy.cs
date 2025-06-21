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
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Utility;
using NINA.StarMessenger.Communication.Ntfy;

namespace NINA.StarMessenger.SequenceItems.Ntfy
{
    [ExportMetadata("Name", "Sends a StarMessage via NTFY")]
    [ExportMetadata("Description", "Sends a StarMessage via NTFY")]
    [ExportMetadata("Icon", "StarMessageNtfyIcon")]
    [ExportMetadata("Category", "StarMessenger")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class StarMessageToNtfy : SequenceItem, IValidatable
    {
        private readonly NtfyClient _ntfyClient;
        private string? _priority;
        private readonly IApplicationStatusMediator _applicationStatusMediator;
        private TriggerSourceTypes _triggerSource = TriggerSourceTypes.Default;
        private ObservableCollection<string>? _priorityTypesItems;

        [ImportingConstructor]
        public StarMessageToNtfy(IApplicationStatusMediator applicationStatusMediator)
        {
            _ntfyClient = new NtfyClient();
            _applicationStatusMediator = applicationStatusMediator;
            _priorityTypesItems = new ObservableCollection<string>(Enum.GetNames(typeof(PriorityTypes)).ToList());
        }


        public StarMessageToNtfy(StarMessageToNtfy deepCopy) : this(deepCopy._applicationStatusMediator)
        {
            CopyMetaData(deepCopy);
        }

        [JsonIgnore]
        public IList<string> Issues { get; set; } = new ObservableCollection<string>();

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


        [JsonProperty]
        public string? Priority
        {
            get => _priority;
            set
            {
                if (value == null)
                {
                    _priority = Communication.Ntfy.PriorityTypes.Normal.ToString();
                }
                else
                {
                    _priority = value;

                }
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        public ObservableCollection<string>? PriorityTypes
        {
            get => _priorityTypesItems;
            set
            {
                _priorityTypesItems = value;
                RaisePropertyChanged();
            }
        }

        public bool Validate()
        {
            return true;
        }

        public override object Clone()
        {
            return new StarMessageToNtfy(this);
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token)
        {
            return SendStarMessageNtfy(token);
        }

        private async Task<bool> SendStarMessageNtfy(CancellationToken token)
        {
            try
            {
                const int timeoutSendStarMessageNtfyInMilliSeconds = 40000;

                var timeoutTask = Task.Delay(timeoutSendStarMessageNtfyInMilliSeconds, token);

                _applicationStatusMediator.StatusUpdate(new ApplicationStatus()
                { Status = "Send StarMessage", Source = "StarMessenger" });

                var parsedPriority = NtfyPrioritization.GetValueByEnum(Communication.Ntfy.PriorityTypes.Normal);

                if (Enum.TryParse(typeof(PriorityTypes), Priority, true, out var tempPriority))
                {
                    parsedPriority = NtfyPrioritization.GetValueByEnum((PriorityTypes)tempPriority);
                }

                var sendTask = _ntfyClient.SendNtfyMessageAsync(parsedPriority, TriggerSource);

                var completedTask = await Task.WhenAny(sendTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    _ntfyClient.CancelCurrentProcessing();

                    var timeoutMessage =
                        $"SendStarMessageNtfy exceeded the timeout ({timeoutSendStarMessageNtfyInMilliSeconds} ms) period.";

                    throw new TimeoutException(timeoutMessage);
                }

                await sendTask;

            }
            catch (TimeoutException ex)
            {
                _ntfyClient.CancelCurrentProcessing();
                Logger.Error(ex.Message, ex);
                throw;
            }
            catch (TaskCanceledException)
            {
                _ntfyClient.CancelCurrentProcessing();
                Logger.Error($"Timeout when SendStarMessageNtfy");
                throw;
            }
            catch (Exception e)
            {
                Logger.Error($"Unexpected error during SendStarMessageNtfy Error: {e.Message}", e);
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
            return $"Category: StarMessenger, Item: {nameof(StarMessageToNtfy)}";
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