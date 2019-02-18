using AutoMapper;
using MyWebJob.Poller.Queues;
using MyWebJob.Common.EntityFramework.Repositories;
using MyWebJob.Common.EntityFramework.UnitOfWork;
using MyWebJob.Common.Logging;
using MyWebJob.Core.Cars;
using MyWebJob.Core.Cars.Entities;
using MyWebJob.Core.Notifications.Entities;
using MyWebJob.Core.RoadSides.Entities;
using MyWebJob.Jobs.Common.Configuration;
using MyWebJob.Jobs.Common.InfoParkingApiClient;
using MyWebJob.Jobs.Common.InfoParkingApiClient.Entities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyWebJob.Poller
{
    public class Functions
    {
        private readonly IMapper _mapper;
        private readonly ILogger<Functions> _logger;

        public Functions(
            IMapper mapper,
            ILogger<Functions> logger
        )
            _mapper = mapper;
            _logger = logger;
        }

        public async Task PollDataFromMyCity([TimerTrigger("0 */1 * * * *", RunOnStartup = true)]TimerInfo _)
        {
			_logger.LogInformation($"Poller service is starting.");
        }
    }
}
