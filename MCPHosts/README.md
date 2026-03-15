# MCP Hosts - Proof of Concept

A collection of Model Context Protocol (MCP) host implementations demonstrating different approaches to building MCP servers in .NET. This project showcases how to create MCP hosts that can be integrated with LLM clients like Claude Code, Cursor, and other MCP-compliant tools.

## Overview

This solution contains multiple implementations of MCP hosts, each demonstrating a different approach to hosting MCP servers:

- **BareBones** - A minimal stdio-based MCP host implementation using raw JSON-RPC over standard input/output

## What is MCP (Model Context Protocol)?

The Model Context Protocol (MCP) is an open standard that enables LLM clients to connect to external tools, resources, and prompts through a standardized interface. Think of it as a universal API protocol that allows AI assistants to interact with your custom applications and services.

### Key Concepts

```
┌─────────────────────────────────────────────────────────────────────┐
│                        MCP Architecture                              │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│   ┌──────────┐     JSON-RPC      ┌──────────────────────┐          │
│   │   LLM    │◄─────────────────►│    MCP Host          │          │
│   │  Client  │                   │  (Your Application)  │          │
│   │          │                   │                      │          │
│   │ Claude   │                   │  ┌────────────────┐  │          │
│   │ Cursor   │                   │  │   Tools        │  │          │
│   │ OpenAI   │                   │  │   - get_time   │  │          │
│   │ Agent    │                   │  │   - echo       │  │          │
│   └──────────┘                   │  └────────────────┘  │          │
│                                  │                      │          │
│                                  │  ┌────────────────┐  │          │
│                                  │  │   Resources    │  │          │
│                                  │  └────────────────┘  │          │
│                                  │                      │          │
│                                  │  ┌────────────────┐  │          │
│                                  │  │   Prompts      │  │          │
│                                  │  └────────────────┘  │          │
│                                  └──────────────────────┘          │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### How MCP Works

1. **Connection**: The LLM client starts your MCP host process (typically via stdio)
2. **Initialization**: The client sends an `initialize` request to establish protocol version and capabilities
3. **Discovery**: The client queries available tools using `tools/list`
4. **Execution**: The client calls tools using `tools/call` with parameters
5. **Response**: The host returns results in a structured JSON format

### JSON-RPC Protocol

MCP uses JSON-RPC 2.0 for communication. Each message is a JSON object with the following structure:

**Request Format:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/list",
  "params": { }
}
```

**Response Format:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "tools": [...]
  }
}
```

**Error Format:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "error": {
    "message": "Error description"
  }
}
```

## Solution Structure

```
MCPHosts/
├── MCPHosts.sln                    # Solution file
├── MCPHosts.BareBones/             # Bare-bones stdio implementation
│   ├── Program.cs                  # Main MCP host implementation
│   └── MCPHosts.BareBones.csproj   # Project file
├── Videos/                         # Demo videos
│   └── MCPHost-Demo.mp4           # Local demo walkthrough
├── log.txt                         # JSON-RPC communication log (generated at runtime)
└── README.md                       # This file
```

**Demo Video**: [Watch the testing walkthrough with Claude Code](https://somup.com/cOeo2lVcXhM) (3:21 min)

## MCPHosts.BareBones - Stdio Implementation

The BareBones project demonstrates the simplest possible MCP host using standard input/output (stdio) for communication. This approach is ideal for:

- Learning the MCP protocol fundamentals
- Quick prototyping
- Simple tool implementations
- Understanding the JSON-RPC flow

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Stdio Communication Flow                         │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  LLM Client (Claude Code)         MCP Host (MCPHosts.BareBones)     │
│  ┌──────────────────┐            ┌────────────────────────┐         │
│  │                  │            │                        │         │
│  │  1. Start Process│───────────►│  Process Started       │         │
│  │                  │            │  Console.In/Out        │         │
│  └──────────────────┘            │                        │         │
│          │                        └────────────────────────┘         │
│          │                                   │                       │
│          │ 2. initialize                    │                       │
│          ├─────────────────────────────────►│                       │
│          │                                   │  Parse JSON           │
│          │                                   │  Extract method       │
│          │ 3. capabilities ◄────────────────│                       │
│          │◄─────────────────────────────────┤                       │
│          │                                   │                       │
│          │ 4. tools/list                    │                       │
│          ├─────────────────────────────────►│                       │
│          │                                   │  Build tool list      │
│          │ 5. available tools ◄─────────────│                       │
│          │◄─────────────────────────────────┤                       │
│          │                                   │                       │
│          │ 6. tools/call                    │                       │
│          ├─────────────────────────────────►│                       │
│          │    {"name":"get_time"}           │  Execute tool         │
│          │                                   │  Return result        │
│          │ 7. tool result ◄─────────────────│                       │
│          │◄─────────────────────────────────┤                       │
│          │                                   │                       │
│          │ 8. Repeat steps 6-7              │                       │
│          │    for each tool call            │                       │
│          ├─────────────────────────────────►│                       │
│          │◄─────────────────────────────────┤                       │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### Implementation Details

#### Main Communication Loop (Program.cs:26-111)

The host runs in an infinite loop, processing JSON-RPC requests from standard input:

```csharp
while (true)
{
    // 1. Read incoming JSON-RPC request
    var line = await reader.ReadLineAsync();

    // 2. Clean UTF-8 BOM if present
    if (!string.IsNullOrEmpty(line) && line.StartsWith("\uFEFF"))
    {
        line = line.TrimStart('\uFEFF');
    }

    // 3. Parse JSON and extract ID and method
    var json = JsonNode.Parse(line);
    var id = json?["id"];
    var method = json?["method"]?.GetValue<string>();

    // 4. Route to appropriate handler
    switch (method)
    {
        case "initialize":
            // Respond with protocol version and capabilities
            response = CreateInitializeResponse(id);
            break;
        case "ping":
            // Simple health check
            response = CreatePingResponse(id);
            break;
        case "tools/list":
            // Return available tools
            response = ToolsList(id);
            break;
        case "tools/call":
            // Execute a tool
            response = ToolsCall(id, json?["params"] as JsonObject);
            break;
    }

    // 5. Send response to standard output
    await writer.WriteLineAsync(response.ToJsonString());
    await writer.FlushAsync();
}
```

#### Supported Methods

| Method | Purpose | Response |
|--------|---------|----------|
| `initialize` | Handshake to establish protocol version | Server capabilities and info |
| `ping` | Health check | "pong" |
| `tools/list` | Discover available tools | Array of tool definitions |
| `tools/call` | Execute a specific tool | Tool execution result or error |

#### Available Tools

**1. get_time**
- **Description**: Returns the current server time in UTC (ISO 8601 format)
- **Parameters**: None
- **Example Response**:
  ```json
  {
    "content": [
      {
        "type": "text",
        "text": "2025-03-15T14:30:45.1234567Z"
      }
    ]
  }
  ```

**2. echo**
- **Description**: Echoes back the input string
- **Parameters**:
  - `arguments` (string, required): The string to echo back
- **Example Response**:
  ```json
  {
    "content": [
      {
        "type": "text",
        "text": "Hello, World!"
      }
    ]
  }
  ```

### Real-World Log Example

Here's an actual excerpt from `log.txt` showing the JSON-RPC conversation when Claude Code connects:

```json
=====================
{"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{"roots":{}},"clientInfo":{"name":"claude-code","version":"2.0.69"}},"jsonrpc":"2.0","id":0}
{"jsonrpc":"2.0","id":"0","result":{"protocolVersion":"2025-06-18","capabilities":{"tools":{"listChanged":false}},"serverInfo":{"name":"barebones-mcp","version":"1.0"}}}
=====================
{"method":"notifications/initialized","jsonrpc":"2.0"}
=====================
{"method":"tools/list","jsonrpc":"2.0","id":1}
{"jsonrpc":"2.0","id":"1","result":{"tools":[{"name":"get_time","description":"Returns the current server time","inputSchema":{"type":"object","properties":{},"required":[]}},{"name":"echo","description":"Echoes back the input","inputSchema":{"type":"object","properties":{"arguments":{"type":"string","description":"The string to echo back"}},"required":["arguments"]}}]}}
=====================
```

**Flow Breakdown:**
1. **Initialize**: Claude Code sends its protocol version and capabilities
2. **Initialize Response**: Server acknowledges with protocol version 2025-06-18
3. **Initialized Notification**: Client confirms initialization is complete
4. **Tools List**: Client requests available tools
5. **Tools Response**: Server returns `get_time` and `echo` tools with their schemas

### Request/Response Flow Example

Here's a complete example of how a tool call flows through the system:

```
Client: {"method":"tools/call","params":{"name":"get_time","arguments":{}},"jsonrpc":"2.0","id":2}

Host parses: method = "tools/call"
            tool  = "get_time"
            args  = {}

Host executes: DateTime.UtcNow.ToString("o")

Host responds: {"jsonrpc":"2.0","id":"2","result":{"content":[{"type":"text","text":"2025-03-15T14:30:45.1234567Z"}]}}
```

## How to Build and Run

### Prerequisites

- .NET 9.0 SDK or later
- An MCP-compatible client (Claude Code, Cursor, etc.)
- Git (for cloning)

### Initial Git Setup

The project includes a comprehensive `.gitignore` file that excludes build artifacts, user-specific settings, and runtime logs. Example configuration templates are provided:

```bash
# Copy example configurations
cp .mcp.json.example .mcp.json
cp .claude/settings.json.example .claude/settings.local.json

# Customize the paths in .mcp.json for your environment
```

**Files excluded from Git:**
- Build artifacts (`bin/`, `obj/`, `.vs/`)
- Runtime logs (`log.txt`)
- User-specific configurations (`.claude/settings.local.json`, `.mcp.json`)
- IDE files (`.vscode/`, `.idea/`)
- NuGet packages

### Building the Project

```bash
# Navigate to the solution directory
cd I:\Yog\Training\QuestpondAI\MyCode\MCPHosts

# Build the solution
dotnet build

# Or build just the BareBones project
cd MCPHosts.BareBones
dotnet build
```

### Running Standalone (for manual testing)

```bash
# Run the executable directly
cd MCPHosts.BareBones\bin\Debug\net9.0
.\MCPHosts.BareBones.exe

# Or use dotnet run
dotnet run --project MCPHosts.BareBones
```

To test manually, you can send JSON-RPC messages via stdin:
```bash
echo '{"method":"ping","jsonrpc":"2.0","id":1}' | .\MCPHosts.BareBones.exe
```

## Demo Video

[Watch the complete walkthrough of testing this MCP host with Claude Code](https://somup.com/cOeo2lVcXhM) (3:21 minutes)

The demo covers:
- Starting the MCP host with Claude Code
- Verifying the connection and available tools
- Using the `get_time` and `echo` tools
- Inspecting the JSON-RPC communication log

## Testing with Claude Code

Claude Code is an MCP-compatible client that can connect to your MCP host. Here's how to set it up:

### Step 1: Create MCP Configuration

Copy the example configuration and customize the path:

```bash
# Copy the example template
cp .mcp.json.example .mcp.json
```

Then edit `.mcp.json` to update the path to your local project:

```json
{
  "mcpServers": {
    "barebones-host": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "I:\\Yog\\Training\\QuestpondAI\\MyCode\\MCPHosts\\MCPHosts.BareBones"
      ],
      "env": {}
    }
  }
}
```

**Configuration Parameters:**
- `type`: Must be "stdio" for standard input/output communication
- `command`: The executable to run (e.g., "dotnet" or full path to .exe)
- `args`: Command-line arguments to pass to the executable
- `env`: Optional environment variables

### Step 2: Configure Claude Code Permissions

Copy the example settings:

```bash
# Copy the example template
mkdir .claude
cp .claude/settings.json.example .claude/settings.local.json
```

The template includes all necessary permissions. If you need to customize, edit `.claude/settings.local.json`:

```json
{
  "permissions": {
    "allow": [
      "Bash(dotnet:*)",
      "Bash(.\\MCPHosts.BareBones.exe:*)",
      "mcp__barebones-host__get_time",
      "mcp__barebones-host__echo"
    ]
  },
  "enableAllProjectMcpServers": false
}
```

**Permission Format:**
- `mcp__<server-name>__<tool-name>`: Grant access to specific tools
- Bash permissions: Allow starting the MCP host process

### Step 3: Start Claude Code

```bash
# Navigate to your project directory
cd I:\Yog\Training\QuestpondAI\MyCode\MCPHosts

# Start Claude Code
claude
```

### Step 4: Verify MCP Connection

When Claude Code starts, it will automatically connect to your MCP server. You can verify this by:

1. **Checking the status line** - Look for connected MCP servers
2. **Asking Claude** - Type "What tools are available?" or use the `/mcp` command
3. **Checking the log** - The `log.txt` file will show the JSON-RPC exchanges

### Step 5: Use the Tools

Once connected, you can interact with your MCP tools through natural language:

```
You: What time is it?

Claude: I'll check the server time for you.
[Calls get_time tool]

Claude: The current server time is 2025-03-15T14:30:45.1234567Z
```

```
You: Please echo "Hello from MCP!"

Claude: I'll echo that message for you.
[Calls echo tool with arguments="Hello from MCP!"]

Claude: The echo result is: Hello from MCP!
```

### Testing Other MCP Clients

The same MCP host can work with other MCP-compatible clients:

#### Cursor IDE

1. Open Cursor Settings
2. Navigate to "MCP Servers"
3. Add your server configuration:
   ```json
   {
     "barebones-host": {
       "command": "dotnet",
       "args": ["run", "--project", "I:\\Yog\\Training\\QuestpondAI\\MyCode\\MCPHosts\\MCPHosts.BareBones"]
     }
   }
   ```

#### Other Clients

Any client that supports the MCP protocol and stdio transport can connect to this host. The configuration pattern is similar:
- Specify the transport type (stdio)
- Provide the command to start your host
- Optionally specify environment variables

## Debugging

### Viewing JSON-RPC Messages

The BareBones implementation logs all JSON-RPC communication to `log.txt`:

```
=====================
{"method":"initialize",...,"id":0}
{"jsonrpc":"2.0","id":"0","result":{...}}
=====================
{"method":"tools/list",...}
{"jsonrpc":"2.0","id":"1","result":{...}}
=====================
```

### Common Issues

**Issue**: MCP server not starting
- **Solution**: Check that the path in `.mcp.json` is correct and absolute
- **Solution**: Ensure .NET SDK is installed and in PATH

**Issue**: Tools not appearing in Claude Code
- **Solution**: Verify `enableAllProjectMcpServers` is set to `false` or the server name matches
- **Solution**: Check permissions in `settings.local.json`

**Issue**: Empty responses
- **Solution**: Ensure JSON responses end with newlines for proper stdio communication
- **Solution**: Check for UTF-8 BOM issues in the input stream

**Issue**: Process already running
```bash
# On Windows, find and kill the process
tasklist | findstr MCPHosts
taskkill /PID <pid> /F
```

### Debug Mode

To see detailed logging, modify `Program.cs` to add more detail to the log file:

```csharp
System.IO.File.AppendAllText("./log.txt", $"[{DateTime.Now}] {line}{Environment.NewLine}");
```

## MCP Protocol Specification

### Protocol Version

Current implementation uses: `2025-06-18`

### Required Methods

An MCP host must implement at minimum:

1. **initialize** - Handshake and capability exchange
2. **tools/list** - Return available tools
3. **tools/call** - Execute a tool

### Optional Methods

- **ping** - Health check
- **resources/list** - Return available resources
- **resources/read** - Read a resource
- **prompts/list** - Return available prompts
- **prompts/get** - Get a prompt

### Message Format

All messages must:
- Include `"jsonrpc": "2.0"`
- Include an `"id"` field for request/response correlation
- Include a `"method"` field (requests only)
- Include either `"result"` or `"error"` (responses only)

### Tool Schema Format

Tools are defined with the following schema:

```json
{
  "name": "tool_name",
  "description": "Human-readable description",
  "inputSchema": {
    "type": "object",
    "properties": {
      "param1": {
        "type": "string",
        "description": "Parameter description"
      }
    },
    "required": ["param1"]
  }
}
```

## Extending the BareBones Host

### Adding a New Tool

1. **Add to ToolsList** (Program.cs:118-163):

```csharp
new JsonObject
{
    ["name"] = "my_tool",
    ["description"] = "Does something useful",
    ["inputSchema"] = new JsonObject
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["input"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Input parameter"
            }
        },
        ["required"] = new JsonArray
        {
            "input"
        }
    }
}
```

2. **Add case to ToolsCall** (Program.cs:191-226):

```csharp
"my_tool" => new JsonObject
{
    ["jsonrpc"] = "2.0",
    ["id"] = id.DeepClone(),
    ["result"] = new JsonObject
    {
        ["content"] = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "text",
                ["text"] = "Tool execution result"
            }
        }
    }
}
```

### Adding Resources

Resources represent data that the LLM can read:

```csharp
case "resources/list":
    response = ResourcesList(id);
    break;

case "resources/read":
    response = ResourcesRead(id, json?["params"] as JsonObject);
    break;
```

### Adding Prompts

Prompts provide pre-defined templates for the LLM:

```csharp
case "prompts/list":
    response = PromptsList(id);
    break;

case "prompts/get":
    response = PromptsGet(id, json?["params"] as JsonObject);
    break;
```

## Future Implementations

This proof of concept will expand to include:

- **SSE Implementation** - Server-Sent Events for web-based transport
- **HTTP Implementation** - REST-based MCP hosting
- **WebSocket Implementation** - Real-time bidirectional communication
- **Advanced Features** - Resources, prompts, and full MCP specification

## References

- [MCP Official Documentation](https://modelcontextprotocol.io/)
- [JSON-RPC 2.0 Specification](https://www.jsonrpc.org/specification)
- [Claude Code Documentation](https://code.anthropic.com/)
- [.NET 9.0 Documentation](https://learn.microsoft.com/en-us/dotnet/core/)

## License

This is a proof of concept project for educational purposes.

## Contributing

This is a personal learning project. Feel free to fork and experiment with your own MCP implementations!

---

**Last Updated**: March 2025
**.NET Version**: 9.0
**MCP Protocol Version**: 2025-06-18
