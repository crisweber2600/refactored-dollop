using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkerService1.Models;
using WorkerService1.Repositories;

namespace WorkerService1
{
    public class MongoWorker : BackgroundService
    {
        private readonly ILogger<MongoWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public MongoWorker(ILogger<MongoWorker> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait for migration to complete before starting
            await MigrationWorker.MigrationCompleted.Task;
            using var scope = _scopeFactory.CreateScope();
            var _repo = scope.ServiceProvider.GetRequiredService<MongoSampleRepository>();
            // Example MongoDB operations with logging
            var entity = new SampleEntity { Name = "MongoEntity1", Value = 42 };
            await _repo.AddAsync(entity);
            _logger.LogInformation("Mongo: Added entity with Name={Name}, Value={Value}", entity.Name, entity.Value);

            var all = await _repo.GetAllAsync();
            _logger.LogInformation("Mongo: Total entities after add: {Count}", all.Count);

            if (all.Count > 0)
            {
                var first = all[0];
                first.Value += 10;
                await _repo.UpdateAsync(first);
                _logger.LogInformation("Mongo: Updated entity Id={Id} to Value={Value}", first.Id, first.Value);

                await _repo.DeleteAsync(first.Id);
                _logger.LogInformation("Mongo: Deleted entity Id={Id}", first.Id);
            }
        }
    }
}
