using Newtonsoft.Json;

namespace NINA.StarMessenger.Providers
{
    public class PropertyItem
    {
        public PropertyItem(PropertyItem deepCopy)
        {
            PropertyName = deepCopy.PropertyName;
            PropertyUserFriendlyName = deepCopy.PropertyUserFriendlyName;
            IsEnabled = deepCopy.IsEnabled;
            GetCurrentValueFunc = deepCopy.GetCurrentValueFunc;
            DataType = deepCopy.DataType;
            ConditionFulfilled = deepCopy.ConditionFulfilled;
        }

        [JsonConstructor]
        public PropertyItem(string propertyName,
            string propertyUserFriendlyName,
            bool isEnabled,
            Type dataType,
            Dictionary<Type, bool> conditionFulfilled,
            Func<object?>? getCurrentValueFunc)
        {
            PropertyName = propertyName;
            PropertyUserFriendlyName = propertyUserFriendlyName;
            IsEnabled = isEnabled;
            GetCurrentValueFunc = getCurrentValueFunc;
            DataType = dataType;
            ConditionFulfilled = conditionFulfilled;
        }

        public string PropertyName { get; set; }

        public string PropertyUserFriendlyName { get; set; }

        public bool IsEnabled { get; set; }

        [JsonIgnore]
        public Func<object?>? GetCurrentValueFunc { get; set; }

        public Type DataType { get; set; }

        public Dictionary<Type, bool> ConditionFulfilled { get; set; }
    }
}
