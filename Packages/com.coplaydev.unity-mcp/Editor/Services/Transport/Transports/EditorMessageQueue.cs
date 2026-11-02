using System;
using System.Threading;
using System.Threading.Tasks;

namespace MCPForUnity.Editor.Services.Transport.Transports
{
    // Written by the single WebSocket receive loop. Editor work remains ordered,
    // while socket reads continue servicing protocol and application heartbeats.
    internal sealed class EditorMessageQueue
    {
        private Task _tail = Task.CompletedTask;
        private readonly Action<Exception> _onError;

        public EditorMessageQueue(Action<Exception> onError) { _onError = onError; }

        public Task Enqueue(Func<Task> action, CancellationToken token)
        {
            return _tail = RunAsync(_tail, action, token);
        }

        private async Task RunAsync(Task previous, Func<Task> action, CancellationToken token)
        {
            try
            {
                await previous.ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                await action().ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested) _onError(ex);
            }
        }
    }
}
