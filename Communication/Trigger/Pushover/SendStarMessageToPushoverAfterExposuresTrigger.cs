#region "copyright"

/*
    Copyright © 2024 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Validations;
using NINA.StarMessenger.Communication.Pushover;
using NINA.StarMessenger.SequenceItems.Pushover;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;

namespace NINA.StarMessenger.Communication.Trigger.Pushover
{
    [ExportMetadata("Name", "StarMessage via Pushover after Exposures")]
    [ExportMetadata("Description", "Sends a StarMessage via Pushover after n Exposures")]
    [ExportMetadata("Icon", "StarMessageTriggerIcon")]
    [ExportMetadata("Category", "StarMessenger")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SendStarMessageToPushoverAfterExposuresTrigger : SequenceTrigger, IValidatable
    {
        private IImageHistoryVM _history;
        private IProfileService _profileService;
        private IApplicationStatusMediator _applicationStatusMediator;
        private readonly PushoverClient _pushoverClient;
        private int _lastTriggerId;
        private int _afterExposures;
        private string? _notificationSound;
        private string? _priority;
        private int _progressExposures;
        private ObservableCollection<string>? _soundTypes;
        private ObservableCollection<string>? _priorityTypes;
        private IList<string> _issues = new List<string>();
        private StarMessageToPushover _starMessageToPushover;


        [ImportingConstructor]
        public SendStarMessageToPushoverAfterExposuresTrigger(IImageHistoryVM history,
                                      IProfileService profileService,
                                      IApplicationStatusMediator applicationStatusMediator)
        {
            _history = history;
            _profileService = profileService;
            _applicationStatusMediator = applicationStatusMediator;
            AfterExposures = 1;
            _soundTypes = new ObservableCollection<string>(Enum.GetNames(typeof(SoundTypes)).ToList());
            _priorityTypes = new ObservableCollection<string>(Enum.GetNames(typeof(PriorityTypes)).ToList());
            _pushoverClient = new PushoverClient();

            _starMessageToPushover = new StarMessageToPushover(applicationStatusMediator);
            TriggerRunner.Add(_starMessageToPushover);
        }

  
        public override object Clone()
        {
            return new SendStarMessageToPushoverAfterExposuresTrigger(this)
            {
                AfterExposures = AfterExposures,
                TriggerRunner = (SequentialContainer)TriggerRunner.Clone()
            };
        }

        [JsonProperty]
        public int AfterExposures
        {
            get => _afterExposures;
            set
            {
                _afterExposures = value;
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
        public ObservableCollection<string>? SoundTypes
        {
            get => _soundTypes;
            set
            {
                _soundTypes = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public string? Priority
        {
            get => _priority;
            set
            {
                _priority = value;
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        public ObservableCollection<string>? PriorityTypes
        {
            get => _priorityTypes;
            set
            {
                _priorityTypes = value;
                RaisePropertyChanged();
            }
        }


        [JsonIgnore]
        public int ProgressExposures => AfterExposures > 0 ? _history.ImageHistory.Where(s => s.Type == "LIGHT").ToList().Count % AfterExposures : 0;

        [JsonIgnore]
        public IList<string> Issues
        {
            get => _issues;
            set
            {
                _issues = ImmutableList.CreateRange(value);
                RaisePropertyChanged();
            }
        }

        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token)
        {
            if (AfterExposures > 0)
            {
                _lastTriggerId = _history.ImageHistory.Where(s => s.Type == "LIGHT").ToList().Count;
                
                foreach (var triggerRunner in TriggerRunner.Items)
                {
                    if (triggerRunner.GetType() == typeof(StarMessageToPushover))
                    {
                        ((StarMessageToPushover)triggerRunner).TriggerSource = TriggerSourceTypes.ByExposures;
                        ((StarMessageToPushover)triggerRunner).NotificationSound = NotificationSound;
                        ((StarMessageToPushover)triggerRunner).Priority = Priority;
                    }
                }
                await TriggerRunner.Run(progress, token);
            }
        }

        public override void AfterParentChanged()
        {
            Validate();
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem)
        {
            if (nextItem is not IExposureItem { ImageType: "LIGHT" })
            {
                return false;
            }

            RaisePropertyChanged(nameof(ProgressExposures));
            if (_lastTriggerId > _history.ImageHistory.Where(s => s.Type == "LIGHT").ToList().Count)
            {
                _lastTriggerId = 0;
            }
            return _lastTriggerId < _history.ImageHistory.Where(s => s.Type == "LIGHT").ToList().Count && _history.ImageHistory.Where(s => s.Type == "LIGHT").ToList().Count > 0 && ProgressExposures == 0;
        }

        public override string ToString()
        {
            return $"Trigger: {nameof(SendStarMessageToPushoverAfterExposuresTrigger)}, After Exposures: {AfterExposures}";
        }

        public bool Validate()
        {
            Issues = new List<string>();
            if (!int.TryParse(AfterExposures.ToString(), out var _))
            {
                Issues.Add("Value is not a valid integer.");
            }

            return !Issues.Any();
        }

        private SendStarMessageToPushoverAfterExposuresTrigger(SendStarMessageToPushoverAfterExposuresTrigger deepCopy) : this(deepCopy._history, deepCopy._profileService, deepCopy._applicationStatusMediator)
        {
            CopyMetaData(deepCopy);
        }
    }
}