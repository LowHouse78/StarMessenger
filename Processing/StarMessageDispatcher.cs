#region "copyright"

/*
    Copyright © 2026 Sascha Lohaus (sascha.lohaus@gmail.com)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Diagnostics;
using System.IO;
using System.Threading.Channels;
using NINA.Core.Utility;

namespace NINA.StarMessenger.Processing
{
    internal enum StarMessageJobResult
    {
        /// <summary>The messenger service accepted the message.</summary>
        Sent,
        /// <summary>Nothing to send, e.g. no condition fulfilled.</summary>
        Skipped,
        /// <summary>Dropped because the sequence was aborted or N.I.N.A. is shutting down.</summary>
        Discarded,
        /// <summary>Sending failed. The error has been reported.</summary>
        Failed
    }

    /// <summary>
    /// A StarMessage that waits for the image with <see cref="ImageId"/> to be saved.
    /// </summary>
    internal sealed class StarMessageJob
    {
        /// <param name="imageId">Id of the image the message is about; null if there is no light frame yet.</param>
        /// <param name="channel">Messenger service used for sending.</param>
        /// <param name="composeMessage">Creates the message text from the saved image. The argument is null if there is
        /// no image or it was not saved within the save timeout. Returning null skips the message.</param>
        /// <param name="abortToken">Token of the sequence. If it is cancelled and the sequence stops, the message is discarded.</param>
        public StarMessageJob(int? imageId,
                              IStarMessageChannel channel,
                              Func<ImageSnapshot?, string?> composeMessage,
                              CancellationToken abortToken)
        {
            ImageId = imageId;
            Channel = channel;
            ComposeMessage = composeMessage;
            AbortToken = abortToken;
        }

        public int? ImageId { get; }
        public IStarMessageChannel Channel { get; }
        public Func<ImageSnapshot?, string?> ComposeMessage { get; }
        public CancellationToken AbortToken { get; }
    }

    internal sealed class StarMessageDispatcherOptions
    {
        /// <summary>Maximum time to wait for N.I.N.A. to save an image.</summary>
        public TimeSpan SaveTimeout { get; init; } = TimeSpan.FromSeconds(120);

        /// <summary>Maximum time for creating the preview and sending one message.</summary>
        public TimeSpan SendTimeout { get; init; } = TimeSpan.FromSeconds(45);

        /// <summary>After the sequence token is cancelled: time to wait for the sequence to stop. If it keeps running,
        /// only a container was interrupted (e.g. by a loop condition) and the message is still sent.</summary>
        public TimeSpan AbortGracePeriod { get; init; } = TimeSpan.FromSeconds(5);

        /// <summary>Number of saved images kept, so a message can still be sent if the image was saved before the trigger ran.</summary>
        public int CacheSize { get; init; } = 50;
    }

    /// <summary>
    /// Decouples sending StarMessages from the sequence: triggers and instructions only enqueue a job and return immediately.
    /// The jobs are processed one after the other in a background worker as soon as N.I.N.A. reports the image as saved.
    /// </summary>
    internal sealed class StarMessageDispatcher : IDisposable
    {
        private static readonly Task NeverCompletingTask = new TaskCompletionSource().Task;
        private static StarMessageDispatcher? _default;

        private readonly StarMessageDispatcherOptions _options;
        private readonly IPreviewImageProvider _previewImageProvider;
        private readonly Func<bool> _isSequenceRunning;
        private readonly Action<string> _reportError;

        private readonly object _lock = new();
        private readonly Dictionary<int, ImageSnapshot> _savedImages = new();
        private readonly Queue<int> _savedImageOrder = new();
        private readonly Dictionary<int, TaskCompletionSource<ImageSnapshot>> _waitingForImage = new();

        private readonly Channel<WorkItem> _queue = Channel.CreateUnbounded<WorkItem>(new UnboundedChannelOptions { SingleReader = true });
        private readonly CancellationTokenSource _shutdown = new();
        private readonly Task _worker;

        public StarMessageDispatcher(IPreviewImageProvider previewImageProvider,
                                     Func<bool> isSequenceRunning,
                                     Action<string> reportError,
                                     StarMessageDispatcherOptions? options = null)
        {
            _previewImageProvider = previewImageProvider;
            _isSequenceRunning = isSequenceRunning;
            _reportError = reportError;
            _options = options ?? new StarMessageDispatcherOptions();
            _worker = Task.Run(WorkAsync);
        }

        /// <summary>The instance used by triggers and instructions. Set by the plugin.</summary>
        public static StarMessageDispatcher? Default
        {
            get => Volatile.Read(ref _default);
            set => Volatile.Write(ref _default, value);
        }

        public TimeSpan SaveTimeout => _options.SaveTimeout;

        /// <summary>Called for every saved image (N.I.N.A. ImageSaved event).</summary>
        public void OnImageSaved(ImageSnapshot snapshot)
        {
            TaskCompletionSource<ImageSnapshot>? waiter;
            lock (_lock)
            {
                if (!_savedImages.ContainsKey(snapshot.ImageId))
                {
                    _savedImageOrder.Enqueue(snapshot.ImageId);
                }
                _savedImages[snapshot.ImageId] = snapshot;

                while (_savedImageOrder.Count > _options.CacheSize)
                {
                    _savedImages.Remove(_savedImageOrder.Dequeue());
                }

                _waitingForImage.Remove(snapshot.ImageId, out waiter);
            }

            waiter?.TrySetResult(snapshot);
        }

        /// <summary>Queues a StarMessage. Returns immediately; the returned task completes once the job has been processed.</summary>
        public Task<StarMessageJobResult> Enqueue(StarMessageJob job)
        {
            var workItem = new WorkItem(job);
            if (_shutdown.IsCancellationRequested || !_queue.Writer.TryWrite(workItem))
            {
                workItem.Completion.TrySetResult(StarMessageJobResult.Discarded);
            }

            return workItem.Completion.Task;
        }

        public void Dispose()
        {
            if (_shutdown.IsCancellationRequested)
            {
                return;
            }

            _shutdown.Cancel();
            _queue.Writer.TryComplete();
            try
            {
                _worker.Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception e)
            {
                Logger.Error("Error while stopping the StarMessage dispatcher", e);
            }
        }

        private async Task WorkAsync()
        {
            try
            {
                await foreach (var workItem in _queue.Reader.ReadAllAsync(_shutdown.Token))
                {
                    var result = await ProcessAsync(workItem.Job);
                    workItem.Completion.TrySetResult(result);
                }
            }
            catch (OperationCanceledException)
            {
                // Shutdown
            }
            catch (Exception e)
            {
                Logger.Error("Unexpected error in the StarMessage dispatcher", e);
            }

            while (_queue.Reader.TryRead(out var remaining))
            {
                remaining.Completion.TrySetResult(StarMessageJobResult.Discarded);
            }
        }

        private async Task<StarMessageJobResult> ProcessAsync(StarMessageJob job)
        {
            var abortState = new AbortState();
            string? previewFilePath = null;
            try
            {
                ImageSnapshot? snapshot = null;
                if (job.ImageId is { } imageId)
                {
                    var (savedImage, discarded) = await WaitForSavedImageAsync(imageId, job.AbortToken, abortState);
                    if (discarded)
                    {
                        Logger.Info($"StarMessage for image {imageId} via {job.Channel.Name} discarded, the sequence was aborted.");
                        return StarMessageJobResult.Discarded;
                    }

                    if (savedImage == null)
                    {
                        Logger.Warning($"Image {imageId} was not saved within {_options.SaveTimeout.TotalSeconds:0} s.");
                    }
                    snapshot = savedImage;
                }

                var message = job.ComposeMessage(snapshot);
                if (message == null)
                {
                    return StarMessageJobResult.Skipped;
                }

                if (await IsDiscardedByAbortAsync(job.AbortToken, abortState))
                {
                    Logger.Info($"StarMessage via {job.Channel.Name} discarded, the sequence was aborted.");
                    return StarMessageJobResult.Discarded;
                }

                using var sendCts = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token);
                sendCts.CancelAfter(_options.SendTimeout);

                if (!string.IsNullOrEmpty(snapshot?.LocalPath))
                {
                    previewFilePath = await _previewImageProvider.CreatePreviewAsync(snapshot.LocalPath, sendCts.Token);
                }

                if (await job.Channel.SendAsync(message, previewFilePath, sendCts.Token))
                {
                    return StarMessageJobResult.Sent;
                }

                ReportError($"StarMessenger: StarMessage via {job.Channel.Name} could not be sent. See log for details.");
                return StarMessageJobResult.Failed;
            }
            catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
            {
                return StarMessageJobResult.Discarded;
            }
            catch (OperationCanceledException)
            {
                ReportError($"StarMessenger: Sending the StarMessage via {job.Channel.Name} exceeded the timeout of {_options.SendTimeout.TotalSeconds:0} s.");
                return StarMessageJobResult.Failed;
            }
            catch (Exception e)
            {
                Logger.Error($"Unexpected error while sending a StarMessage via {job.Channel.Name}: {e.Message}", e);
                ReportError($"StarMessenger: StarMessage via {job.Channel.Name} could not be sent: {e.Message}");
                return StarMessageJobResult.Failed;
            }
            finally
            {
                DeleteFile(previewFilePath);
            }
        }

        private async Task<(ImageSnapshot? snapshot, bool discarded)> WaitForSavedImageAsync(int imageId,
                                                                                            CancellationToken abortToken,
                                                                                            AbortState abortState)
        {
            TaskCompletionSource<ImageSnapshot>? waiter;
            lock (_lock)
            {
                if (_savedImages.TryGetValue(imageId, out var savedImage))
                {
                    return (savedImage, false);
                }

                if (!_waitingForImage.TryGetValue(imageId, out waiter))
                {
                    waiter = new TaskCompletionSource<ImageSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _waitingForImage[imageId] = waiter;
                }
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token);
            timeoutCts.CancelAfter(_options.SaveTimeout);
            var timeoutTask = Task.Delay(Timeout.Infinite, timeoutCts.Token);

            var abortSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var abortRegistration = abortToken.Register(() => abortSignal.TrySetResult());
            var abortTask = abortToken.CanBeCanceled ? abortSignal.Task : NeverCompletingTask;

            try
            {
                while (true)
                {
                    var completedTask = await Task.WhenAny(waiter.Task, timeoutTask, abortTask);
                    if (completedTask == waiter.Task)
                    {
                        return (await waiter.Task, false);
                    }

                    if (completedTask == timeoutTask)
                    {
                        _shutdown.Token.ThrowIfCancellationRequested();
                        return (null, false);
                    }

                    if (await IsDiscardedByAbortAsync(abortToken, abortState))
                    {
                        return (null, true);
                    }

                    // Only a container was interrupted, the sequence keeps running: keep waiting for the image.
                    abortTask = NeverCompletingTask;
                }
            }
            finally
            {
                lock (_lock)
                {
                    if (!waiter.Task.IsCompleted &&
                        _waitingForImage.TryGetValue(imageId, out var registeredWaiter) &&
                        registeredWaiter == waiter)
                    {
                        _waitingForImage.Remove(imageId);
                    }
                }
            }
        }

        /// <summary>
        /// A cancelled sequence token alone does not mean the user aborted the sequence: N.I.N.A. also cancels it when a
        /// container gets interrupted. The message is only discarded if the sequence stops within the grace period.
        /// </summary>
        private async Task<bool> IsDiscardedByAbortAsync(CancellationToken abortToken, AbortState abortState)
        {
            if (!abortToken.IsCancellationRequested)
            {
                return false;
            }

            if (abortState.Decision is { } decision)
            {
                return decision;
            }

            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                if (!IsSequenceRunning())
                {
                    abortState.Decision = true;
                    return true;
                }

                if (stopwatch.Elapsed >= _options.AbortGracePeriod)
                {
                    abortState.Decision = false;
                    return false;
                }

                var remaining = _options.AbortGracePeriod - stopwatch.Elapsed;
                await Task.Delay(remaining < TimeSpan.FromMilliseconds(100) ? remaining : TimeSpan.FromMilliseconds(100), _shutdown.Token);
            }
        }

        private bool IsSequenceRunning()
        {
            try
            {
                return _isSequenceRunning();
            }
            catch (Exception e)
            {
                // If the state is unknown, rather send a message too much than lose one.
                Logger.Error("Could not determine whether the sequence is running", e);
                return true;
            }
        }

        private void ReportError(string message)
        {
            try
            {
                Logger.Error(message);
                _reportError(message);
            }
            catch (Exception e)
            {
                Logger.Error("Error while reporting a StarMessenger error", e);
            }
        }

        private static void DeleteFile(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Could not delete temporary preview image {filePath}", e);
            }
        }

        private sealed class AbortState
        {
            public bool? Decision { get; set; }
        }

        private sealed class WorkItem
        {
            public WorkItem(StarMessageJob job)
            {
                Job = job;
            }

            public StarMessageJob Job { get; }

            public TaskCompletionSource<StarMessageJobResult> Completion { get; } =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}
