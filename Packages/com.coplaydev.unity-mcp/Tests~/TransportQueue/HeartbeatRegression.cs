using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MCPForUnity.Editor.Services.Transport.Transports;

static class HeartbeatRegression
{
    public static async Task Run()
    {
        if (await Probe(false)) throw new Exception("Baseline unexpectedly serviced ping during awaited command");
        if (!await Probe(true)) throw new Exception("Queued dispatch failed to service WebSocket ping");
        Console.WriteLine("PASS: real WebSocket ping stalls with awaited dispatch, succeeds with queued dispatch");
    }

    private static async Task<bool> Probe(bool queued)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var workSeen = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientTask = Task.Run(async () =>
        {
            using var socket = new ClientWebSocket();
            await socket.ConnectAsync(new Uri("ws://127.0.0.1:" + port + "/"), CancellationToken.None);
            var queue = new EditorMessageQueue(ex => throw ex);
            var buffer = new byte[64];
            await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            workSeen.SetResult(true);
            if (queued) _ = queue.Enqueue(() => release.Task, CancellationToken.None);
            else await release.Task;
            // ClientWebSocket processes protocol ping/pong inside ReceiveAsync.
            await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        });
        using var peer = await listener.AcceptTcpClientAsync();
        using var stream = peer.GetStream();
        var header = new StringBuilder();
        var b = new byte[1];
        while (!header.ToString().EndsWith("\r\n\r\n"))
        {
            await stream.ReadExactlyAsync(b);
            header.Append((char)b[0]);
        }
        string key = null;
        foreach (string line in header.ToString().Split("\r\n"))
            if (line.StartsWith("Sec-WebSocket-Key:", StringComparison.OrdinalIgnoreCase))
                key = line.Substring(line.IndexOf(':') + 1).Trim();
        var accept = Convert.ToBase64String(SHA1.HashData(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
        await stream.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: " + accept + "\r\n\r\n"));
        await stream.WriteAsync(new byte[] { 0x81, 4, 119, 111, 114, 107 });
        await workSeen.Task.WaitAsync(TimeSpan.FromSeconds(20));
        await stream.WriteAsync(new byte[] { 0x89, 1, 42 });
        bool pong = false;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(queued ? 10 : 2));
            var frame = new byte[2];
            await stream.ReadExactlyAsync(frame, timeout.Token);
            pong = (frame[0] & 15) == 10;
        }
        catch (OperationCanceledException) { }
        finally
        {
            await stream.WriteAsync(new byte[] { 0x81, 4, 100, 111, 110, 101 });
            release.TrySetResult(true);
        }
        await clientTask.WaitAsync(TimeSpan.FromSeconds(20));
        return pong;
    }
}
