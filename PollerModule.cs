using Autofac;
using MyWebJob.Poller.Queues;
using MyWebJob.Common.Events;
using MyWebJob.Common.Logging;
using MyWebJob.Common.Session;
using MyWebJob.Core.Cars;
using MyWebJob.Infrastructure.My;
using MyWebJob.Jobs.Common.InfoParkingApiClient;
using Microsoft.Extensions.DependencyModel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MyWebJob.Poller
{
    public class PollerModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule<LoggingModule>();
            builder.RegisterModule<InfoParkingApiClientModule>();

            IEnumerable<Assembly> assemblies = GetAssemblies();

            builder.RegisterAssemblyTypes(assemblies.ToArray())
                .AsClosedTypesOf(typeof(IEventHandler<>))
                .InstancePerLifetimeScope();

            builder.RegisterType<MyCarRentalService>().As<ICarRentalService>().InstancePerLifetimeScope();
            builder.RegisterType<NullSession>().As<IUserSession>().InstancePerLifetimeScope();
            builder.RegisterType<MyCityQueueService>().As<IMyCityQueueService>().InstancePerLifetimeScope();
        }

        private static Assembly[] GetAssemblies()
        {
            var assemblies = new List<Assembly>();
            foreach (var library in DependencyContext.Default.RuntimeLibraries)
            {
                if (library.Name.StartsWith("MyWebJob"))
                {
                    var assembly = Assembly.Load(new AssemblyName(library.Name));
                    assemblies.Add(assembly);
                }
            }
            return assemblies.ToArray();
        }
    }
}
