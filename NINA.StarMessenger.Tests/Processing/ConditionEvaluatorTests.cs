using FluentAssertions;
using NINA.StarMessenger.Communication.Trigger;
using NINA.StarMessenger.Processing;
using NINA.StarMessenger.Providers;
using NUnit.Framework;

namespace NINA.StarMessenger.Tests.Processing
{
    [TestFixture]
    public class ConditionEvaluatorTests
    {
        private const string NotEqual = "≠";

        private sealed class Owner
        {
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            DataProvider.Configure();
        }

        internal static Condition<T> CreateCondition<T>(string propertyName, string selectedOperator, string value, string aimedConsecutiveCount = "1")
        {
            var property = DataProvider.GetProperties().Single(p => p.PropertyName == propertyName);
            var condition = new Condition<T>
            {
                SelectedPropertyForCondition = property
            };
            condition.SelectedOperatorForCondition = selectedOperator;
            condition.ValueForCondition = value;
            condition.AimedConsecutiveCount = aimedConsecutiveCount;
            return condition;
        }

        private static IReadOnlyCollection<string> Evaluate(ImageSnapshot snapshot, params Condition<Owner>[] conditions)
        {
            return ConditionEvaluator.Evaluate(conditions, snapshot, _ => true);
        }

        [TestCase(">", 2.5, 3.1, true)]
        [TestCase(">", 2.5, 2.4, false)]
        [TestCase("<", 2.5, 2.4, true)]
        [TestCase("<", 2.5, 2.6, false)]
        [TestCase("=", 2.5, 2.505, true)]
        [TestCase("=", 2.5, 2.6, false)]
        public void Evaluate_DoubleOperators(string selectedOperator, double conditionValue, double actualValue, bool expected)
        {
            var condition = CreateCondition<Owner>("HFR", selectedOperator, conditionValue.ToString());

            var fulfilled = Evaluate(TestSnapshots.Create(1, ("HFR", actualValue)), condition);

            fulfilled.Contains("HFR").Should().Be(expected);
        }

        [TestCase("<", "-10", -12.3, true)]
        [TestCase("<", "-10", -5.0, false)]
        [TestCase(">", "-10", -5.0, true)]
        public void Evaluate_NegativeConditionValues(string selectedOperator, string conditionValue, double temperature, bool expected)
        {
            var condition = CreateCondition<Owner>("Temperature", selectedOperator, conditionValue);

            var fulfilled = Evaluate(TestSnapshots.Create(1, ("Temperature", temperature)), condition);

            fulfilled.Contains("Temperature").Should().Be(expected);
        }

        [Test]
        public void Evaluate_IntegerProperty()
        {
            var condition = CreateCondition<Owner>("Stars", "<", "100");

            Evaluate(TestSnapshots.Create(1, ("Stars", 42)), condition).Should().Contain("Stars");
            Evaluate(TestSnapshots.Create(2, ("Stars", 420)), condition).Should().BeEmpty();
        }

        [Test]
        public void Evaluate_RotatorPositionWithDecimals_IsComparedAsNumber()
        {
            // The rotator position is a double in N.I.N.A.; before 1.1.0 it was treated as integer and never matched.
            var condition = CreateCondition<Owner>("RotatorPosition", ">", "90");

            Evaluate(TestSnapshots.Create(1, ("RotatorPosition", 90.75)), condition).Should().Contain("RotatorPosition");
        }

        [TestCase("=", "Ha", "Ha", true)]
        [TestCase("=", "Ha", "OIII", false)]
        [TestCase(NotEqual, "Ha", "OIII", true)]
        [TestCase(NotEqual, "Ha", "Ha", false)]
        public void Evaluate_StringOperators(string selectedOperator, string conditionValue, string filter, bool expected)
        {
            var condition = CreateCondition<Owner>("Filter", selectedOperator, conditionValue);

            var fulfilled = Evaluate(TestSnapshots.Create(1, ("Filter", filter)), condition);

            fulfilled.Contains("Filter").Should().Be(expected);
        }

        [Test]
        public void Evaluate_DateTime()
        {
            var limit = new DateTime(2026, 11, 4, 3, 0, 0);
            var condition = CreateCondition<Owner>("Time", ">", limit.ToString());

            Evaluate(TestSnapshots.Create(1, ("Time", limit.AddMinutes(5))), condition).Should().Contain("Time");
            Evaluate(TestSnapshots.Create(2, ("Time", limit.AddMinutes(-5))), condition).Should().BeEmpty();
        }

        [Test]
        public void Evaluate_ConditionsAreOrCombined()
        {
            var hfr = CreateCondition<Owner>("HFR", ">", "3");
            var stars = CreateCondition<Owner>("Stars", "<", "100");

            var fulfilled = Evaluate(TestSnapshots.Create(1, ("HFR", 2.0), ("Stars", 50)), hfr, stars);

            fulfilled.Should().Equal("Stars");
        }

        [Test]
        public void Evaluate_ConsecutiveCount_OnlyFulfilledAfterAimedNumberOfImagesInARow()
        {
            var condition = CreateCondition<Owner>("HFR", ">", "3", aimedConsecutiveCount: "3");

            Evaluate(TestSnapshots.Create(1, ("HFR", 3.5)), condition).Should().BeEmpty();
            condition.ConsecutiveCountStatusLevel.Should().Be(ConsecutiveCountStatusLevelType.Warning);

            Evaluate(TestSnapshots.Create(2, ("HFR", 3.5)), condition).Should().BeEmpty();
            Evaluate(TestSnapshots.Create(3, ("HFR", 3.5)), condition).Should().Contain("HFR");
            condition.ConsecutiveCountStatusLevel.Should().Be(ConsecutiveCountStatusLevelType.Error);

            // Stays fulfilled as long as the condition is met
            Evaluate(TestSnapshots.Create(4, ("HFR", 3.5)), condition).Should().Contain("HFR");
        }

        [Test]
        public void Evaluate_ConsecutiveCount_IsResetWhenConditionIsNotMet()
        {
            var condition = CreateCondition<Owner>("HFR", ">", "3", aimedConsecutiveCount: "2");

            Evaluate(TestSnapshots.Create(1, ("HFR", 3.5)), condition);
            Evaluate(TestSnapshots.Create(2, ("HFR", 2.0)), condition).Should().BeEmpty();
            condition.CurrentConsecutiveCount.Should().Be(0);
            condition.ConsecutiveCountStatusLevel.Should().Be(ConsecutiveCountStatusLevelType.Ok);

            Evaluate(TestSnapshots.Create(3, ("HFR", 3.5)), condition).Should().BeEmpty();
            Evaluate(TestSnapshots.Create(4, ("HFR", 3.5)), condition).Should().Contain("HFR");
        }

        [Test]
        public void Evaluate_ValueNotAvailableForImage_ConditionIsSkippedWithoutResettingTheCounter()
        {
            var condition = CreateCondition<Owner>("RMSTotal", ">", "1", aimedConsecutiveCount: "2");

            Evaluate(TestSnapshots.Create(1, ("RMSTotal", 1.5)), condition);
            Evaluate(TestSnapshots.Create(2), condition).Should().BeEmpty();
            condition.CurrentConsecutiveCount.Should().Be(1);
        }

        [Test]
        public void Evaluate_PropertyDisabledInOptions_IsSkipped()
        {
            var condition = CreateCondition<Owner>("HFR", ">", "3");

            var fulfilled = ConditionEvaluator.Evaluate(new[] { condition }, TestSnapshots.Create(1, ("HFR", 5.0)), _ => false);

            fulfilled.Should().BeEmpty();
        }
    }
}
