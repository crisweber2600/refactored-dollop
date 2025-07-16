using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkerService1.Models;
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
            var _service = scope.ServiceProvider.GetRequiredService<ISampleEntityService>();

            // 1. Add and count (valid)
            (bool addSuccess, int addCount) = await _service.AddAndCountAsync("ValidName", 15);
            _logger.LogInformation("AddAndCount (valid): Success={Success}, Count={Count}", addSuccess, addCount);

            // 1b. Add and count (invalid)
            (bool addFail, int addFailCount) = await _service.AddAndCountAsync("InvalidName", 5);
            _logger.LogInformation("AddAndCount (invalid): Success={Success}, Count={Count}", addFail, addFailCount);

            // 2. Add many and count (mix valid/invalid)
            (int manyCount, int manyValid, int manyInvalid) = await _service.AddManyAndCountAsync(new[] {
                ("A", 5.0), ("Valid2", 20.0), ("", 0.0), ("Valid3", 9.0)
            });
            _logger.LogInformation("AddManyAndCount: Total={Count}, Valid={Valid}, Invalid={Invalid}", manyCount, manyValid, manyInvalid);

            // 3. Update entity and check (valid)
            (bool updateSuccess, bool updateValid) = await _service.UpdateAndCheckAsync(1, "UpdatedName", 25);
            _logger.LogInformation("UpdateAndCheck (valid): Success={Success}, IsValid={IsValid}", updateSuccess, updateValid);

            // 3b. Update entity and check (invalid)
            (bool updateFail, bool updateFailValid) = await _service.UpdateAndCheckAsync(1, "", 2);
            _logger.LogInformation("UpdateAndCheck (invalid): Success={Success}, IsValid={IsValid}", updateFail, updateFailValid);

            // 4. Update many and check (mix valid/invalid)
            var updates = new Dictionary<int, (string, double)> { { 2, ("ValidUpdated", 30) }, { 3, ("", 1) } };
            (bool allUpdated, int validCount, int invalidCount) = await _service.UpdateManyAndCheckAsync(updates);
            _logger.LogInformation("UpdateManyAndCheck: AllUpdated={AllUpdated}, Valid={Valid}, Invalid={Invalid}", allUpdated, validCount, invalidCount);

            // 5. Delete and check unvalidated
            (bool deleted, bool deletedValid) = await _service.DeleteAndCheckUnvalidatedAsync(1);
            _logger.LogInformation("DeleteAndCheckUnvalidated: Deleted={Deleted}, WasValid={WasValid}", deleted, deletedValid);

            // 6. Hard delete and check removed
            (bool hardDeleted, bool hardDeletedValid) = await _service.HardDeleteAndCheckRemovedAsync(2);
            _logger.LogInformation("HardDeleteAndCheckRemoved: Deleted={Deleted}, WasValid={WasValid}", hardDeleted, hardDeletedValid);

            // 7. Get by id including deleted
            var entity = await _service.GetByIdIncludingDeletedAsync(3);
            _logger.LogInformation("GetByIdIncludingDeleted: {Entity}", entity?.Name ?? "null");

            // 8. Final add many and count
            (int finalCount, int finalValid, int finalInvalid) = await _service.AddManyAndCountAsync(new[] { ("Five", 50.0), ("", 0.0), ("Six", 60.0) });
            _logger.LogInformation("Final AddManyAndCount: Total={Count}, Valid={Valid}, Invalid={Invalid}", finalCount, finalValid, finalInvalid);
        }
    }
}
