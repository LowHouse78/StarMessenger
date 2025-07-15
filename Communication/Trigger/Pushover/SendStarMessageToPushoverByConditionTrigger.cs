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
using System.Windows.Input;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Validations;
using NINA.StarMessenger.Communication.Pushover;
using NINA.StarMessenger.Providers;
using NINA.StarMessenger.SequenceItems.Pushover;
using NINA.StarMessenger.Utils;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using RelayCommand = CommunityToolkit.Mvvm.Input.RelayCommand;

namespace NINA.StarMessenger.Communication.Trigger.Pushover
{
    [ExportMetadata("Name", "StarMessage via Pushover by Condition")]
    [ExportMetadata("Description",
        "Sends a StarMessage via Pushover after a condition is fulfilled")]
    [ExportMetadata("Icon", "StarMessageTriggerIcon")]
    [ExportMetadata("Category", "StarMessenger")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SendStarMessageToPushoverByConditionTrigger : SequenceTrigger, IValidatable
    {
        private readonly char[] _numberOperators = { '<', '>', '=' };
        private IImageHistoryVM _history;
        private IProfileService _profileService;
        private IApplicationStatusMediator _applicationStatusMediator;
        private readonly PushoverClient _pushoverClient;
        private string? _notificationSound;
        private string? _priority;
        private ObservableCollection<string>? _priorityTypes;
        private ObservableCollection<string>? _soundTypes;
        private ObservableCollection<Condition<SendStarMessageToPushoverByConditionTrigger>>? _conditions;
        private StarMessageToPushover _starMessageToPushover;

        [ImportingConstructor]
        public SendStarMessageToPushoverByConditionTrigger(IImageHistoryVM history,
            IProfileService profileService,
            IApplicationStatusMediator applicationStatusMediator)
        {
            Conditions = new ObservableCollection<Condition<SendStarMessageToPushoverByConditionTrigger>>();
            AddConditionCommand = new RelayCommand(() =>
            {
                Conditions.Add(new Condition<SendStarMessageToPushoverByConditionTrigger>());
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
            Conditions.Add(new Condition<SendStarMessageToPushoverByConditionTrigger>());

            _history = history;
            _profileService = profileService;
            _applicationStatusMediator = applicationStatusMediator;
            _pushoverClient = new PushoverClient();
            _soundTypes = new ObservableCollection<string>(Enum.GetNames(typeof(SoundTypes)).ToList());
            _priorityTypes = new ObservableCollection<string>(Enum.GetNames(typeof(PriorityTypes)).ToList());


            _starMessageToPushover = new StarMessageToPushover(applicationStatusMediator);
            TriggerRunner.Add(_starMessageToPushover);

        }

        private SendStarMessageToPushoverByConditionTrigger(SendStarMessageToPushoverByConditionTrigger deepCopy) : this(deepCopy._history,
            deepCopy._profileService, deepCopy._applicationStatusMediator)
        {
            CopyMetaData(deepCopy);
        }

        public ICommand AddConditionCommand { get; set; }
        public ICommand RemoveConditionCommand { get; set; }

        public override object Clone()
        {
            return new SendStarMessageToPushoverByConditionTrigger(this)
            {
                TriggerRunner = (SequentialContainer)TriggerRunner.Clone()
            };
        }

        [JsonProperty]
        public ObservableCollection<Condition<SendStarMessageToPushoverByConditionTrigger>>? Conditions
        {
            get => _conditions;
            set
            {
                _conditions = value;
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
        public IList<string> Issues { get; set; } = new ObservableCollection<string>();

        public override Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress,
            CancellationToken token)
        {
            return Task.Run(async () =>
            {
                try
                {
                    if (await DataProvider.HasCurrentImageAlreadyBeenSent<SendStarMessageToPushoverByConditionTrigger>(true))
                    {
                        return;
                    }
                    if (await ConditionsFulfilled())
                    {
                        foreach (var triggerRunner in TriggerRunner.Items)
                        {
                            if (triggerRunner.GetType() == typeof(StarMessageToPushover))
                            {
                                ((StarMessageToPushover)triggerRunner).TriggerSource = TriggerSourceTypes.ByCondition;
                                ((StarMessageToPushover)triggerRunner).NotificationSound = NotificationSound;
                                ((StarMessageToPushover)triggerRunner).Priority = Priority;
                            }
                        }
                        await TriggerRunner.Run(progress, token);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error($"Unexpected error during executing of {nameof(SendStarMessageToPushoverByConditionTrigger)} Error: {e.Message}", e);
                }

            }, token);
        }

        public override void AfterParentChanged()
        {
            if (Conditions != null)
            {
                for (int i = Conditions.Count - 1; i >= 0; i--)
                {
                    if (Conditions[i].SelectedPropertyForCondition == null &&
                         Conditions.Count > 1)
                    {
                        Conditions.RemoveAt(i);
                    }
                }
            }

            Validate();
        }


        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem)
        {
            if (nextItem is not IExposureItem { ImageType: "LIGHT" })
            {
                return false;
            }

            return _history.ImageHistory.Where(s => s.Type == "LIGHT").ToList().Count > 0;
        }

        public override string ToString()
        {
            return $"Trigger: {nameof(SendStarMessageToPushoverByConditionTrigger)}, After condition fulfilled";
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
                conditionIssues.AddRange(condition.Issues);
                if (condition.Validate())
                {
                    continue;
                }

                validateResult = false;
                Issues = condition.Issues;
            }

            if (!Equals(conditionIssues, Issues))
            {
                Issues = conditionIssues;
                RaisePropertyChanged(nameof(Issues));
            }

            return validateResult;

        }

        //ToDo: make common for SendStarMessageToEmailByConditionTrigger
        private async Task<bool> ConditionsFulfilled()
        {
            var atLeastOneConditionFulfilled = false;
            DataProvider.ResetConditionIsFulfilledInformation<PushoverClient>();
            if (Conditions == null)
            {
                return false;
            }
            foreach (var condition in Conditions)
            {
                if (condition.SelectedPropertyForCondition?.PropertyName != null)
                {
                    if (condition.ValueForCondition == null)
                    {
                        continue;
                    }

                    var isEnabled = condition.SelectedPropertyForCondition?.IsEnabled;
                    if (isEnabled == null || !(bool)isEnabled)
                    {
                        Logger.Info($"Selected property: {condition.SelectedPropertyForCondition?.PropertyUserFriendlyName} for condition is not enabled in configuration.");
                        continue;
                    }

                    var actualValue =
                        await DataProvider.RetrieveSingleImageInformationDataAsync(propertyName: condition.SelectedPropertyForCondition?.PropertyName);

                    var parsingResult = DataParser.TryParseAccordingToDataType(
                        condition.SelectedPropertyForCondition?.DataType, actualValue, parsedValue: condition.ValueForCondition);

                    if (!parsingResult.IsSuccessfullyParsed ||
                        parsingResult.firstValueParsed == null ||
                        parsingResult.secondValueParsed == null)
                    {
                        Logger.Error($"Parsing Error during {nameof(SendStarMessageToPushoverByConditionTrigger)}.{nameof(ConditionsFulfilled)}");
                        continue;
                    }

                    Logger.Debug($"Condition in {nameof(SendStarMessageToPushoverByConditionTrigger)}.{nameof(ConditionsFulfilled)}: " +
                                    $"actualValue = {parsingResult.firstValueParsed} {condition.SelectedOperatorForCondition} conditionValue = {parsingResult.secondValueParsed}");

                    if (IsConditionCheckedByOperatorFullfilled(parsingResult.firstValueParsed, parsingResult.secondValueParsed, condition))
                    {
                        if (IsAimedConsecutiveCountReached(condition))
                        {
                            Logger.Debug($"{nameof(SendStarMessageToPushoverByConditionTrigger)}.{nameof(ConditionsFulfilled)}: Condition set to fulfilled");
                            DataProvider.SetPropertyConditionIsFulfilled<PushoverClient>(condition.SelectedPropertyForCondition?.PropertyName);
                            atLeastOneConditionFulfilled = true;
                        }
                    }
                    else
                    {
                        condition.ConsecutiveCountStatusLevel = ConsecutiveCountStatusLevelType.Ok;
                        condition.CurrentConsecutiveCount = 0;
                    }
                }
            }
            Logger.Debug($"{nameof(SendStarMessageToPushoverByConditionTrigger)}.{nameof(ConditionsFulfilled)} fulfilled: {atLeastOneConditionFulfilled}");
            return atLeastOneConditionFulfilled;
        }

        private static bool IsAimedConsecutiveCountReached(Condition<SendStarMessageToPushoverByConditionTrigger> currentCondition)
        {
            int aimedConsecutiveCount = 1;
            if (!string.IsNullOrWhiteSpace(currentCondition.AimedConsecutiveCount) && int.TryParse(currentCondition.AimedConsecutiveCount, out var parsed))
            {
                aimedConsecutiveCount = parsed;
            }

            currentCondition.CurrentConsecutiveCount++;
            
            //  To avoid stack overflow
            if ((currentCondition.CurrentConsecutiveCount >= int.MaxValue-10) && currentCondition.CurrentConsecutiveCount > aimedConsecutiveCount)
                                {
                {
                    currentCondition.CurrentConsecutiveCount = aimedConsecutiveCount;
                }
            }

            if (currentCondition.CurrentConsecutiveCount < 1)
            {
                currentCondition.ConsecutiveCountStatusLevel = ConsecutiveCountStatusLevelType.Ok;
            }
            else
            if (currentCondition.CurrentConsecutiveCount >= 1 && currentCondition.CurrentConsecutiveCount < aimedConsecutiveCount)
            {
                currentCondition.ConsecutiveCountStatusLevel = ConsecutiveCountStatusLevelType.Warning;
            }
            else
            {
                currentCondition.ConsecutiveCountStatusLevel = ConsecutiveCountStatusLevelType.Error;
            }

            return currentCondition.CurrentConsecutiveCount >= aimedConsecutiveCount;
        }

        private static bool IsConditionCheckedByOperatorFullfilled(object actualValueParsed, object expectedValueParsed, Condition<SendStarMessageToPushoverByConditionTrigger> currentCondition)
        {
            if (currentCondition.SelectedPropertyForCondition == null)
            {
                return false;
            }

            if (currentCondition.SelectedPropertyForCondition.DataType == typeof(string))
            {
                if (currentCondition.SelectedOperatorForCondition[0] == '=')
                {
                    return (string)actualValueParsed == (string)expectedValueParsed;
                }
                return (string)actualValueParsed != (string)expectedValueParsed;
            }

            double? actualValueDouble;
            double? expectedValueDouble;

            if (currentCondition.SelectedPropertyForCondition.DataType == typeof(DateTime))
            {
                actualValueDouble = DataParser.ConvertDateTimeToDouble((DateTime)actualValueParsed);
                expectedValueDouble = DataParser.ConvertDateTimeToDouble((DateTime)expectedValueParsed);
            }
            else
            if (currentCondition.SelectedPropertyForCondition.DataType == typeof(int))
            {
                actualValueDouble = (int)actualValueParsed;
                expectedValueDouble = (int)expectedValueParsed;
            }
            else
            {
                actualValueDouble = (double)actualValueParsed;
                expectedValueDouble = (double)expectedValueParsed;
            }

            {
                switch (currentCondition.SelectedOperatorForCondition)
                {
                    case "<":
                        return Utilities.LessThan((double)actualValueDouble, (double)expectedValueDouble);

                    case ">":
                        return Utilities.GreaterThan((double)actualValueDouble, (double)expectedValueDouble);

                    case "=":
                        return Utilities.EqualTo((double)actualValueDouble, (double)expectedValueDouble);
                }

                return false;
            }
        }
    }
}