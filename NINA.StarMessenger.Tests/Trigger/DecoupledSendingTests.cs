using System.Diagnostics;
using FluentAssertions;
using Moq;
using NINA.Core.Model;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.StarMessenger.Communication.Trigger;
using NINA.StarMessenger.Processing;
using NINA.StarMessenger.Providers;
using NINA.StarMessenger.SequenceItems;
using NINA.StarMessenger.Tests.Processing;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Model;
using NUnit.Framework;

namespace NINA.StarMessenger.Tests.Trigger
{
    internal sealed class TestByConditionTrigger : StarMessageByConditionTriggerBase<TestByConditionTrigger>
    {
        private readonly FakeChannel _channel;

        public TestByConditionTrigger(IImageHistoryVM history, FakeChannel channel) : base(history)
        {
            _channel = channel;
        }

        public override object Clone() => throw new NotSupportedException();

        internal override IStarMessageChannel CreateChannel() => _channel;
    }

    internal sealed class TestAfterExposuresTrigger : StarMessageAfterExposuresTriggerBase
    {
        private readonly FakeChannel _channel;

        public TestAfterExposuresTrigger(IImageHistoryVM history, FakeChannel channel) : base(history)
        {
            _channel = channel;
        }

        public override object Clone() => throw new NotSupportedException();

        internal override IStarMessageChannel CreateChannel() => _channel;
    }

    internal sealed class TestInstruction : StarMessageInstructionBase
    {
        private readonly FakeChannel _channel;

        public TestInstruction(IImageHistoryVM history, FakeChannel channel)
            : base(history, new Mock<IApplicationStatusMediator>().Object)
        {
            _channel = channel;
        }

        public override object Clone() => throw new NotSupportedException();

        internal override IStarMessageChannel CreateChannel() => _channel;
    }

    /// <summary>
    /// Proves on the level of the sequencer entities that N.I.N.A. no longer waits for StarMessenger:
    /// Execute returns while the image is still being saved, the message follows once it is saved.
    /// </summary>
    [TestFixture]
    public class DecoupledSendingTests
    {
        private readonly List<ImageHistoryPoint> _imageHistory = new();
        private Mock<IImageHistoryVM> _history = null!;
        private FakeChannel _channel = null!;
        private StarMessageDispatcher _dispatcher = null!;
        private bool _originalHfrSetting;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            DataProvider.Configure();
        }

        [SetUp]
        public void SetUp()
        {
            _originalHfrSetting = Settings.Default.HFR;
            Settings.Default.HFR = true;

            _imageHistory.Clear();
            _history = new Mock<IImageHistoryVM>();
            _history.SetupGet(h => h.ImageHistory).Returns(_imageHistory);
            _channel = new FakeChannel();

            _dispatcher = new StarMessageDispatcher(new FakePreviewProvider(), () => true, _ => { },
                new StarMessageDispatcherOptions { SaveTimeout = TimeSpan.FromSeconds(10) });
            StarMessageDispatcher.Default = _dispatcher;
        }

        [TearDown]
        public void TearDown()
        {
            StarMessageDispatcher.Default = null;
            _dispatcher.Dispose();
            Settings.Default.HFR = _originalHfrSetting;
        }

        /// <summary>Like N.I.N.A.: the history point is added after the download, before the image is saved.</summary>
        private int AddLightFrameToHistory()
        {
            var id = _imageHistory.Count + 1;
            _imageHistory.Add(new ImageHistoryPoint(id, "LIGHT"));
            return id;
        }

        private static ISequenceItem CreateExposureItem(string imageType)
        {
            var exposure = new Mock<IExposureItem>();
            exposure.SetupGet(e => e.ImageType).Returns(imageType);
            return exposure.Object;
        }

        private static Task ExecuteTrigger(NINA.Sequencer.Trigger.SequenceTrigger trigger)
        {
            return trigger.Execute(new Mock<ISequenceContainer>().Object, new Progress<ApplicationStatus>(), CancellationToken.None);
        }

        private TestByConditionTrigger CreateHfrTrigger()
        {
            var trigger = new TestByConditionTrigger(_history.Object, _channel);
            trigger.Conditions!.Clear();
            trigger.Conditions.Add(ConditionEvaluatorTests.CreateCondition<TestByConditionTrigger>("HFR", ">", "3"));
            return trigger;
        }

        [Test]
        public async Task ByConditionTrigger_ReturnsWhileImageIsSaving_ConditionIsEvaluatedAfterSave()
        {
            var trigger = CreateHfrTrigger();
            var imageId = AddLightFrameToHistory();

            var stopwatch = Stopwatch.StartNew();
            var execute = ExecuteTrigger(trigger);
            stopwatch.Stop();

            execute.IsCompleted.Should().BeTrue("the sequence must not wait for the image to be saved");
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);

            await Task.Delay(300);
            _channel.Sent.Should().BeEmpty();

            _dispatcher.OnImageSaved(TestSnapshots.Create(imageId, ("HFR", 3.5)));

            (await trigger.LastQueuedMessage!.WithTimeout()).Should().Be(StarMessageJobResult.Sent);
            _channel.Sent.Single().Message.Should().Contain($"HFR: {3.5}{StarMessageComposer.ConditionFulfilledHint}");
        }

        [Test]
        public async Task ByConditionTrigger_ConditionNotFulfilled_NothingIsSent()
        {
            var trigger = CreateHfrTrigger();
            var imageId = AddLightFrameToHistory();

            await ExecuteTrigger(trigger);
            _dispatcher.OnImageSaved(TestSnapshots.Create(imageId, ("HFR", 2.0)));

            (await trigger.LastQueuedMessage!.WithTimeout()).Should().Be(StarMessageJobResult.Skipped);
            _channel.Sent.Should().BeEmpty();
        }

        [Test]
        public async Task ByConditionTrigger_SameImage_IsEvaluatedOnlyOnce()
        {
            var trigger = CreateHfrTrigger();
            AddLightFrameToHistory();

            await ExecuteTrigger(trigger);
            var firstQueuedMessage = trigger.LastQueuedMessage;
            await ExecuteTrigger(trigger);

            trigger.LastQueuedMessage.Should().BeSameAs(firstQueuedMessage);
        }

        [Test]
        public void ByConditionTrigger_ShouldTrigger_OnlyBeforeLightExposuresWithLightFramesInHistory()
        {
            var trigger = CreateHfrTrigger();

            trigger.ShouldTrigger(null!, CreateExposureItem("LIGHT")).Should().BeFalse("no light frame in history yet");

            AddLightFrameToHistory();
            trigger.ShouldTrigger(null!, CreateExposureItem("LIGHT")).Should().BeTrue();
            trigger.ShouldTrigger(null!, CreateExposureItem("FLAT")).Should().BeFalse();
        }

        [Test]
        public async Task AfterExposuresTrigger_ReturnsWhileImageIsSaving_MessageIsSentAfterSave()
        {
            var trigger = new TestAfterExposuresTrigger(_history.Object, _channel) { AfterExposures = 1 };
            var imageId = AddLightFrameToHistory();

            var execute = ExecuteTrigger(trigger);
            execute.IsCompleted.Should().BeTrue("the sequence must not wait for the image to be saved");

            await Task.Delay(300);
            _channel.Sent.Should().BeEmpty();

            _dispatcher.OnImageSaved(TestSnapshots.Create(imageId, ("HFR", 2.5)));

            (await trigger.LastQueuedMessage!.WithTimeout()).Should().Be(StarMessageJobResult.Sent);
            _channel.Sent.Single().Message.Should().Contain($"HFR: {2.5}").And.Contain(StarMessageComposer.ExposuresFulfilledText);
        }

        [Test]
        public async Task AfterExposuresTrigger_ShouldTrigger_AfterEveryNthLightFrame()
        {
            var trigger = new TestAfterExposuresTrigger(_history.Object, _channel) { AfterExposures = 2 };
            var nextLight = CreateExposureItem("LIGHT");

            AddLightFrameToHistory();
            trigger.ShouldTrigger(null!, nextLight).Should().BeFalse();

            AddLightFrameToHistory();
            trigger.ShouldTrigger(null!, nextLight).Should().BeTrue();
            await ExecuteTrigger(trigger);
            trigger.ShouldTrigger(null!, nextLight).Should().BeFalse("already triggered for this image");

            AddLightFrameToHistory();
            trigger.ShouldTrigger(null!, nextLight).Should().BeFalse();

            AddLightFrameToHistory();
            trigger.ShouldTrigger(null!, nextLight).Should().BeTrue();
        }

        [Test]
        public async Task Instruction_WithoutWaitForDelivery_ReturnsWhileImageIsSaving()
        {
            var instruction = new TestInstruction(_history.Object, _channel);
            var imageId = AddLightFrameToHistory();

            var execute = instruction.Execute(new Progress<ApplicationStatus>(), CancellationToken.None);
            execute.IsCompleted.Should().BeTrue();

            _dispatcher.OnImageSaved(TestSnapshots.Create(imageId, ("HFR", 2.5)));
            (await instruction.LastQueuedMessage!.WithTimeout()).Should().Be(StarMessageJobResult.Sent);
        }

        [Test]
        public async Task Instruction_WithWaitForDelivery_WaitsUntilMessageIsSent()
        {
            var instruction = new TestInstruction(_history.Object, _channel) { WaitForDelivery = true };
            var imageId = AddLightFrameToHistory();

            var execute = instruction.Execute(new Progress<ApplicationStatus>(), CancellationToken.None);
            await Task.Delay(300);
            execute.IsCompleted.Should().BeFalse("the instruction waits for the image and the delivery");

            _dispatcher.OnImageSaved(TestSnapshots.Create(imageId, ("HFR", 2.5)));

            await execute.WaitAsync(TimeSpan.FromSeconds(5));
            _channel.Sent.Should().ContainSingle();
        }

        [Test]
        public async Task Instruction_WithWaitForDelivery_SendingFails_InstructionFails()
        {
            _channel.Succeeds = false;
            var instruction = new TestInstruction(_history.Object, _channel) { WaitForDelivery = true };

            var execute = () => instruction.Execute(new Progress<ApplicationStatus>(), CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));

            await execute.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Instruction_NoLightFrameYet_SendsNoImageText()
        {
            var instruction = new TestInstruction(_history.Object, _channel) { WaitForDelivery = true };

            await instruction.Execute(new Progress<ApplicationStatus>(), CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));

            _channel.Sent.Single().Message.Should().StartWith(StarMessageComposer.NoImageInHistoryText);
        }
    }
}
