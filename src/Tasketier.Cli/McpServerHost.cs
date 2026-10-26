using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tasketier.Core;

namespace Tasketier.Cli;

/// <summary>Runs the MCP server over stdin/stdout until the client closes the stream. Logs go to stderr.</summary>
public static class McpServerHost
{
    /// <summary>Runs the server for a service.</summary>
    /// <param name="service">The task service the tools use.</param>
    /// <returns>Completes when the client disconnects.</returns>
    public static async Task RunAsync(TaskService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings { Args = [] });
        builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.Services.AddSingleton(service);
        builder.Services.AddMcpServer(options =>
        {
            options.ServerInfo = new() { Name = "tasketier", Version = typeof(McpServerHost).Assembly.GetName().Version?.ToString(3) ?? "0.0.0" };
        })
            .WithStdioServerTransport()
            .WithTools<TaskTools>();
        using IHost host = builder.Build();
        await host.RunAsync().ConfigureAwait(false);
    }
}
