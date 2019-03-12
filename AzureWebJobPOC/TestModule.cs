using Autofac;
using AzureWebJobPOC.Services;

namespace AzureWebJobPOC
{
    public  class TestModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<TestService>().As<ITestService>().InstancePerLifetimeScope();
        }
    }
}
