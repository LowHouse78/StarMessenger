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
using NINA.StarMessenger.Communication.Ntfy;
using NINA.StarMessenger.Providers;
using NINA.StarMessenger.SequenceItems.Ntfy;
using NINA.StarMessenger.Utils;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using RelayCommand = CommunityToolkit.Mvvm.Input.RelayCommand;

namespace NINA.StarMessenger.Communication.Trigger.Ntfy
{
    [ExportMetadata("Name", "StarMessage via Ntfy by Condition")]
    [ExportMetadata("Description",
        "Sends a StarMessage via Ntfy after a condition is fulfilled")]
    [ExportMetadata("Icon", "StarMessageNtfyIcon")]
    [ExportMetadata("Category", "StarMessenger")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SendStarMessageToNtfyByConditionTrigger : SequenceTrigger, IValidatable
    {
        private readonly char[] _numberOperators = { '<', '>', '=' };
        private IImageHistoryVM _history;
        private IProfileService _profileService;
        private IApplicationStatusMediator _applicationStatusMediator;
        private readonly NtfyClient _ntfyClient;
        private string? _notificationSound;
        private string? _priority;
        private ObservableCollection<string>? _priorityTypes;
        private ObservableCollection<string>? _soundTypes;
        private ObservableCollection<Condition<SendStarMessageToNtfyByConditionTrigger>>? _conditions;
        private StarMessageToNtfy _starMessageToNtfy;

        [ImportingConstructor]
        public SendStarMessageToNtfyByConditionTrigger(IImageHistoryVM history,
            IProfileService profileService,
            IApplicationStatusMediator applicationStatusMediator)
        {
            Conditions = new ObservableCollection<Condition<SendStarMessageToNtfyByConditionTrigger>>();
            AddConditionCommand = new RelayCommand(() =>
            {
                Conditions.Add(new Condition<SendStarMessageToNtfyByConditionTrigger>());
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
            Conditions.Add(new Condition<SendStarMessageToNtfyByConditionTrigger>());

            _history = history;
            _profileService = profileService;
            _applicationStatusMediator = applicationStatusMediator;
            _ntfyClient = new NtfyClient();
            _priorityTypes = new ObservableCollection<string>(Enum.GetNames(typeof(PriorityTypes)).ToList());


            _starMessageToNtfy = new StarMessageToNtfy(applicationStatusMediator);
            TriggerRunner.Add(_starMessageToNtfy);

        }

        private SendStarMessageToNtfyByConditionTrigger(SendStarMessageToNtfyByConditionTrigger deepCopy) : this(deepCopy._history,
            deepCopy._profileService, deepCopy._applicationStatusMediator)
        {
            CopyMetaData(deepCopy);
        }

        public ICommand AddConditionCommand { get; set; }
        public ICommand RemoveConditionCommand { get; set; }

        public override object Clone()
        {
            return new SendStarMessageToNtfyByConditionTrigger(this)
            {
                TriggerRunner = (SequentialContainer)TriggerRunner.Clone()
            };
        }

        [JsonProperty]
        public ObservableCollection<Condition<SendStarMessageToNtfyByConditionTrigger>>? Conditions
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
                    if (await DataProvider.HasCurrentImageAlreadyBeenSent<SendStarMessageToNtfyByConditionTrigger>(true))
                    {
                        return;
                    }
                    if (await ConditionsFulfilled())
                    {
                        foreach (var triggerRunner in TriggerRunner.Items)
                        {
                            if (triggerRunner.GetType() == typeof(StarMessageToNtfy))
                            {
                                ((StarMessageToNtfy)triggerRunner).TriggerSource = TriggerSourceTypes.ByCondition;
                                ((StarMessageToNtfy)triggerRunner).Priority = Priority;
                            }
                        }
                        await TriggerRunner.Run(progress, token);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error($"Unexpected error during executing of {nameof(SendStarMessageToNtfyByConditionTrigger)} Error: {e.Message}", e);
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
            return $"Trigger: {nameof(SendStarMessageToNtfyByConditionTrigger)}, After condition fulfilled";
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
            DataProvider.ResetConditionIsFulfilledInformation<NtfyClient>();
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
                        Logger.Error($"Parsing Error during {nameof(SendStarMessageToNtfyByConditionTrigger)}.{nameof(ConditionsFulfilled)}");
                        continue;
                    }

                    Logger.Debug($"Condition in {nameof(SendStarMessageToNtfyByConditionTrigger)}.{nameof(ConditionsFulfilled)}: " +
                                    $"actualValue = {parsingResult.firstValueParsed} {condition.SelectedOperatorForCondition} conditionValue = {parsingResult.secondValueParsed}");

                    if (CheckConditionByOperator(parsingResult.firstValueParsed, parsingResult.secondValueParsed, condition))
                    {
                        Logger.Debug($"{nameof(SendStarMessageToNtfyByConditionTrigger)}.{nameof(ConditionsFulfilled)}: Condition set to fulfilled");
                        DataProvider.SetPropertyConditionIsFulfilled<NtfyClient>(condition.SelectedPropertyForCondition?.PropertyName);
                        atLeastOneConditionFulfilled = true;
                    }
                }
            }
            Logger.Debug($"{nameof(SendStarMessageToNtfyByConditionTrigger)}.{nameof(ConditionsFulfilled)} fulfilled: {atLeastOneConditionFulfilled}");
            return atLeastOneConditionFulfilled;
        }


        private static bool CheckConditionByOperator(object actualValueParsed, object expectedValueParsed, Condition<SendStarMessageToNtfyByConditionTrigger> currentCondition)
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