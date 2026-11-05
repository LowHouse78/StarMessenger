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
            DataType = deepCopy.DataType;
            ConditionFulfilled = deepCopy.ConditionFulfilled;
        }

        [JsonConstructor]
        public PropertyItem(string propertyName,
            string propertyUserFriendlyName,
            bool isEnabled,
            Type dataType,
            Dictionary<Type, bool> conditionFulfilled)
        {
            PropertyName = propertyName;
            PropertyUserFriendlyName = propertyUserFriendlyName;
            IsEnabled = isEnabled;
            DataType = dataType;
            ConditionFulfilled = conditionFulfilled;
        }

        public string PropertyName { get; set; }

        public string PropertyUserFriendlyName { get; set; }

        public bool IsEnabled { get; set; }

        public Type DataType { get; set; }

        /// <summary>No longer used. Kept, so sequences saved with older versions can still be loaded.</summary>
        public Dictionary<Type, bool> ConditionFulfilled { get; set; }
    }
}
