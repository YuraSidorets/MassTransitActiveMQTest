using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace MassTransitActiveMQTimeoutTest;

public sealed class ProducerTestApp : IAsyncDisposable
{
    public IHost TestHost;

    public ProducerTestApp(ITestOutputHelper testOutputHelper, string host, int port)
    {
        IHostBuilder hostBuilder = Host.CreateDefaultBuilder();

        hostBuilder.ConfigureServices(services =>
        {
            services.AddMassTransit(cfg =>
            {
                cfg.UsingActiveMq((context, busConfigurator) =>
                {
                    busConfigurator.Host(host, port, configure =>
                    {
                        configure.Username("admin");
                        configure.Password("admin");
                    });

                    busConfigurator.ConfigureEndpoints(context);
                });
            });
        });
        hostBuilder.ConfigureLogging(l =>
        {
            l.ClearProviders();
            l.AddXUnit(testOutputHelper, o => o.IncludeScopes = true);
            l.SetMinimumLevel(LogLevel.Trace);
        });
        TestHost = hostBuilder.Build();
    }

    public Task RunAsync()
    {
        return TestHost.RunAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await TestHost.StopAsync().ConfigureAwait(false);
        TestHost.Dispose();
        GC.SuppressFinalize(this);
    }
}
