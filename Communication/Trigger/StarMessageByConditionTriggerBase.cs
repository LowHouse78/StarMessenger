#region "copyright"

/*
    Copyright © 2026 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.ObjectModel;
using System.Windows.Input;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Validations;
using NINA.StarMessenger.Processing;
using NINA.StarMessenger.Providers;
using NINA.WPF.Base.Interfaces.ViewModel;
using RelayCommand = CommunityToolkit.Mvvm.Input.RelayCommand;

namespace NINA.StarMessenger.Communication.Trigger
{
    /// <summary>
    /// Sends a StarMessage if at least one condition is fulfilled for a light frame. The trigger only queues the image and
    /// returns immediately. The conditions are evaluated as soon as N.I.N.A. has saved the image.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class StarMessageByConditionTriggerBase<TSelf> : SequenceTrigger, IValidatable
        where TSelf : StarMessageByConditionTriggerBase<TSelf>
    {
        private ObservableCollection<Condition<TSelf>>? _conditions;
        private int? _lastQueuedImageId;

        protected StarMessageByConditionTriggerBase(IImageHistoryVM history)
        {
            History = history;
            Conditions = new ObservableCollection<Condition<TSelf>> { new Condition<TSelf>() };

            AddConditionCommand = new RelayCommand(() =>
            {
                Conditions.Add(new Condition<TSelf>());
                if (Conditions is { Count: > 1 })
                {
                    Conditions[^2].IsOrTextVisible = true;
                }
            });
            RemoveConditionCommand = new RelayCommand(() =>
            {
                if (Conditions is { Count: > 1 })
                {
                    Conditions.RemoveAt(Conditions.Count - 1);
                    Conditions[^1].IsOrTextVisible = false;
                }
            });
        }

        protected IImageHistoryVM History { get; }

        public ICommand AddConditionCommand { get; set; }

        public ICommand RemoveConditionCommand { get; set; }

        [JsonProperty]
        public ObservableCollection<Condition<TSelf>>? Conditions
        {
            get => _conditions;
            set
            {
                _conditions = value;
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        public IList<string> Issues { get; set; } = new ObservableCollection<string>();

        /// <summary>Task of the last queued image. Completes once it has been evaluated and the message sent, skipped or discarded.</summary>
        internal Task<StarMessageJobResult>? LastQueuedMessage { get; private set; }

        internal abstract IStarMessageChannel CreateChannel();

        public override Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token)
        {
            var imageId = StarMessageTriggerHelper.GetLatestLightImageId(History);
            if (imageId == null || imageId == _lastQueuedImageId)
            {
                // Every image is evaluated only once, e.g. when the sequence is restarted
                return Task.CompletedTask;
            }

            _lastQueuedImageId = imageId;
            var conditions = Conditions?.ToList() ?? new List<Condition<TSelf>>();

            LastQueuedMessage = StarMessageTriggerHelper.Enqueue(new StarMessageJob(
                imageId,
                CreateChannel(),
                snapshot => ComposeMessage(conditions, snapshot),
                token));

            return Task.CompletedTask;
        }

        private string? ComposeMessage(IList<Condition<TSelf>> conditions, ImageSnapshot? snapshot)
        {
            if (snapshot == null)
            {
                Logger.Warning($"{GetType().Name}: conditions not evaluated, the image was not saved within {StarMessageTriggerHelper.SaveTimeout.TotalSeconds:0} s.");
                return null;
            }

            var fulfilledProperties = ConditionEvaluator.Evaluate(conditions, snapshot, DataProvider.IsPropertyEnabled);
            Logger.Debug($"{GetType().Name}: conditions fulfilled for image {snapshot.ImageId}: {fulfilledProperties.Count > 0}");
            if (fulfilledProperties.Count == 0)
            {
                return null;
            }

            return StarMessageComposer.Compose(snapshot, DataProvider.GetEnabledProperties(), TriggerSourceTypes.ByCondition, fulfilledProperties);
        }

        public override void AfterParentChanged()
        {
            if (Conditions != null)
            {
                for (var i = Conditions.Count - 1; i >= 0; i--)
                {
                    if (Conditions[i].SelectedPropertyForCondition == null && Conditions.Count > 1)
                    {
                        Conditions.RemoveAt(i);
                    }
                }
            }

            Validate();
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem)
        {
            if (nextItem is not IExposureItem { ImageType: StarMessageTriggerHelper.LightImageType })
            {
                return false;
            }

            return StarMessageTriggerHelper.GetLightImageCount(History) > 0;
        }

        public override string ToString()
        {
            return $"Trigger: {GetType().Name}, After condition fulfilled";
        }

        public bool Validate()
        {
            if (Conditions == null)
            {
                return true;
            }

            var validateResult = true;
            var conditionIssues = new List<string>();
            foreach (var condition in Conditions)
            {
                if (!condition.Validate())
                {
                    validateResult = false;
                }
                conditionIssues.AddRange(condition.Issues);
            }

            if (!conditionIssues.SequenceEqual(Issues))
            {
                Issues = conditionIssues;
                RaisePropertyChanged(nameof(Issues));
            }

            return validateResult;
        }
    }
}
