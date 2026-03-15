using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;    

namespace MCPHosts.BareBones
{
    /*
     * LLM Client
       (Claude / Cursor / OpenAI agent)
        │
        │ JSON-RPC
        ▼
        MCP Host (your .NET app)
        │
        ├── Tools
        ├── Resources
        └── Prompts
    */

    /// <summary>
    /// A simple JSON-RPC server that listens for requests from an LLM client and responds with tool information or executes tools based on the request. 
    /// This is a bare-bones implementation for demonstration purposes.
    /// </summary>
    internal class Program
    {
        static async Task Main()
        {
            var reader = Console.In; // Read from standard input (LLM client)
            var writer = Console.Out; // Write to standard output (LLM client)

            // Main loop to process incoming JSON-RPC requests
            while (true)
            {
                var line = await reader.ReadLineAsync(); // Read a line of input from the LLM client

                // Remove UTF-8 BOM if present (PowerShell sometimes adds it)
                if (!string.IsNullOrEmpty(line) && line.StartsWith("\uFEFF"))
                {
                    line = line.TrimStart('\uFEFF');
                }                

                if (string.IsNullOrWhiteSpace(line)) // If the line is empty or whitespace, skip processing
                {
                    continue;
                }

                System.IO.File.AppendAllText("./log.txt", "=====================" + Environment.NewLine +  line + Environment.NewLine); // Log the raw input for debugging purposes
                var json = JsonNode.Parse(line); // Parse the input line as JSON
                var id = json?["id"]; // Extract the "id" field from the JSON request for correlation with responses
                var method = json?["method"]?.GetValue<string>(); // Extract the "method" field to determine which action to take

                // Skip if id is null - we need it to correlate responses
                if (id == null)
                {
                    continue;
                }

                JsonObject response;

                switch (method)
                {
                    case "initialize":
                        //var tools = ToolsList(id)["result"]?["tools"];

                        response = new JsonObject
                        {
                            ["jsonrpc"] = "2.0",
                            ["id"] = id.ToString(),
                            ["result"] = new JsonObject
                            {
                                ["protocolVersion"] = "2025-06-18",
                                ["capabilities"] = new JsonObject
                                {
                                    ["tools"] = new JsonObject
                                    {
                                        ["listChanged"] = false
                                    }
                                },
                                ["serverInfo"] = new JsonObject
                                {
                                    ["name"] = "barebones-mcp",
                                    ["version"] = "1.0"
                                }
                            }
                        };
                        break;
                    case "ping": // A simple ping method to check if the server is responsive
                        response = new JsonObject
                        {
                            ["id"] = id.ToString(),
                            ["result"] = "pong"
                        };
                        break;

                    case "tools/list": // Method to list available tools that the LLM client can use
                        response = ToolsList(id);
                        break;
                    case "tools/call": // Method to call a specific tool with parameters provided by the LLM client
                        response = ToolsCall(id, json?["params"] as JsonObject);
                        break;

                    default:
                        continue; // If the method is not recognized, skip processing and wait for the next request                        
                }

                System.IO.File.AppendAllText("./log.txt", response.ToJsonString() + Environment.NewLine); // Log the raw input for debugging purposes

                await writer.WriteLineAsync(response.ToJsonString() + Environment.NewLine + "=====================" ); // Write the response back to the LLM client as a JSON string
                await writer.FlushAsync(); // Ensure that the response is sent immediately without buffering
            }
        }
        

        /// <summary>
        /// Creates a JSON-RPC response listing available tools with their names and descriptions.
        /// </summary>
        /// <param name="id">The identifier for the JSON-RPC response.</param>
        /// <returns>A JsonObject containing the JSON-RPC version, response id, and an array of tool definitions.</returns>
        static JsonObject ToolsList(JsonNode id)
        {
            var result = new JsonObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id.ToString(),
                ["result"] = new JsonObject
                {
                    ["tools"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["name"] = "get_time",
                            ["description"] = "Returns the current server time",
                            ["inputSchema"] = new JsonObject
                            {
                                ["type"] = "object",
                                ["properties"] = new JsonObject(),
                                ["required"] = new JsonArray()
                            }
                        },
                        new JsonObject
                        {
                            ["name"] = "echo",
                            ["description"] = "Echoes back the input",
                            ["inputSchema"] = new JsonObject
                            {
                                ["type"] = "object",
                                ["properties"] = new JsonObject
                                {
                                    ["arguments"] = new JsonObject
                                    {
                                        ["type"] = "string",
                                        ["description"] = "The string to echo back"
                                    }
                                },
                                ["required"] = new JsonArray
                                {
                                    "arguments"
                                }
                            }
                        }
                    }
                }
            };
            return result;
        }

        

        /// <summary>
        /// Processes tool requests by name and returns a JSON-RPC response with either the current UTC time, an echo of
        /// input, or an error for unknown tools.
        /// </summary>
        /// <param name="id">The identifier for the JSON-RPC request.</param>
        /// <param name="parameters">A JSON object containing tool parameters, including the tool name and any required input.</param>
        /// <returns>A JsonObject representing the JSON-RPC response for the requested tool.</returns>
        static JsonObject ToolsCall(JsonNode id, JsonObject? parameters)
        {
            // Check if parameters are provided
            if (parameters == null)
            {
                return Error(id, "Missing parameters in tool call");
            }

            var toolName = parameters?["name"]?.GetValue<string>(); // Extract the tool name from the parameters to determine which tool to execute

            // Check if tool name is provided
            if (string.IsNullOrEmpty(toolName))
            {
                return Error(id, "Missing tool name in parameters");
            }

            return toolName switch // Use a switch expression to determine the response based on the tool name
            {
                "get_time" => new JsonObject // If the tool is "get_time", return the current UTC time in ISO 8601 format
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
                                ["text"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
                            }
                        }
                    }
                },
                "echo" => new JsonObject // If the tool is "echo", return the input provided in the parameters back to the client
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
                                ["text"] = parameters?["arguments"]?.GetValue<string>() ?? ""
                            }
                        }
                    }
                },
                _ => Error(id, $"Unknown tool: {toolName}") // For any unknown tool name, return an error response indicating that the tool is not recognized
            };
        }

        /// <summary>
        /// Creates a JSON-RPC error response object with the specified id and error message.
        /// </summary>
        /// <param name="id">The identifier for the JSON-RPC request.</param>
        /// <param name="message">The error message to include in the response.</param>
        /// <returns>A JsonObject representing the JSON-RPC error response.</returns>
        static JsonObject Error(JsonNode id, string message)
        {
            return new JsonObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id.ToString(),
                ["error"] = new JsonObject
                {
                    ["message"] = message
                }
            };
        }
    }
}
