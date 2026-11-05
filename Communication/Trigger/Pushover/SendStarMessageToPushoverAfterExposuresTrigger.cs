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
using NINA.Sequencer.Trigger;
using NINA.StarMessenger.Communication.Pushover;
using NINA.StarMessenger.Processing;
using NINA.WPF.Base.Interfaces.ViewModel;

namespace NINA.StarMessenger.Communication.Trigger.Pushover
{
    [ExportMetadata("Name", "StarMessage via Pushover after Exposures")]
    [ExportMetadata("Description", "Sends a StarMessage via Pushover after n Exposures")]
    [ExportMetadata("Icon", "StarMessageTriggerIcon")]
    [ExportMetadata("Category", "StarMessenger")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SendStarMessageToPushoverAfterExposuresTrigger : StarMessageAfterExposuresTriggerBase
    {
        private string? _notificationSound;
        private string? _priority;
        private ObservableCollection<string>? _soundTypes;
        private ObservableCollection<string>? _priorityTypes;

        [ImportingConstructor]
        public SendStarMessageToPushoverAfterExposuresTrigger(IImageHistoryVM history) : base(history)
        {
            _soundTypes = new ObservableCollection<string>(Enum.GetNames(typeof(SoundTypes)).ToList());
            _priorityTypes = new ObservableCollection<string>(Enum.GetNames(typeof(PriorityTypes)).ToList());
        }

        private SendStarMessageToPushoverAfterExposuresTrigger(SendStarMessageToPushoverAfterExposuresTrigger deepCopy) : this(deepCopy.History)
        {
            CopyMetaData(deepCopy);
        }

        public override object Clone()
        {
            return new SendStarMessageToPushoverAfterExposuresTrigger(this)
            {
                AfterExposures = AfterExposures,
                NotificationSound = NotificationSound,
                Priority = Priority
            };
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

        internal override IStarMessageChannel CreateChannel()
        {
            return PushoverClient.Create(NotificationSound, Priority);
        }
    }
}
