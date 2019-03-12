using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AzureWebJobPOC.Services
{
    public class TestService : ITestService
    {
        private readonly ILogger<TestService> _logger;

        public TestService(ILogger<TestService> logger)
        {
            _logger = logger;
        }

        public Task RunAsync()
        {
            _logger.LogInformation("Yes, it works!");
            return Task.CompletedTask;
        }
    }
}
