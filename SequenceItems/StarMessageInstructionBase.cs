#region "copyright"

/*
    Copyright © 2026 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.ObjectModel;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using NINA.StarMessenger.Communication.Trigger;
using NINA.StarMessenger.Processing;
using NINA.StarMessenger.Providers;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;

namespace NINA.StarMessenger.SequenceItems
{
    /// <summary>
    /// Sends a StarMessage about the latest light frame. By default the instruction only queues the message and the
    /// sequence continues immediately. With <see cref="WaitForDelivery"/> it waits until the message has been sent.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class StarMessageInstructionBase : SequenceItem, IValidatable
    {
        private bool _waitForDelivery;

        protected StarMessageInstructionBase(IImageHistoryVM history, IApplicationStatusMediator applicationStatusMediator)
        {
            History = history;
            ApplicationStatusMediator = applicationStatusMediator;
        }

        protected IImageHistoryVM History { get; }

        protected IApplicationStatusMediator ApplicationStatusMediator { get; }

        [JsonProperty]
        public bool WaitForDelivery
        {
            get => _waitForDelivery;
            set
            {
                _waitForDelivery = value;
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        public IList<string> Issues { get; set; } = new ObservableCollection<string>();

        /// <summary>Task of the last queued message. Completes once it has been sent, skipped or discarded.</summary>
        internal Task<StarMessageJobResult>? LastQueuedMessage { get; private set; }

        internal abstract IStarMessageChannel CreateChannel();

        public bool Validate()
        {
            return true;
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token)
        {
            var imageId = StarMessageTriggerHelper.GetLatestLightImageId(History);
            var saveTimeout = StarMessageTriggerHelper.SaveTimeout;
            var channel = CreateChannel();

            var queuedMessage = StarMessageTriggerHelper.Enqueue(new StarMessageJob(
                imageId,
                channel,
                snapshot => snapshot == null
                    ? StarMessageComposer.ComposeWithoutImage(TriggerSourceTypes.Default, imageId == null ? null : saveTimeout)
                    : StarMessageComposer.Compose(snapshot, DataProvider.GetEnabledProperties(), TriggerSourceTypes.Default),
                token));
            LastQueuedMessage = queuedMessage;

            if (!WaitForDelivery)
            {
                return;
            }

            try
            {
                ApplicationStatusMediator.StatusUpdate(new ApplicationStatus { Status = "Send StarMessage", Source = "StarMessenger" });

                var result = await queuedMessage.WaitAsync(token);
                if (result == StarMessageJobResult.Failed)
                {
                    throw new InvalidOperationException($"StarMessage via {channel.Name} could not be sent.");
                }
            }
            finally
            {
                ApplicationStatusMediator.StatusUpdate(new ApplicationStatus { Status = string.Empty, Source = "StarMessenger" });
            }
        }

        public override void AfterParentChanged()
        {
            Validate();
        }

        public override TimeSpan GetEstimatedDuration()
        {
            return TimeSpan.FromSeconds(1);
        }

        public override string ToString()
        {
            return $"Category: StarMessenger, Item: {GetType().Name}";
        }
    }
}
