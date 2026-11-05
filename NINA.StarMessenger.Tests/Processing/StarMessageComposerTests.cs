using FluentAssertions;
using NINA.StarMessenger.Processing;
using NINA.StarMessenger.Providers;
using NUnit.Framework;

namespace NINA.StarMessenger.Tests.Processing
{
    [TestFixture]
    public class StarMessageComposerTests
    {
        private static readonly PropertyItem[] Properties =
        {
            new("HFR", "HFR", true, typeof(double), new Dictionary<Type, bool>()),
            new("Stars", "Stars", true, typeof(int), new Dictionary<Type, bool>()),
            new("RotatorPosition", "Rotor Position", true, typeof(double), new Dictionary<Type, bool>())
        };

        private static string[] Lines(string message)
        {
            return message.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        }

        [Test]
        public void Compose_OneLinePerAvailableValueInOrderOfProperties()
        {
            var snapshot = TestSnapshots.Create(1, ("Stars", 321), ("HFR", 2.5));

            var message = StarMessageComposer.Compose(snapshot, Properties, TriggerSourceTypes.Default);

            // Rotator position is not available for this image and is left out
            Lines(message).Should().Equal($"HFR: {2.5}", "Stars: 321");
        }

        [Test]
        public void Compose_ByCondition_MarksFulfilledConditions()
        {
            var snapshot = TestSnapshots.Create(1, ("Stars", 321), ("HFR", 2.5));

            var message = StarMessageComposer.Compose(snapshot, Properties, TriggerSourceTypes.ByCondition, new[] { "HFR" });

            Lines(message).Should().Equal($"HFR: {2.5}{StarMessageComposer.ConditionFulfilledHint}", "Stars: 321");
        }

        [Test]
        public void Compose_ByExposures_AppendsFooter()
        {
            var message = StarMessageComposer.Compose(TestSnapshots.Create(1, ("HFR", 2.5)), Properties, TriggerSourceTypes.ByExposures);

            Lines(message).Should().Contain(StarMessageComposer.ExposuresFulfilledText);
        }

        [Test]
        public void Compose_NoValueAvailable_ShowsNoImageText()
        {
            var message = StarMessageComposer.Compose(TestSnapshots.Create(1), Properties, TriggerSourceTypes.Default);

            Lines(message).First().Should().Be(StarMessageComposer.NoImageInHistoryText);
        }

        [Test]
        public void ComposeWithoutImage_NoLightFrameYet_ShowsNoImageText()
        {
            var message = StarMessageComposer.ComposeWithoutImage(TriggerSourceTypes.Default);

            Lines(message).First().Should().Be(StarMessageComposer.NoImageInHistoryText);
        }

        [Test]
        public void ComposeWithoutImage_SaveTimeout_ShowsNotSavedText()
        {
            var message = StarMessageComposer.ComposeWithoutImage(TriggerSourceTypes.ByExposures, TimeSpan.FromSeconds(120));

            Lines(message).Should().StartWith("StarMessage - the image was not saved within 120 s")
                          .And.Contain(StarMessageComposer.ExposuresFulfilledText);
        }
    }
}
