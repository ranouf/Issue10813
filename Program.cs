using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoMapper;
using MyWebJob.Poller.Queues;
using MyWebJob.Common.Extensions;
using MyWebJob.Common.Identity.Configuration;
using MyWebJob.Common.Identity.Entities;
using MyWebJob.Common.Queues.Configuration;
using MyWebJob.Core;
using MyWebJob.Infrastructure;
using MyWebJob.Infrastructure.My.Configuration;
using MyWebJob.Infrastructure.SQLServer;
using MyWebJob.Jobs.Common.Configuration;
using MyWebJob.Jobs.Common.InfoParkingApiClient;
using MyWebJob.Jobs.Common.InfoParkingApiClient.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetEscapades.Extensions.Logging.RollingFile;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MyWebJob.Poller
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            //source: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-2.2
            var host = new HostBuilder()
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                    configHost.AddJsonFile("hostsettings.json", optional: true);
                    configHost.AddEnvironmentVariables(prefix: "PREFIX_");
                    configHost.AddCommandLine(args);
                })
                .ConfigureWebJobs(webJobConfiguration =>
                {
                    webJobConfiguration.AddTimers();
                    webJobConfiguration.AddAzureStorageCoreServices();
                })
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    configApp.AddJsonFile("appsettings.json", optional: true);
                    configApp.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true);
                    configApp.AddEnvironmentVariables(prefix: "PREFIX_");
                    configApp.AddCommandLine(args);
                })
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureServices((hostContext, services) =>
                {
                    // Automapper
                    services.AddAutoMapper(c => { c.AddProfile<InfoParkingApiClientProfile>(); });

                    services.ConfigureAndValidate<DefaultUserAccountsSettings>(hostContext.Configuration);
                    services.ConfigureAndValidate<AnalyzerSettings>(hostContext.Configuration);
                    services.ConfigureAndValidate<MySettings>(hostContext.Configuration);
                    services.ConfigureAndValidate<InfoParkingApiSettings>(hostContext.Configuration);
                    services.ConfigureAndValidate<ConnectionStringsSettings>(hostContext.Configuration);
                    if (hostContext.HostingEnvironment.IsDevelopment())
                    {
                        services.ConfigureAndValidate<FileLoggerOptions>(hostContext.Configuration, "FileLogging");
                    }

                    // Setup SQLServerDB
                    services.AddDbContext<MyWebJobDBContext>(options =>
                        options.UseSqlServer(
                            hostContext.Configuration.GetConnectionString("DefaultConnection"),
                            opt => opt
                                .UseNetTopologySuite()
                                .EnableRetryOnFailure(
                                    maxRetryCount: 5,
                                    maxRetryDelay: TimeSpan.FromSeconds(30),
                                    errorNumbersToAdd: null
                                )
                                .CommandTimeout(hostContext.HostingEnvironment.IsDevelopment()
                                    ? 240 // my machine could be very slow sometimes ...
                                    : 30
                                )
                        )
                    );

                    // Identity
                    services.AddIdentity<User, Role>()
                        .AddEntityFrameworkStores<MyWebJobDBContext>()
                        .AddDefaultTokenProviders()
                        .AddUserStore<UserStore<User, Role, MyWebJobDBContext, Guid, IdentityUserClaim<Guid>, UserRole, IdentityUserLogin<Guid>, IdentityUserToken<Guid>, IdentityRoleClaim<Guid>>>()
                        .AddRoleStore<RoleStore<Role, MyWebJobDBContext, Guid, UserRole, IdentityRoleClaim<Guid>>>();
                })
                .ConfigureContainer<ContainerBuilder>(builder =>
                {
                    builder.RegisterModule<InfrastructureModule>();
                    builder.RegisterModule<CoreModule>(); 
                    builder.RegisterModule<PollerModule>();
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

        #region Private
        private static async Task EnsureQueueExistsAsync(IServiceProvider services, ILogger<Program> logger)
        {
            try
            {
                var MyCityQueueService = services.GetRequiredService<IMyCityQueueService>();
                await MyCityQueueService.EnsureQueueExistsAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while creating the queue.");
            }
        }

        private static async Task InitializeDataBaseAsync(IServiceProvider services, ILogger<Program> logger)
        {
            try
            {
                logger.LogInformation("Starting the SQLServerDB initialization.");
                await SQLServerDBInitializer.InitializeAsync(services, logger);
                logger.LogInformation("The SQLServerDB initialization has been done.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initialization the SQLServerDB.");
            }
        }
        #endregion
    }
}
