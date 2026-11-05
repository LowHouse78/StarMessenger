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
using NINA.Sequencer.SequenceItem;
using NINA.StarMessenger.Communication.Pushover;
using NINA.StarMessenger.Processing;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;

namespace NINA.StarMessenger.SequenceItems.Pushover
{
    [ExportMetadata("Name", "Sends a StarMessage via Pushover")]
    [ExportMetadata("Description", "Sends a StarMessage via Pushover")]
    [ExportMetadata("Icon", "StarMessagePushoverIcon")]
    [ExportMetadata("Category", "StarMessenger")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class StarMessageToPushover : StarMessageInstructionBase
    {
        private string? _notificationSound = SoundTypes.Pushover.ToString().ToLower();
        private string? _priority;
        private ObservableCollection<string>? _priorityTypesItems;
        private ObservableCollection<string>? _soundTypesItems;

        [ImportingConstructor]
        public StarMessageToPushover(IImageHistoryVM history, IApplicationStatusMediator applicationStatusMediator)
            : base(history, applicationStatusMediator)
        {
            _soundTypesItems = new ObservableCollection<string>(Enum.GetNames(typeof(SoundTypes)).ToList());
            _priorityTypesItems = new ObservableCollection<string>(Enum.GetNames(typeof(PriorityTypes)).ToList());
        }

        public StarMessageToPushover(StarMessageToPushover deepCopy) : this(deepCopy.History, deepCopy.ApplicationStatusMediator)
        {
            CopyMetaData(deepCopy);
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
                _priority = value ?? Communication.Pushover.PriorityTypes.Normal.ToString();
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

        public override object Clone()
        {
            return new StarMessageToPushover(this)
            {
                NotificationSound = NotificationSound,
                Priority = Priority,
                WaitForDelivery = WaitForDelivery
            };
        }

        internal override IStarMessageChannel CreateChannel()
        {
            return PushoverClient.Create(NotificationSound, Priority);
        }
    }
}
