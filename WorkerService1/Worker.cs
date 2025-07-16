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
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait for migration to complete before starting
            await MigrationWorker.MigrationCompleted.Task;
            using var scope = _scopeFactory.CreateScope();
            var sampleRepo = scope.ServiceProvider.GetRequiredService<IRepository<SampleEntity>>();
            var otherRepo = scope.ServiceProvider.GetRequiredService<IRepository<OtherEntity>>();
            var _service = scope.ServiceProvider.GetRequiredService<ISampleEntityService>();
            var _otherService = scope.ServiceProvider.GetRequiredService<IOtherEntityService>();

            // SampleEntity actions
            var sample = new SampleEntity { Name = "WorkerSample", Value = 10 };
            await sampleRepo.AddAsync(sample);
            _logger.LogInformation("Worker: Added SampleEntity Name={Name}, Value={Value}", sample.Name, sample.Value);
            var allSamples = await sampleRepo.GetAllAsync();
            _logger.LogInformation("Worker: Total SampleEntities after add: {Count}", allSamples.Count);

            // OtherEntity actions
            var other = new OtherEntity { Code = "WorkerOther", Amount = 20, IsActive = true };
            await otherRepo.AddAsync(other);
            _logger.LogInformation("Worker: Added OtherEntity Code={Code}, Amount={Amount}, IsActive={IsActive}", other.Code, other.Amount, other.IsActive);
            var allOthers = await otherRepo.GetAllAsync();
            _logger.LogInformation("Worker: Total OtherEntities after add: {Count}", allOthers.Count);

            // Existing ISampleEntityService logic
            (bool addSuccess, int addCount) = await _service.AddAndCountAsync("ValidName", 15);
            _logger.LogInformation("AddAndCount (valid): Success={Success}, Count={Count}", addSuccess, addCount);
            (bool addFail, int addFailCount) = await _service.AddAndCountAsync("InvalidName", 5);
            _logger.LogInformation("AddAndCount (invalid): Success={Success}, Count={Count}", addFail, addFailCount);
            (int manyCount, int manyValid, int manyInvalid) = await _service.AddManyAndCountAsync(new[] {
                ("A", 5.0), ("Valid2", 20.0), ("", 0.0), ("Valid3", 9.0)
            });
            _logger.LogInformation("AddManyAndCount: Total={Count}, Valid={Valid}, Invalid={Invalid}", manyCount, manyValid, manyInvalid);
            (bool updateSuccess, bool updateValid) = await _service.UpdateAndCheckAsync(1, "UpdatedName", 25);
            _logger.LogInformation("UpdateAndCheck (valid): Success={Success}, IsValid={IsValid}", updateSuccess, updateValid);
            (bool updateFail, bool updateFailValid) = await _service.UpdateAndCheckAsync(1, "", 2);
            _logger.LogInformation("UpdateAndCheck (invalid): Success={Success}, IsValid={IsValid}", updateFail, updateFailValid);
            var updates = new Dictionary<int, (string, double)> { { 2, ("ValidUpdated", 30) }, { 3, ("", 1) } };
            (bool allUpdated, int validCount, int invalidCount) = await _service.UpdateManyAndCheckAsync(updates);
            _logger.LogInformation("UpdateManyAndCheck: AllUpdated={AllUpdated}, Valid={Valid}, Invalid={Invalid}", allUpdated, validCount, invalidCount);
            (bool deleted, bool deletedValid) = await _service.DeleteAndCheckUnvalidatedAsync(1);
            _logger.LogInformation("DeleteAndCheckUnvalidated: Deleted={Deleted}, WasValid={WasValid}", deleted, deletedValid);
            (bool hardDeleted, bool hardDeletedValid) = await _service.HardDeleteAndCheckRemovedAsync(2);
            _logger.LogInformation("HardDeleteAndCheckRemoved: Deleted={Deleted}, WasValid={WasValid}", hardDeleted, hardDeletedValid);
            var entity = await _service.GetByIdIncludingDeletedAsync(3);
            _logger.LogInformation("GetByIdIncludingDeleted: {Entity}", entity?.Name ?? "null");
            (int finalCount, int finalValid, int finalInvalid) = await _service.AddManyAndCountAsync(new[] { ("Five", 50.0), ("", 0.0), ("Six", 60.0) });
            _logger.LogInformation("Final AddManyAndCount: Total={Count}, Valid={Valid}, Invalid={Invalid}", finalCount, finalValid, finalInvalid);

            // IOtherEntityService logic
            (bool oaddSuccess, int oaddCount) = await _otherService.AddAndCountAsync("OValid", 100, true);
            _logger.LogInformation("Other AddAndCount (valid): Success={Success}, Count={Count}", oaddSuccess, oaddCount);
            (bool oaddFail, int oaddFailCount) = await _otherService.AddAndCountAsync("", -5, false);
            _logger.LogInformation("Other AddAndCount (invalid): Success={Success}, Count={Count}", oaddFail, oaddFailCount);
            (int omanyCount, int omanyValid, int omanyInvalid) = await _otherService.AddManyAndCountAsync(new[] {
                ("B", 10, true), ("", 0, false), ("C", 20, true)
            });
            _logger.LogInformation("Other AddManyAndCount: Total={Count}, Valid={Valid}, Invalid={Invalid}", omanyCount, omanyValid, omanyInvalid);
            (bool oupdateSuccess, bool oupdateValid) = await _otherService.UpdateAndCheckAsync(1, "OUpdated", 200, true);
            _logger.LogInformation("Other UpdateAndCheck (valid): Success={Success}, IsValid={IsValid}", oupdateSuccess, oupdateValid);
            (bool oupdateFail, bool oupdateFailValid) = await _otherService.UpdateAndCheckAsync(1, "", -1, false);
            _logger.LogInformation("Other UpdateAndCheck (invalid): Success={Success}, IsValid={IsValid}", oupdateFail, oupdateFailValid);
        }
    }
}
