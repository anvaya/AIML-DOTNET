using Microsoft.Extensions.AI;
using OllamaSharp;
using ModelContextProtocol.Client;

namespace AgenticAI
{
    internal class OllamaMCP
    {
        public static async Task Chat()
        {            
            var transport = new StdioClientTransport(
                new()
                {
                    Command = "dotnet",
                    Arguments = ["run", "--project", "I:\\Yog\\Training\\QuestpondAI\\MyCode\\MCPHosts\\MCPHosts.Server"],
                    Name = "MCPHosts.Server",
                    WorkingDirectory = "I:\\Yog\\Training\\QuestpondAI\\MyCode\\MCPHosts\\MCPHosts.Server"
                });

            McpClient mcpClient = await McpClient.CreateAsync(transport);            

            // 1. Create the Ollama client
            var ollama = new OllamaApiClient("http://localhost:11434", "gpt-oss:20b-cloud");

            // 2. Wrap it with FunctionInvokingChatClient
            var chatClient = new ChatClientBuilder(ollama)
                .UseFunctionInvocation()
                .Build();

            Console.WriteLine("Available tools:");
            IList<McpClientTool> tools = await mcpClient.ListToolsAsync();
            foreach (McpClientTool tool in tools)
            {
                Console.WriteLine($"{tool}");
            }
            Console.WriteLine();

            // 4. Send the request - the client will now auto-invoke functions
            var response = await chatClient.GetResponseAsync("What is the server time?", new ChatOptions { Tools = [.. tools] });            

            Console.WriteLine($"Response: {response.Text}");
        }
    }
}
