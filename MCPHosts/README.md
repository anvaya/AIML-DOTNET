# MCP Hosts - Proof of Concept

A collection of Model Context Protocol (MCP) host implementations demonstrating different approaches to building MCP servers in .NET. This project showcases how to create MCP hosts that can be integrated with LLM clients like Claude Code, Cursor, and other MCP-compliant tools.

## Overview

This solution contains multiple implementations of MCP hosts and clients, each demonstrating a different approach to building MCP servers in .NET:

- **BareBones** - A minimal stdio-based MCP host implementation using raw JSON-RPC over standard input/output
- **Server** - A production-ready MCP host using the `ModelContextProtocol.Server` library with attribute-based tool registration
- **Clients** - A complete MCP client demo showcasing how to connect to MCP servers and integrate them with LLMs (Ollama) using Microsoft.Extensions.AI

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
│   ├── Program.cs                  # Raw JSON-RPC implementation
│   └── MCPHosts.BareBones.csproj   # Project file
├── MCPHosts.Server/                # Production-ready MCP server
│   ├── Program.cs                  # Server library implementation
│   └── MCPHosts.Server.csproj      # Project file with ModelContextProtocol.Server
├── MCPHosts.Clients/               # MCP client demo
│   ├── Program.cs                  # Client with Ollama integration
│   └── MCPHosts.Clients.csproj     # Project file with OllamaSharp & Microsoft.Extensions.AI
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

# Build the entire solution
dotnet build

# Or build individual projects
dotnet build MCPHosts.BareBones
dotnet build MCPHosts.Server
dotnet build MCPHosts.Clients
```

### Running Projects

#### MCPHosts.BareBones (Manual Testing)

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

#### MCPHosts.Server (Production)

```bash
dotnet run --project MCPHosts.Server
```

#### MCPHosts.Clients (Demo with Ollama)

```bash
# Ensure Ollama is running first
ollama serve

# In another terminal, run the client
dotnet run --project MCPHosts.Clients
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

---

## MCPHosts.Server - Production-Ready Implementation

The Server project demonstrates the recommended approach for building MCP hosts in .NET using the official `ModelContextProtocol.Server` library. This approach provides:

- **Declarative tool registration** using attributes
- **Clean, maintainable code** without manual JSON-RPC handling
- **Built-in protocol compliance** with MCP specification
- **Integration with .NET hosting abstractions** (IHost, DI, logging)
- **Automatic schema generation** from method signatures

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                    ModelContextProtocol.Server                       │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  LLM Client (Claude Code)         MCPHosts.Server                    │
│  ┌──────────────────┐            ┌────────────────────────┐         │
│  │                  │            │                        │         │
│  │  1. Start Process│───────────►│  Host.CreateApplicationBuilder│         │
│  │                  │            │                        │         │
│  └──────────────────┘            │  .AddMcpServer()       │         │
│          │                        │  .WithStdioTransport()│         │
│          │ 2. initialize          │  .WithTools<T>()      │         │
│          ├──────────────────────────────────────────────►│         │
│          │◄──────────────────────────────────────────────┤         │
│          │                        │                        │         │
│          │ 3. tools/list          │  [McpServerToolType]  │         │
│          ├──────────────────────────────────────────────►│  MCPTools│         │
│          │◄──────────────────────────────────────────────┤  Class   │         │
│          │                        │                        │         │
│          │ 4. tools/call          │  [McpServerTool]      │         │
│          ├──────────────────────────────────────────────►│  Methods │         │
│          │◄──────────────────────────────────────────────┤         │
│          │                        │                        │         │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### Implementation Details

#### Host Setup (Program.cs:13-29)

The server uses .NET's `HostApplicationBuilder` for a clean, configurable setup:

```csharp
HostApplicationBuilder? builder = Host.CreateApplicationBuilder(args);

// Configure logging to stderr (doesn't interfere with stdio)
builder.Logging.ClearProviders();
builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Register MCP server with stdio transport and tools
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<MCPTools>();

await builder.Build().RunAsync();
```

#### Tool Registration with Attributes (Program.cs:33-49)

Tools are defined as simple C# methods with attributes:

```csharp
[McpServerToolType]  // Marks this class as containing MCP tools
public class MCPTools
{
    [McpServerTool]  // Marks this method as an MCP tool
    [Description("Gets the current server time.")]
    public string getServerTime()
    {
        return DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
    }

    [McpServerTool]
    [Description("Echoes the provided message back to the caller.")]
    public string Echo(string message)
    {
        return $"Echo: {message}";
    }
}
```

### Comparison: BareBones vs Server

| Aspect | BareBones | Server (Recommended) |
|--------|-----------|---------------------|
| **Lines of Code** | ~200 LOC | ~50 LOC |
| **JSON Handling** | Manual parsing with JsonNode | Automatic via library |
| **Protocol Compliance** | Manual implementation | Built-in compliance |
| **Extensibility** | Requires manual updates | Just add methods with attributes |
| **Error Handling** | Manual try-catch | Built into framework |
| **Dependency Injection** | Not supported | Full DI support |
| **Logging** | Manual file logging | Structured logging with ILogger |
| **Tool Schema** | Manual JSON schema | Auto-generated from signatures |

### Available Tools

**1. getServerTime**
- **Description**: Gets the current server time in UTC (ISO 8601 format)
- **Parameters**: None
- **Returns**: String representation of DateTime.UtcNow

**2. Echo**
- **Description**: Echoes the provided message back to the caller
- **Parameters**:
  - `message` (string, required): The message to echo
- **Returns**: The message prefixed with "Echo: "

### Running MCPHosts.Server with Claude Code

Update your `.mcp.json` configuration:

```json
{
  "mcpServers": {
    "server-host": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "I:\\Yog\\Training\\QuestpondAI\\MyCode\\MCPHosts\\MCPHosts.Server"
      ],
      "env": {}
    }
  }
}
```

Update `.claude/settings.local.json` permissions:

```json
{
  "permissions": {
    "allow": [
      "Bash(dotnet:*)",
      "mcp__server-host__getServerTime",
      "mcp__server-host__echo"
    ]
  }
}
```

---

## MCPHosts.Clients - MCP Client with LLM Integration

The Clients project demonstrates how to build an MCP client that connects to an MCP server and integrates it with an LLM (Ollama) for automated tool calling. This showcases the **client-side** of the MCP protocol, complementing the server implementations.

### Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    MCP Client with LLM Integration                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│   User          ChatClientBuilder         McpClient          MCP Server │
│    │                    │                     │                  │      │
│    │  "What time is it?"│                     │                  │      │
│    ├───────────────────►│                     │                  │      │
│    │                    │  ListToolsAsync()   │                  │      │
│    │                    ├────────────────────►│                  │      │
│    │                    │  [getServerTime]    │                  │      │
│    │                    │◄────────────────────┤                  │      │
│    │                    │                     │                  │      │
│    │                    │  GetResponseAsync() │                  │      │
│    │                    │  with Tools         │                  │      │
│    │                    ├───────────────────────────────────────►│      │
│    │                    │                     │  execute tool    │      │
│    │                    │◄───────────────────────────────────────┤      │
│    │                    │                     │                  │      │
│    │◄───────────────────┤  "The time is..."    │                  │      │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Implementation Details

#### Client Setup (Program.cs:17-26)

Create a stdio transport to connect to the MCP server:

```csharp
var transport = new StdioClientTransport(
    new()
    {
        Command = "dotnet",
        Arguments = ["run", "--project", "I:\\Yog\\Training\\QuestpondAI\\MyCode\\MCPHosts\\MCPHosts.Server"],
        Name = "MCPHosts.Server",
        WorkingDirectory = "I:\\Yog\\Training\\QuestpondAI\\MyCode\\MCPHosts\\MCPHosts.Server"
    });

McpClient mcpClient = await McpClient.CreateAsync(transport);
```

#### LLM Integration with Ollama (Program.cs:28-34)

Connect to Ollama and enable automatic function invocation:

```csharp
var ollama = new OllamaApiClient("http://localhost:11434", "gpt-oss:20b-cloud");

var chatClient = new ChatClientBuilder(ollama)
    .UseFunctionInvocation()  // Auto-calls MCP tools
    .Build();
```

#### Tool Discovery and Usage (Program.cs:36-47)

```csharp
// Discover available tools
IList<McpClientTool> tools = await mcpClient.ListToolsAsync();
foreach (McpClientTool tool in tools)
{
    Console.WriteLine($"{tool}");
}

// Send request with automatic tool invocation
var response = await chatClient.GetResponseAsync(
    "What is the server time?",
    new ChatOptions { Tools = [.. tools] }
);
```

### How It Works

1. **Client Initialization**: The stdio transport starts the MCP server process (MCPHosts.Server)
2. **Tool Discovery**: `ListToolsAsync()` queries the server for available tools
3. **LLM Request**: User sends a natural language query to the LLM
4. **Automatic Function Calling**: The `ChatClientBuilder` with `UseFunctionInvocation()` automatically:
   - Analyzes the user's query
   - Selects appropriate MCP tools
   - Calls the tools via the MCP client
   - Incorporates tool results into the final response
5. **Response**: The LLM returns a natural language response incorporating tool results

### Dependencies

- **ModelContextProtocol** (v1.1.0) - Official MCP client library
- **OllamaSharp** (v5.4.23) - Ollama client for .NET
- **Microsoft.Extensions.AI** (v10.4.0) - AI abstractions for function calling

### Prerequisites for Running

1. **Ollama Installation**: Install [Ollama](https://ollama.com/)
2. **Pull a Model**: `ollama pull gpt-oss:20b-cloud` (or any model)
3. **Start Ollama**: Ensure Ollama is running on `http://localhost:11434`
4. **Update Paths**: Edit `Program.cs:21-23` to match your local paths

### Example Run

```bash
cd MCPHosts.Clients
dotnet run
```

**Output:**
```
Available tools:
McpClientTool { Name = getServerTime, Description = Gets the current server time. }
McpClientTool { Name = echo, Description = Echoes the provided message back to the caller. }

Response: The current server time is 2025-03-15T14:30:45.1234567Z
```

### Key Takeaways

- **MCP is bidirectional**: You can be both a server (providing tools) and a client (consuming tools)
- **LLM Integration**: Microsoft.Extensions.AI provides seamless integration between LLMs and MCP tools
- **Automatic Tool Calling**: The `UseFunctionInvocation()` middleware handles tool selection and execution automatically
- **Vendor Agnostic**: This pattern works with any LLM that supports function calling (OpenAI, Azure OpenAI, etc.)

---

## Future Implementations

This proof of concept will expand to include:

- **SSE Implementation** - Server-Sent Events for web-based transport
- **HTTP Implementation** - REST-based MCP hosting
- **WebSocket Implementation** - Real-time bidirectional communication
- **Advanced Features** - Resources, prompts, and full MCP specification
- **Multi-Server Client** - Demonstrating clients that connect to multiple MCP servers

## References

- [MCP Official Documentation](https://modelcontextprotocol.io/)
- [ModelContextProtocol .NET Library](https://www.nuget.org/packages/ModelContextProtocol/)
- [JSON-RPC 2.0 Specification](https://www.jsonrpc.org/specification)
- [Claude Code Documentation](https://code.anthropic.com/)
- [.NET 9.0 Documentation](https://learn.microsoft.com/en-us/dotnet/core/)
- [Ollama](https://ollama.com/) - Local LLM runtime
- [OllamaSharp](https://github.com/WaitForOllama/OllamaSharp) - .NET client for Ollama
- [Microsoft.Extensions.AI](https://learn.microsoft.com/en-us/dotnet/ai/) - AI abstractions for .NET

## License

This is a proof of concept project for educational purposes.

## Contributing

This is a personal learning project. Feel free to fork and experiment with your own MCP implementations!

---

**Last Updated**: March 2026
**.NET Version**: 9.0
**MCP Protocol Version**: 2025-06-18
**ModelContextProtocol Package**: 1.1.0
