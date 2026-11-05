using System.Diagnostics;
using System.IO;
using FluentAssertions;
using NINA.StarMessenger.Processing;
using NUnit.Framework;

namespace NINA.StarMessenger.Tests.Processing
{
    [TestFixture]
    public class StarMessageDispatcherTests
    {
        private FakeChannel _channel = null!;
        private FakePreviewProvider _previewProvider = null!;
        private List<string> _reportedErrors = null!;
        private bool _sequenceRunning;
        private StarMessageDispatcher _dispatcher = null!;

        [SetUp]
        public void SetUp()
        {
            _channel = new FakeChannel();
            _previewProvider = new FakePreviewProvider();
            _reportedErrors = new List<string>();
            _sequenceRunning = true;
            _dispatcher = CreateDispatcher(TimeSpan.FromSeconds(10));
        }

        [TearDown]
        public void TearDown()
        {
            _dispatcher.Dispose();
        }

        private StarMessageDispatcher CreateDispatcher(TimeSpan saveTimeout)
        {
            return new StarMessageDispatcher(_previewProvider,
                                             () => _sequenceRunning,
                                             error => { lock (_reportedErrors) { _reportedErrors.Add(error); } },
                                             new StarMessageDispatcherOptions
                                             {
                                                 SaveTimeout = saveTimeout,
                                                 SendTimeout = TimeSpan.FromSeconds(5),
                                                 AbortGracePeriod = TimeSpan.FromMilliseconds(300)
                                             });
        }

        private StarMessageJob CreateJob(int? imageId, CancellationToken abortToken = default, Func<ImageSnapshot?, string?>? compose = null)
        {
            return new StarMessageJob(imageId,
                                      _channel,
                                      compose ?? (snapshot => snapshot == null ? "no image" : $"image {snapshot.ImageId}"),
                                      abortToken);
        }

        [Test]
        public async Task Enqueue_ImageNotSavedYet_ReturnsImmediatelyAndSendsOnlyAfterImageSaved()
        {
            var stopwatch = Stopwatch.StartNew();
            var queuedMessage = _dispatcher.Enqueue(CreateJob(1));
            stopwatch.Stop();

            // The caller (trigger/instruction) is not blocked
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);

            // While N.I.N.A. is still saving, nothing is sent
            await Task.Delay(500);
            queuedMessage.IsCompleted.Should().BeFalse();
            _channel.Sent.Should().BeEmpty();

            _dispatcher.OnImageSaved(TestSnapshots.Create(1));

            (await queuedMessage.WithTimeout()).Should().Be(StarMessageJobResult.Sent);
            _channel.Sent.Should().ContainSingle().Which.Message.Should().Be("image 1");
        }

        [Test]
        public async Task Enqueue_SavingTakesLong_MessageStillContainsImageData()
        {
            // Bug 'no new image in history found' without dithering: the old code gave up after a fixed 15 s.
            // Now the message waits for the ImageSaved event (up to the save timeout, here 10 s).
            var queuedMessage = _dispatcher.Enqueue(CreateJob(1));

            await Task.Delay(1500);
            _dispatcher.OnImageSaved(TestSnapshots.Create(1));

            (await queuedMessage.WithTimeout()).Should().Be(StarMessageJobResult.Sent);
            _channel.Sent.Single().Message.Should().Be("image 1");
        }

        [Test]
        public async Task Enqueue_ImageAlreadySaved_SendsImmediately()
        {
            // e.g. dithering ran before the trigger, so the image was saved before the trigger queued the message
            _dispatcher.OnImageSaved(TestSnapshots.Create(1));

            (await _dispatcher.Enqueue(CreateJob(1)).WithTimeout(1000)).Should().Be(StarMessageJobResult.Sent);
            _channel.Sent.Single().Message.Should().Be("image 1");
        }

        [Test]
        public async Task Enqueue_ImageNotSavedWithinSaveTimeout_ComposeGetsNullAndHintIsSent()
        {
            _dispatcher.Dispose();
            _dispatcher = CreateDispatcher(TimeSpan.FromMilliseconds(300));

            var result = await _dispatcher.Enqueue(CreateJob(1)).WithTimeout();

            result.Should().Be(StarMessageJobResult.Sent);
            _channel.Sent.Single().Message.Should().Be("no image");
        }

        [Test]
        public async Task Enqueue_WithoutImageId_ComposeGetsNullWithoutWaiting()
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await _dispatcher.Enqueue(CreateJob(null)).WithTimeout(1000);

            result.Should().Be(StarMessageJobResult.Sent);
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
            _channel.Sent.Single().Message.Should().Be("no image");
        }

        [Test]
        public async Task Enqueue_ComposeReturnsNull_IsSkippedAndNothingSent()
        {
            _dispatcher.OnImageSaved(TestSnapshots.Create(1));

            var result = await _dispatcher.Enqueue(CreateJob(1, compose: _ => null)).WithTimeout();

            result.Should().Be(StarMessageJobResult.Skipped);
            _channel.Sent.Should().BeEmpty();
        }

        [Test]
        public async Task SequenceAborted_WhileWaitingForImage_MessageIsDiscarded()
        {
            using var sequenceToken = new CancellationTokenSource();
            var queuedMessage = _dispatcher.Enqueue(CreateJob(1, sequenceToken.Token));

            _sequenceRunning = false;
            sequenceToken.Cancel();

            (await queuedMessage.WithTimeout()).Should().Be(StarMessageJobResult.Discarded);

            // An image saved afterwards does not resurrect the message
            _dispatcher.OnImageSaved(TestSnapshots.Create(1));
            await Task.Delay(200);
            _channel.Sent.Should().BeEmpty();
        }

        [Test]
        public async Task SequenceAborted_ImageAlreadySaved_MessageIsDiscarded()
        {
            using var sequenceToken = new CancellationTokenSource();
            _sequenceRunning = false;
            sequenceToken.Cancel();
            _dispatcher.OnImageSaved(TestSnapshots.Create(1));

            (await _dispatcher.Enqueue(CreateJob(1, sequenceToken.Token)).WithTimeout()).Should().Be(StarMessageJobResult.Discarded);
            _channel.Sent.Should().BeEmpty();
        }

        [Test]
        public async Task ContainerInterrupted_SequenceKeepsRunning_MessageIsStillSent()
        {
            // N.I.N.A. cancels the token also when e.g. a loop condition interrupts a container. The sequence goes on.
            using var sequenceToken = new CancellationTokenSource();
            var queuedMessage = _dispatcher.Enqueue(CreateJob(1, sequenceToken.Token));

            sequenceToken.Cancel();
            await Task.Delay(500); // longer than the grace period
            _dispatcher.OnImageSaved(TestSnapshots.Create(1));

            (await queuedMessage.WithTimeout()).Should().Be(StarMessageJobResult.Sent);
            _channel.Sent.Should().ContainSingle();
        }

        [Test]
        public async Task SequenceEndsRegularly_ImageSavedAfterwards_MessageIsStillSent()
        {
            var queuedMessage = _dispatcher.Enqueue(CreateJob(1, new CancellationTokenSource().Token));

            _sequenceRunning = false; // regular end: the token is not cancelled
            _dispatcher.OnImageSaved(TestSnapshots.Create(1));

            (await queuedMessage.WithTimeout()).Should().Be(StarMessageJobResult.Sent);
        }

        [Test]
        public async Task Jobs_AreProcessedInOrderOfEnqueueing_EvenIfImagesAreReportedOutOfOrder()
        {
            var first = _dispatcher.Enqueue(CreateJob(1));
            var second = _dispatcher.Enqueue(CreateJob(2));
            var third = _dispatcher.Enqueue(CreateJob(3));

            _dispatcher.OnImageSaved(TestSnapshots.Create(3));
            _dispatcher.OnImageSaved(TestSnapshots.Create(2));
            await Task.Delay(200);
            _channel.Sent.Should().BeEmpty("message for image 1 has to be sent first");

            _dispatcher.OnImageSaved(TestSnapshots.Create(1));
            await Task.WhenAll(first, second, third).WithTimeout();

            _channel.Sent.Select(s => s.Message).Should().Equal("image 1", "image 2", "image 3");
        }

        [Test]
        public async Task SendFails_ErrorIsReportedAndResultIsFailed()
        {
            _channel.Succeeds = false;
            _dispatcher.OnImageSaved(TestSnapshots.Create(1));

            (await _dispatcher.Enqueue(CreateJob(1)).WithTimeout()).Should().Be(StarMessageJobResult.Failed);
            _reportedErrors.Should().ContainSingle().Which.Should().Contain("Fake");
        }

        [Test]
        public async Task ComposeThrows_ErrorIsReportedAndFollowingJobsAreStillProcessed()
        {
            _dispatcher.OnImageSaved(TestSnapshots.Create(1));
            _dispatcher.OnImageSaved(TestSnapshots.Create(2));

            var failing = _dispatcher.Enqueue(CreateJob(1, compose: _ => throw new InvalidOperationException("boom")));
            var following = _dispatcher.Enqueue(CreateJob(2));

            (await failing.WithTimeout()).Should().Be(StarMessageJobResult.Failed);
            (await following.WithTimeout()).Should().Be(StarMessageJobResult.Sent);
            _reportedErrors.Should().ContainSingle().Which.Should().Contain("boom");
        }

        [Test]
        public async Task Preview_IsAttachedAndDeletedAfterSending()
        {
            _previewProvider.CreatePreview = true;
            _dispatcher.OnImageSaved(TestSnapshots.Create(1));

            await _dispatcher.Enqueue(CreateJob(1)).WithTimeout();

            var sent = _channel.Sent.Single();
            sent.ImageFilePath.Should().Be(_previewProvider.LastPreviewFilePath);
            sent.ImageFileExistedWhileSending.Should().BeTrue();
            File.Exists(_previewProvider.LastPreviewFilePath).Should().BeFalse();
        }

        [Test]
        public async Task Preview_IsDeletedAlsoIfSendingFails()
        {
            _previewProvider.CreatePreview = true;
            _channel.Succeeds = false;
            _dispatcher.OnImageSaved(TestSnapshots.Create(1));

            await _dispatcher.Enqueue(CreateJob(1)).WithTimeout();

            File.Exists(_previewProvider.LastPreviewFilePath).Should().BeFalse();
        }

        [Test]
        public async Task WaitingForImage_DoesNotKeepTheCpuBusy()
        {
            // The old implementation polled in a loop without delay and kept one core busy while waiting.
            var process = Process.GetCurrentProcess();
            var cpuBefore = process.TotalProcessorTime;

            var queuedMessage = _dispatcher.Enqueue(CreateJob(1));
            await Task.Delay(1500);

            process.Refresh();
            var cpuUsed = process.TotalProcessorTime - cpuBefore;
            cpuUsed.Should().BeLessThan(TimeSpan.FromMilliseconds(750));

            _dispatcher.OnImageSaved(TestSnapshots.Create(1));
            await queuedMessage.WithTimeout();
        }

        [Test]
        public async Task Dispose_DiscardsWaitingAndQueuedMessages()
        {
            var waiting = _dispatcher.Enqueue(CreateJob(1));
            var queued = _dispatcher.Enqueue(CreateJob(2));

            _dispatcher.Dispose();

            (await waiting.WithTimeout()).Should().Be(StarMessageJobResult.Discarded);
            (await queued.WithTimeout()).Should().Be(StarMessageJobResult.Discarded);
            (await _dispatcher.Enqueue(CreateJob(3)).WithTimeout()).Should().Be(StarMessageJobResult.Discarded);
            _channel.Sent.Should().BeEmpty();
        }

        [Test]
        public async Task Cache_KeepsOnlyTheConfiguredNumberOfImages()
        {
            _dispatcher.Dispose();
            _dispatcher = new StarMessageDispatcher(_previewProvider, () => true, _ => { },
                new StarMessageDispatcherOptions { CacheSize = 2, SaveTimeout = TimeSpan.FromMilliseconds(200) });

            _dispatcher.OnImageSaved(TestSnapshots.Create(1));
            _dispatcher.OnImageSaved(TestSnapshots.Create(2));
            _dispatcher.OnImageSaved(TestSnapshots.Create(3));

            await _dispatcher.Enqueue(CreateJob(1)).WithTimeout();
            await _dispatcher.Enqueue(CreateJob(3)).WithTimeout();

            _channel.Sent.Select(s => s.Message).Should().Equal("no image", "image 3");
        }
    }
}
