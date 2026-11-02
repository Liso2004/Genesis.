"""Local MCP validation client; keeps evidence out of Unity's disposable Temp folder."""
import json, sys, urllib.request

sys.stdout.reconfigure(encoding="utf-8")
url = "http://127.0.0.1:8080/mcp"
session = None
sequence = 0

def call(method, params):
    global session, sequence
    sequence += 1
    headers = {"Content-Type": "application/json", "Accept": "application/json, text/event-stream"}
    if session:
        headers["Mcp-Session-Id"] = session
    payload = {"jsonrpc": "2.0", "id": sequence, "method": method, "params": params}
    with urllib.request.urlopen(urllib.request.Request(url, json.dumps(payload).encode(), headers), timeout=180) as response:
        session = response.headers.get("mcp-session-id", session)
        raw = response.read().decode()
    if raw.startswith("{"):
        return json.loads(raw)
    for line in raw.splitlines():
        if line.startswith("data:"):
            value = json.loads(line[5:])
            if value.get("id") == sequence:
                return value
    return {"raw": raw[:2000]}

def strip_images(value):
    if isinstance(value, dict):
        return {k: strip_images(v) for k, v in value.items() if not (value.get("type") == "image" and k == "data")}
    if isinstance(value, list):
        return [strip_images(v) for v in value]
    return value

call("initialize", {"protocolVersion": "2024-11-05", "capabilities": {}, "clientInfo": {"name": "genesis-validation", "version": "1"}})
source = sys.stdin.read()
if "--code" in sys.argv:
    requests = [{"params": {"name": "execute_custom_tool", "arguments": {"tool_name": "execute_code", "parameters": {"action": "execute", "code": source}}}}]
else:
    requests = json.loads(source)
    if not isinstance(requests, list):
        requests = [requests]
for request in requests:
    print(json.dumps(strip_images(call(request.get("method", "tools/call"), request.get("params", {}))), ensure_ascii=True))
