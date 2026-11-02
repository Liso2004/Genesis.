# Local transport repair
Based on MCP for Unity v10.0.0 (7b7db7b31f4e).
This embedded package takes precedence over the cached Git package.
The receive loop queues editor-dependent registration and commands in order,
instead of awaiting them before the next socket receive. This allows protocol
ping/pong frames to be processed while editor work is delayed. Queued work is
cancelled with its connection; exceptions are observed without blocking later work.
Do not overwrite this package on update without checking whether upstream includes
the same fix. No scene or gameplay changes are part of this repair.

Regression tests (.NET 9): dotnet run --project Packages/com.coplaydev.unity-mcp/Tests~/TransportQueue/TransportTests.csproj
Checks: nonblocking caller, FIFO execution, connection cancellation, and exception recovery.

The loopback regression uses a real ClientWebSocket and protocol ping frame. It reproduces missing pong replies while dispatch is awaited, then verifies that queued dispatch allows the pong to arrive before the simulated editor command completes. All four regression checks passed on 2026-09-25.
This repair does not make Unity executable during a domain reload; callers must still retry while Unity compiles or reloads.

Live verification: Unity compilation succeeded; Genesis reconnected after domain reload; editor state returned ready_for_tools=true with no blocking reasons; project/info returned Genesis on Unity 6000.0.35f1. Scenes and gameplay files were not edited.
