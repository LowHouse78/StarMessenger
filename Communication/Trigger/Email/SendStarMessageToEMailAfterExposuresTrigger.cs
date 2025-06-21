#region "copyright"

/*
    Copyright © 2024 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Validations;
using NINA.StarMessenger.SequenceItems.Email;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;

namespace NINA.StarMessenger.Communication.Trigger.Email
{
    [ExportMetadata("Name", "StarMessage via EMail after Exposures")]
    [ExportMetadata("Description", "Sends a StarMessage via EMail after n Exposures")]
    [ExportMetadata("Icon", "StarMessageEMailIcon")]
    [ExportMetadata("Category", "StarMessenger")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SendStarMessageToEMailAfterExposuresTrigger : SequenceTrigger, IValidatable
    {
        private IImageHistoryVM _history;
        private IProfileService _profileService;
        private IApplicationStatusMediator _applicationStatusMediator;
        private int _lastTriggerId;
        private int _afterExposures;
        private int _progressExposures;
        private IList<string> _issues = new List<string>();


        [ImportingConstructor]
        public SendStarMessageToEMailAfterExposuresTrigger(IImageHistoryVM history,
                                      IProfileService profileService,
                                      IApplicationStatusMediator applicationStatusMediator)
        {
            _history = history;
            _profileService = profileService;
            _applicationStatusMediator = applicationStatusMediator;
            AfterExposures = 1;
            TriggerRunner.Add(new StarMessageToEMail(applicationStatusMediator));
        }

        public override object Clone()
        {
            return new SendStarMessageToEMailAfterExposuresTrigger(this)
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
                    if (triggerRunner.GetType() == typeof(StarMessageToEMail))
                    {
                        ((StarMessageToEMail)triggerRunner).TriggerSource = TriggerSourceTypes.ByExposures;
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
            return $"Trigger: {nameof(SendStarMessageToEMailAfterExposuresTrigger)}, After Exposures: {AfterExposures}";
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

        private SendStarMessageToEMailAfterExposuresTrigger(SendStarMessageToEMailAfterExposuresTrigger deepCopy) : this(deepCopy._history, deepCopy._profileService, deepCopy._applicationStatusMediator)
        {
            CopyMetaData(deepCopy);
        }
    }
}