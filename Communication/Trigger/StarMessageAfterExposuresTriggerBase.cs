#region "copyright"

/*
    Copyright © 2026 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.Immutable;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Validations;
using NINA.StarMessenger.Processing;
using NINA.StarMessenger.Providers;
using NINA.WPF.Base.Interfaces.ViewModel;

namespace NINA.StarMessenger.Communication.Trigger
{
    /// <summary>
    /// Sends a StarMessage after every n light frames. The trigger only queues the message and returns immediately,
    /// the message is sent as soon as N.I.N.A. has saved the image.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class StarMessageAfterExposuresTriggerBase : SequenceTrigger, IValidatable
    {
        private int _lastTriggerId;
        private int _afterExposures;
        private IList<string> _issues = new List<string>();

        protected StarMessageAfterExposuresTriggerBase(IImageHistoryVM history)
        {
            History = history;
            AfterExposures = 1;
        }

        protected IImageHistoryVM History { get; }

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
        public int ProgressExposures => AfterExposures > 0 ? StarMessageTriggerHelper.GetLightImageCount(History) % AfterExposures : 0;

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

        /// <summary>Task of the last queued message. Completes once it has been sent, skipped or discarded.</summary>
        internal Task<StarMessageJobResult>? LastQueuedMessage { get; private set; }

        internal abstract IStarMessageChannel CreateChannel();

        public override Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token)
        {
            if (AfterExposures <= 0)
            {
                return Task.CompletedTask;
            }

            _lastTriggerId = StarMessageTriggerHelper.GetLightImageCount(History);
            var imageId = StarMessageTriggerHelper.GetLatestLightImageId(History);
            var saveTimeout = StarMessageTriggerHelper.SaveTimeout;

            LastQueuedMessage = StarMessageTriggerHelper.Enqueue(new StarMessageJob(
                imageId,
                CreateChannel(),
                snapshot => snapshot == null
                    ? StarMessageComposer.ComposeWithoutImage(TriggerSourceTypes.ByExposures, imageId == null ? null : saveTimeout)
                    : StarMessageComposer.Compose(snapshot, DataProvider.GetEnabledProperties(), TriggerSourceTypes.ByExposures),
                token));

            return Task.CompletedTask;
        }

        public override void AfterParentChanged()
        {
            Validate();
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem)
        {
            if (nextItem is not IExposureItem { ImageType: StarMessageTriggerHelper.LightImageType })
            {
                return false;
            }

            RaisePropertyChanged(nameof(ProgressExposures));
            var lightImageCount = StarMessageTriggerHelper.GetLightImageCount(History);
            if (_lastTriggerId > lightImageCount)
            {
                _lastTriggerId = 0;
            }

            return _lastTriggerId < lightImageCount && lightImageCount > 0 && ProgressExposures == 0;
        }

        public override string ToString()
        {
            return $"Trigger: {GetType().Name}, After Exposures: {AfterExposures}";
        }

        public bool Validate()
        {
            Issues = new List<string>();
            return true;
        }
    }
}
