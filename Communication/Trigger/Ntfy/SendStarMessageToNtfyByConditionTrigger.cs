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
using NINA.StarMessenger.Communication.Ntfy;
using NINA.StarMessenger.Processing;
using NINA.WPF.Base.Interfaces.ViewModel;

namespace NINA.StarMessenger.Communication.Trigger.Ntfy
{
    [ExportMetadata("Name", "StarMessage via Ntfy by Condition")]
    [ExportMetadata("Description",
        "Sends a StarMessage via Ntfy after a condition is fulfilled")]
    [ExportMetadata("Icon", "StarMessageNtfyIcon")]
    [ExportMetadata("Category", "StarMessenger")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SendStarMessageToNtfyByConditionTrigger : StarMessageByConditionTriggerBase<SendStarMessageToNtfyByConditionTrigger>
    {
        private string? _priority;
        private ObservableCollection<string>? _priorityTypes;

        [ImportingConstructor]
        public SendStarMessageToNtfyByConditionTrigger(IImageHistoryVM history) : base(history)
        {
            _priorityTypes = new ObservableCollection<string>(Enum.GetNames(typeof(PriorityTypes)).ToList());
        }

        private SendStarMessageToNtfyByConditionTrigger(SendStarMessageToNtfyByConditionTrigger deepCopy) : this(deepCopy.History)
        {
            CopyMetaData(deepCopy);
        }

        public override object Clone()
        {
            return new SendStarMessageToNtfyByConditionTrigger(this)
            {
                Priority = Priority
            };
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
            return NtfyClient.Create(Priority);
        }
    }
}
