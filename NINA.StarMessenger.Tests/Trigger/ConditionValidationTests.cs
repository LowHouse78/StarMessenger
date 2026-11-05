using FluentAssertions;
using NINA.StarMessenger.Communication.Trigger;
using NINA.StarMessenger.Providers;
using NINA.StarMessenger.Tests.Processing;
using NUnit.Framework;

namespace NINA.StarMessenger.Tests.Trigger
{
    [TestFixture]
    public class ConditionValidationTests
    {
        private sealed class Owner
        {
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            DataProvider.Configure();
        }

        private static Condition<Owner> CreateEnabledCondition(string propertyName, string value, string aimedConsecutiveCount = "1")
        {
            var condition = ConditionEvaluatorTests.CreateCondition<Owner>(propertyName, ">", value, aimedConsecutiveCount);
            condition.SelectedPropertyForCondition!.IsEnabled = true;
            return condition;
        }

        [TestCase("Temperature", "-10")]
        [TestCase("Temperature", "2.5")]
        [TestCase("Stars", "-3")]
        [TestCase("Stars", "100")]
        public void Validate_ValidNumbers(string propertyName, string value)
        {
            var condition = CreateEnabledCondition(propertyName, value.Replace(".", Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator));

            condition.Validate().Should().BeTrue(string.Join(", ", condition.Issues));
        }

        [TestCase("Temperature", "abc")]
        [TestCase("Temperature", "1-0")]
        [TestCase("Stars", "1.5")]
        public void Validate_InvalidNumbers(string propertyName, string value)
        {
            var condition = CreateEnabledCondition(propertyName, value.Replace(".", Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator));

            condition.Validate().Should().BeFalse();
            condition.Issues.Should().ContainSingle();
        }

        [TestCase("HFR")]
        [TestCase("Filter")]
        public void Validate_ConsecutiveCountOutOfRange_IsInvalidForAllDataTypes(string propertyName)
        {
            var condition = CreateEnabledCondition(propertyName, "1", aimedConsecutiveCount: "0");

            condition.Validate().Should().BeFalse();
            condition.Issues.Should().ContainSingle().Which.Should().Contain("consecutive count");
        }

        [Test]
        public void Validate_PropertyDisabledInOptions_IsInvalid()
        {
            var condition = CreateEnabledCondition("HFR", "2");
            condition.SelectedPropertyForCondition!.IsEnabled = false;

            condition.Validate().Should().BeFalse();
            condition.Issues.Should().Contain(i => i.Contains("not enabled"));
        }

        [Test]
        public void SettingChanged_OnlyAffectsTheMatchingProperty()
        {
            var condition = CreateEnabledCondition("HFR", "2");
            var originalStars = Settings.Default.Stars;
            try
            {
                Settings.Default.Stars = !originalStars;

                condition.SelectedPropertyForCondition!.IsEnabled.Should().BeTrue("disabling 'Stars' must not disable 'HFR'");
                condition.PropertiesForCondition.Single(p => p.PropertyName == "Stars").IsEnabled.Should().Be(!originalStars);
            }
            finally
            {
                Settings.Default.Stars = originalStars;
            }
        }
    }
}
