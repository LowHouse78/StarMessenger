using System.Collections.Concurrent;
using System.IO;
using NINA.StarMessenger.Processing;

namespace NINA.StarMessenger.Tests
{
    internal sealed record SentMessage(string Message, string? ImageFilePath, bool ImageFileExistedWhileSending);

    /// <summary>Records every message instead of sending it.</summary>
    internal sealed class FakeChannel : IStarMessageChannel
    {
        private readonly ConcurrentQueue<SentMessage> _sent = new();

        public string Name => "Fake";

        public bool Succeeds { get; set; } = true;

        public IReadOnlyList<SentMessage> Sent => _sent.ToList();

        public Task<bool> SendAsync(string message, string? imageFilePath, CancellationToken token)
        {
            _sent.Enqueue(new SentMessage(message, imageFilePath, imageFilePath != null && File.Exists(imageFilePath)));
            return Task.FromResult(Succeeds);
        }
    }

    /// <summary>Creates a real temporary file, so tests can check that it is deleted after sending.</summary>
    internal sealed class FakePreviewProvider : IPreviewImageProvider
    {
        public bool CreatePreview { get; set; }

        public string? LastPreviewFilePath { get; private set; }

        public Task<string?> CreatePreviewAsync(string imageFilePath, CancellationToken token)
        {
            if (!CreatePreview)
            {
                return Task.FromResult<string?>(null);
            }

            LastPreviewFilePath = Path.Combine(Path.GetTempPath(), $"StarMessengerTest_{Guid.NewGuid()}.jpg");
            File.WriteAllBytes(LastPreviewFilePath, new byte[] { 0xFF, 0xD8, 0xFF });
            return Task.FromResult<string?>(LastPreviewFilePath);
        }
    }

    internal static class TestSnapshots
    {
        public static ImageSnapshot Create(int imageId, params (string Name, object Value)[] values)
        {
            return new ImageSnapshot(imageId, "LIGHT", $@"C:\Images\image_{imageId}.fits",
                values.ToDictionary(v => v.Name, v => v.Value));
        }
    }

    internal static class TaskExtensions
    {
        /// <summary>Fails the test instead of hanging if the task does not complete in time.</summary>
        public static async Task<T> WithTimeout<T>(this Task<T> task, int milliseconds = 5000)
        {
            var completed = await Task.WhenAny(task, Task.Delay(milliseconds));
            if (completed != task)
            {
                throw new TimeoutException($"Task did not complete within {milliseconds} ms.");
            }

            return await task;
        }
    }
}
