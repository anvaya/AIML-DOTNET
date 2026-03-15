using Microsoft.Extensions.AI;
using OllamaSharp;
using ModelContextProtocol.Client;

namespace MCPHosts.Host
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await Chat();
        }

        public static async Task Chat()
        {
            // 1. Create the MCP client with stdio transport
            var transport = new StdioClientTransport(
                new()
                {
                    Command = "dotnet",
                    Arguments = ["run", "--project", "I:\\Yog\\Training\\QuestpondAI\\MyCode\\MCPHosts\\MCPHosts.Server"],
                    Name = "MCPHosts.Server",
                    WorkingDirectory = "I:\\Yog\\Training\\QuestpondAI\\MyCode\\MCPHosts\\MCPHosts.Server"
                });

            // 2. Create the MCP client
            McpClient mcpClient = await McpClient.CreateAsync(transport);

            // 3. Create the Ollama client
            var ollama = new OllamaApiClient("http://localhost:11434", "gpt-oss:20b-cloud");

            // 4. Create the chat client with function invocation enabled
            var chatClient = new ChatClientBuilder(ollama)
                .UseFunctionInvocation()
                .Build();

            // 5. List available tools from the MCP server
            Console.WriteLine("Available tools:");
            IList<McpClientTool> tools = await mcpClient.ListToolsAsync(); // This will list the tools that the MCP server has registered
            foreach (McpClientTool tool in tools)
            {
                Console.WriteLine($"{tool}");
            }
            Console.WriteLine();

            // 6. Send the request - the client will now auto-invoke functions
            var response = await chatClient.GetResponseAsync("What is the server time?", new ChatOptions { Tools = [.. tools] });

            Console.WriteLine($"Response: {response.Text}");
        }
    }
}
