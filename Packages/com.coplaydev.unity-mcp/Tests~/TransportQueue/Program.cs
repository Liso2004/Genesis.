using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MCPForUnity.Editor.Services.Transport.Transports;

static class Program
{
    static async Task Main()
    {
        await HeartbeatRegression.Run();
        var errors = new List<Exception>();
        var queue = new EditorMessageQueue(errors.Add);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var order = new List<int>();
        var first = queue.Enqueue(async () => { order.Add(1); await release.Task; order.Add(2); }, CancellationToken.None);
        var second = queue.Enqueue(() => { order.Add(3); return Task.CompletedTask; }, CancellationToken.None);
        if (first.IsCompleted || second.IsCompleted || order.Count != 1)
            throw new Exception("Queue did not preserve pending editor work");
        // The caller (receive loop) can continue processing heartbeat frames.
        int heartbeats = 0;
        for (int i = 0; i < 5; i++) { await Task.Delay(20); heartbeats++; }
        if (heartbeats != 5 || first.IsCompleted) throw new Exception("Heartbeat servicing blocked");
        release.SetResult(true);
        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(2));
        if (string.Join(",", order) != "1,2,3") throw new Exception("Commands reordered");
        Console.WriteLine("PASS: delayed command does not block caller; commands stay ordered");

        var held = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cts = new CancellationTokenSource();
        var waiting = queue.Enqueue(() => held.Task, cts.Token);
        bool staleRan = false;
        var stale = queue.Enqueue(() => { staleRan = true; return Task.CompletedTask; }, cts.Token);
        cts.Cancel();
        held.SetResult(true);
        await Task.WhenAll(waiting, stale).WaitAsync(TimeSpan.FromSeconds(2));
        if (staleRan) throw new Exception("Cancelled connection executed queued work");
        Console.WriteLine("PASS: cancelled connection skips pending commands");

        await queue.Enqueue(() => throw new InvalidOperationException("expected"), CancellationToken.None);
        bool nextRan = false;
        await queue.Enqueue(() => { nextRan = true; return Task.CompletedTask; }, CancellationToken.None);
        if (errors.Count != 1 || !nextRan) throw new Exception("Failure poisoned command queue");
        Console.WriteLine("PASS: exceptions are observed and later messages still execute");
    }
}
