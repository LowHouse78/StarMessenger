#region "copyright"

/*
    Copyright © 2024 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using NINA.Core.Utility;
using NINA.Sequencer.Validations;
using NINA.StarMessenger.Providers;

namespace NINA.StarMessenger.Communication.Trigger;

public class Condition<T> : INotifyPropertyChanged, IValidatable
{
    private readonly string[] _textOperators = { "=", "\u2260" };
    private readonly string[] _numberOperators = { "<", ">", "=" };
    private bool _isOrTextVisible;
    private string _selectedOperatorForCondition;
    private PropertyItem? _selectedPropertyForCondition;
    private string? _valueForCondition;
    private ObservableCollection<string>? _operatorsForCondition;
    private ObservableCollection<PropertyItem> _propertiesForCondition;
    private readonly Dictionary<Type, IList<string>> _issuesPerType = new Dictionary<Type, IList<string>>();

    public Condition()
    {
        InitializeCondition();
    }

    private void InitializeCondition()
    {
        Settings.Default.PropertyChanged += SettingsChanged;
        _operatorsForCondition = new ObservableCollection<string>(_numberOperators);
        _propertiesForCondition = new ObservableCollection<PropertyItem>();

        foreach (var property in DataProvider.GetProperties().OrderBy(item => item.PropertyUserFriendlyName))
        {
            _propertiesForCondition.Add(new PropertyItem(property));
        }
    }

    public bool IsOrTextVisible
    {
        get => _isOrTextVisible;
        set
        {
            _isOrTextVisible = value;
            RaisePropertyChanged();
        }
    }

    public string SelectedOperatorForCondition
    {
        get => _selectedOperatorForCondition;
        set
        {
            _selectedOperatorForCondition = value;
            RaisePropertyChanged();
        }
    }


    [JsonIgnore]
    public ObservableCollection<string>? OperatorsForCondition
    {
        get => _operatorsForCondition;
        set
        {
            _operatorsForCondition = value;
            RaisePropertyChanged();
        }
    }

    [JsonIgnore]
    public ObservableCollection<PropertyItem> PropertiesForCondition
    {
        get => _propertiesForCondition;
        set
        {
            _propertiesForCondition = value;
            RaisePropertyChanged();
        }
    }

    public PropertyItem? SelectedPropertyForCondition
    {
        get => _selectedPropertyForCondition;
        set
        {
            if (_selectedPropertyForCondition != value)
            {
                if (value != null)
                {
                    ValueForCondition = GetDatatypeTemplate(value.DataType);
                    GetOperatorsForDatatype(value.DataType, out var newOperators);
                    OperatorsForCondition = newOperators;

                    var prop = PropertiesForCondition.First(s => s.PropertyName == value.PropertyName);
                    _selectedPropertyForCondition = prop;
                }
                RaisePropertyChanged();
            }
        }
    }

    public string? ValueForCondition
    {
        get => _valueForCondition;
        set
        {
            _valueForCondition = value;
            RaisePropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public bool Validate()
    {
        try
        {
            ClearIssues();
            if (string.IsNullOrEmpty(SelectedPropertyForCondition?.PropertyUserFriendlyName) ||
                string.IsNullOrWhiteSpace(SelectedPropertyForCondition?.PropertyUserFriendlyName))
            {
                AddIssue("Parameter for condition is missing");
            }

            if (string.IsNullOrEmpty(SelectedOperatorForCondition) ||
                string.IsNullOrWhiteSpace(SelectedOperatorForCondition))
            {
                AddIssue("Operator for condition is missing");
            }

            if (SelectedPropertyForCondition is { IsEnabled: false })
            {
                AddIssue("Selected parameter is not enabled in configuration!");
            }

            var validateValue = ValidateValue();
            if (!string.IsNullOrEmpty(validateValue))
            {
                AddIssue(validateValue);
            }

            RaisePropertyChanged(nameof(Issues));

            return GetIssueCount() == 0;
        }
        catch (Exception e)
        {
            Logger.Error($"Unexpected error while trying to validate a condition: {e.Message}", e);
        }

        return false;
    }

    public IList<string> Issues
    {
        get
        {
            if (_issuesPerType.ContainsKey(typeof(T)))
            {
                return _issuesPerType[typeof(T)];
            }

            return new List<string>();
        }
        set => _issuesPerType[typeof(T)] = value;
    }

    private string GetDatatypeTemplate(Type dataType)
    {
        if (dataType == typeof(double) || dataType == typeof(int))
        {
            return "0";
        }

        if (dataType == typeof(string))
        {
            return "Filter 1";
        }

        if (dataType == typeof(DateTime))
        {
            return DateTime.Now.ToString(CultureInfo.CurrentCulture);
        }

        return string.Empty;
    }

    private void GetOperatorsForDatatype(Type dataType, out ObservableCollection<string> operators)
    {
        operators = dataType == typeof(string)
            ? new ObservableCollection<string>(_textOperators)
            : new ObservableCollection<string>(_numberOperators);
    }

    private string ValidateValue()
    {
        if (SelectedPropertyForCondition == null)
        {
            return string.Empty;
        }

        if (SelectedPropertyForCondition.DataType == typeof(double) ||
            SelectedPropertyForCondition.DataType == typeof(int))
        {
            var culture = CultureInfo.CurrentCulture;
            var decimalSeparator = culture.NumberFormat.NumberDecimalSeparator[0];

            if (ValueForCondition is not { } _)
            {
                return "Unexpected value format.";
            }

            if (ValueForCondition is { } s && s.Any(c => !char.IsDigit(c) && c != decimalSeparator))
            {
                return "Value is not a valid decimal number.";
            }

            return string.Empty;
        }

        if (SelectedPropertyForCondition.DataType == typeof(DateTime))
        {
            if (!DateTime.TryParse(ValueForCondition, out _))
            {
                var pattern = DateTime.Now.ToString(CultureInfo.CurrentCulture);
                return $"Value has not a valid date time format: e.g. {pattern}";
            }
        }

        return string.Empty;
    }

    private void SettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        try
        {
            if (sender == null || e.PropertyName == null || e.PropertyName == "Image")
            {
                return;
            }
            var settingsType = sender.GetType();
            var settingsPropertyInfo = settingsType.GetProperty(e.PropertyName);
            var settingsValue = settingsPropertyInfo?.GetValue(sender);
            if (settingsValue is bool newValue)
            {
                var appliedProperty = _selectedPropertyForCondition;
                var appliedPropertyType = appliedProperty?.GetType();
                var appliedPropertyInfo = appliedPropertyType?.GetProperty(nameof(PropertyItem.IsEnabled));
                appliedPropertyInfo?.SetValue(appliedProperty, newValue);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error while trying to change the settings: {ex.Message}", ex);
        }
    }

    private void AddIssue(string newIssue)
    {
        if (!_issuesPerType.ContainsKey(typeof(T)))
        {
            _issuesPerType[typeof(T)] = new List<string> { newIssue };
        }
        else
        {
            _issuesPerType[typeof(T)].Add(newIssue);
        }
    }

    private void ClearIssues()
    {
        if (_issuesPerType.ContainsKey(typeof(T)))
        {
            _issuesPerType[typeof(T)].Clear();
        }
    }

    private int GetIssueCount()
    {
        return !_issuesPerType.ContainsKey(typeof(T)) ? 0 : _issuesPerType[typeof(T)].Count();
    }
}