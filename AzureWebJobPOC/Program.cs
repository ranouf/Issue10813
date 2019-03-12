using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace AzureWebJobPOC
{
    public class Program
    {
        public static IHostingEnvironment Environment { get; set; }
        public static async Task Main(string[] args)
        {
            //source: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-2.2
            var host = new HostBuilder()
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                    configHost.AddJsonFile("hostsettings.json", optional: true);
                    configHost.AddEnvironmentVariables();
                    configHost.AddCommandLine(args);
                })
                .ConfigureWebJobs(webJobConfiguration =>
                {
                    webJobConfiguration.AddTimers();
                    webJobConfiguration.AddAzureStorageCoreServices();
                })
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    Environment = hostContext.HostingEnvironment;
                    configApp.AddJsonFile("appsettings.json", optional: true);
                    configApp.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true);
                    configApp.AddEnvironmentVariables();
                    configApp.AddCommandLine(args);
                })
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureServices((hostContext, services) =>
                {
                    
                })
                .ConfigureContainer<ContainerBuilder>(builder =>
                {
                    builder.RegisterModule(new TestModule());
                })
                .ConfigureLogging((hostContext, configLogging) =>
                {
                    configLogging.AddApplicationInsights(c =>
                        c.InstrumentationKey = hostContext.Configuration["ApplicationInsights:InstrumentationKey"]
                    );
                    if (hostContext.HostingEnvironment.IsDevelopment())
                    {
                        configLogging.AddConsole();
                        configLogging.AddDebug();
                        configLogging.AddFile();
                        configLogging.AddFilter("Microsoft", LogLevel.Warning);
                    }
                })
                .UseConsoleLifetime()
                .Build();

            using (host)
            {
                await host.RunAsync();
            }
        }
    }
}
