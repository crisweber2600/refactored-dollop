using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkerService1.Models;
using WorkerService1.Repositories;
using WorkerService1.Services;
using ExampleLib.Domain;

namespace WorkerService1
{
    public class OtherWorker : BackgroundService
    {
        private readonly ILogger<OtherWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public OtherWorker(ILogger<OtherWorker> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await MigrationWorker.MigrationCompleted.Task;
            using var scope = _scopeFactory.CreateScope();
            var sampleRepo = scope.ServiceProvider.GetRequiredService<IRepository<SampleEntity>>();
            var otherRepo = scope.ServiceProvider.GetRequiredService<IRepository<OtherEntity>>();
            var _service = scope.ServiceProvider.GetRequiredService<ISampleEntityService>();
            var _otherService = scope.ServiceProvider.GetRequiredService<IOtherEntityService>();

            // OtherEntity actions
            var other = new OtherEntity { Code = "OtherWorker", Amount = 77, IsActive = true };
            await otherRepo.AddAsync(other);
            _logger.LogInformation("OtherWorker: Added OtherEntity Code={Code}, Amount={Amount}, IsActive={IsActive}", other.Code, other.Amount, other.IsActive);
            var allOthers = await otherRepo.GetAllAsync();
            _logger.LogInformation("OtherWorker: Total OtherEntities after add: {Count}", allOthers.Count);

            // SampleEntity actions
            var sample = new SampleEntity { Name = "OtherWorkerSample", Value = 88 };
            await sampleRepo.AddAsync(sample);
            _logger.LogInformation("OtherWorker: Added SampleEntity Name={Name}, Value={Value}", sample.Name, sample.Value);
            var allSamples = await sampleRepo.GetAllAsync();
            _logger.LogInformation("OtherWorker: Total SampleEntities after add: {Count}", allSamples.Count);

            // IOtherEntityService logic
            (bool addSuccess, int addCount) = await _otherService.AddAndCountAsync("A1", 100, true);
            _logger.LogInformation("OtherWorker AddAndCount (valid): Success={Success}, Count={Count}", addSuccess, addCount);
            (bool addFail, int addFailCount) = await _otherService.AddAndCountAsync("", -5, false);
            _logger.LogInformation("OtherWorker AddAndCount (invalid): Success={Success}, Count={Count}", addFail, addFailCount);
            (int manyCount, int manyValid, int manyInvalid) = await _otherService.AddManyAndCountAsync(new[] {
                ("B", 10, true), ("", 0, false), ("C", 20, true)
            });
            _logger.LogInformation("OtherWorker AddManyAndCount: Total={Count}, Valid={Valid}, Invalid={Invalid}", manyCount, manyValid, manyInvalid);
            (bool updateSuccess, bool updateValid) = await _otherService.UpdateAndCheckAsync(1, "A1-Updated", 200, true);
            _logger.LogInformation("OtherWorker UpdateAndCheck (valid): Success={Success}, IsValid={IsValid}", updateSuccess, updateValid);
            (bool updateFail, bool updateFailValid) = await _otherService.UpdateAndCheckAsync(1, "", -1, false);
            _logger.LogInformation("OtherWorker UpdateAndCheck (invalid): Success={Success}, IsValid={IsValid}", updateFail, updateFailValid);

            // ISampleEntityService logic
            (bool saddSuccess, int saddCount) = await _service.AddAndCountAsync("OtherValid", 123);
            _logger.LogInformation("OtherWorker Sample AddAndCount (valid): Success={Success}, Count={Count}", saddSuccess, saddCount);
        }
    }
}
