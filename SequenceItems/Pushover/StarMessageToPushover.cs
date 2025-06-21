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
using NINA.StarMessenger.Communication.Pushover;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Utility;

namespace NINA.StarMessenger.SequenceItems.Pushover
{
    [ExportMetadata("Name", "Sends a StarMessage via Pushover")]
    [ExportMetadata("Description", "Sends a StarMessage via Pushover")]
    [ExportMetadata("Icon", "StarMessagePushoverIcon")]
    [ExportMetadata("Category", "StarMessenger")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class StarMessageToPushover : SequenceItem, IValidatable
    {
        private readonly PushoverClient _pushoverClient;
        private string? _notificationSound = SoundTypes.Pushover.ToString().ToLower();
        private string? _priority;
        private readonly IApplicationStatusMediator _applicationStatusMediator;
        private TriggerSourceTypes _triggerSource = TriggerSourceTypes.Default;
        private ObservableCollection<string>? _priorityTypesItems;
        private ObservableCollection<string>? _soundTypesItems;

        [ImportingConstructor]
        public StarMessageToPushover(IApplicationStatusMediator applicationStatusMediator)
        {
            _pushoverClient = new PushoverClient();
            _applicationStatusMediator = applicationStatusMediator;
            _soundTypesItems = new ObservableCollection<string>(Enum.GetNames(typeof(SoundTypes)).ToList());
            _priorityTypesItems = new ObservableCollection<string>(Enum.GetNames(typeof(PriorityTypes)).ToList());
        }


        public StarMessageToPushover(StarMessageToPushover deepCopy) : this(deepCopy._applicationStatusMediator)
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
        public string? NotificationSound
        {
            get => _notificationSound;
            set
            {
                _notificationSound = value;
                RaisePropertyChanged();
            }
        }


        [JsonIgnore]
        public ObservableCollection<string>? SoundTypesItems
        {
            get => _soundTypesItems;
            set
            {
                _soundTypesItems = value;
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
                    _priority = Communication.Pushover.PriorityTypes.Normal.ToString();
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
            return new StarMessageToPushover(this);
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token)
        {
            return SendStarMessagePushover(token);
        }

        private async Task<bool> SendStarMessagePushover(CancellationToken token)
        {
            try
            {
                const int timeoutSendStarMessagePushoverInMilliSeconds = 40000;
                
                var timeoutTask = Task.Delay(timeoutSendStarMessagePushoverInMilliSeconds, token);

                _applicationStatusMediator.StatusUpdate(new ApplicationStatus()
                { Status = "Send StarMessage", Source = "StarMessenger" });


                var parsedPriority = ((int)Communication.Pushover.PriorityTypes.Normal).ToString();
                var parsedSound = SoundTypes.Pushover.ToString();

                if (Enum.TryParse(typeof(PriorityTypes), Priority, true, out var tempPriority))
                {
                    parsedPriority = ((int)(PriorityTypes)tempPriority).ToString();
                }

                if (Enum.TryParse(typeof(SoundTypes), NotificationSound, true, out var tempSound))
                {
                    parsedSound = tempSound.ToString() ?? SoundTypes.Pushover.ToString();
                }

                var sendTask = _pushoverClient.SendMessageAsync(parsedSound, parsedPriority, TriggerSource, token);

                var completedTask = await Task.WhenAny(sendTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    _pushoverClient.CancelCurrentProcessing();

                    var timeoutMessage =
                        $"SendStarMessagePushover exceeded the timeout ({timeoutSendStarMessagePushoverInMilliSeconds} ms) period.";
                    
                    throw new TimeoutException(timeoutMessage);
                }

                await sendTask; 

            }
            catch (TimeoutException ex)
            {
                _pushoverClient.CancelCurrentProcessing();
                Logger.Error( ex.Message, ex);
                throw;
            }
            catch (TaskCanceledException)
            {
                _pushoverClient.CancelCurrentProcessing();
                Logger.Error($"Timeout when SendStarMessagePushover");
                throw;
            }
            catch (Exception e)
            {
                Logger.Error($"Unexpected error during SendStarMessagePushover Error: {e.Message}", e);
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
            return $"Category: StarMessenger, Item: {nameof(StarMessageToPushover)}";
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