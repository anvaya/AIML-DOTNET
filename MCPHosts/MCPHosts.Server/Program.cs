
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Globalization;

namespace MCPHosts.Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            HostApplicationBuilder? builder = Host.CreateApplicationBuilder(args);

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole(consoleLogOptions =>
            {
                // Configure all logs to go to stderr
                consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;                
            });

            builder.Services            
                .AddMcpServer()
                .WithStdioServerTransport()
                .WithTools<MCPTools>();

            await builder.Build().RunAsync();
        }        
    }

    [McpServerToolType]
    public class MCPTools
    {
        [McpServerTool]
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
}
