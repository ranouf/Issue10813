using Autofac;
using AzureWebJobPOC.Services;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;

namespace AzureWebJobPOC
{
    public class TestTriggers
    {
        private readonly ILifetimeScope _parentScope;

        public TestTriggers(ILifetimeScope parentScope)
        {
            _parentScope = parentScope;
        }

        public async Task AnalyseCarFromMontrealAsync(
            [TimerTrigger("0 */2 * * * *", RunOnStartup = true)]TimerInfo _
        )
        {
            // to have a new instance and be thread safe on each call
            using (var threadLifetime = _parentScope.BeginLifetimeScope())
            {
                var testService = threadLifetime.Resolve<ITestService>();
                await testService.RunAsync();
            }
        }
    }
}
