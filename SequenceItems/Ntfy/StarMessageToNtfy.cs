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
using NINA.StarMessenger.Communication.Ntfy;
using NINA.StarMessenger.Processing;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;

namespace NINA.StarMessenger.SequenceItems.Ntfy
{
    [ExportMetadata("Name", "Sends a StarMessage via NTFY")]
    [ExportMetadata("Description", "Sends a StarMessage via NTFY")]
    [ExportMetadata("Icon", "StarMessageNtfyIcon")]
    [ExportMetadata("Category", "StarMessenger")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class StarMessageToNtfy : StarMessageInstructionBase
    {
        private string? _priority;
        private ObservableCollection<string>? _priorityTypesItems;

        [ImportingConstructor]
        public StarMessageToNtfy(IImageHistoryVM history, IApplicationStatusMediator applicationStatusMediator)
            : base(history, applicationStatusMediator)
        {
            _priorityTypesItems = new ObservableCollection<string>(Enum.GetNames(typeof(PriorityTypes)).ToList());
        }

        public StarMessageToNtfy(StarMessageToNtfy deepCopy) : this(deepCopy.History, deepCopy.ApplicationStatusMediator)
        {
            CopyMetaData(deepCopy);
        }

        [JsonProperty]
        public string? Priority
        {
            get => _priority;
            set
            {
                _priority = value ?? Communication.Ntfy.PriorityTypes.Normal.ToString();
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
            return new StarMessageToNtfy(this)
            {
                Priority = Priority,
                WaitForDelivery = WaitForDelivery
            };
        }

        internal override IStarMessageChannel CreateChannel()
        {
            return NtfyClient.Create(Priority);
        }
    }
}
