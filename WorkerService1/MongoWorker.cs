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
            var sampleRepo = scope.ServiceProvider.GetRequiredService<IRepository<SampleEntity>>();
            var otherRepo = scope.ServiceProvider.GetRequiredService<IRepository<OtherEntity>>();

            // SampleEntity actions
            var sample = new SampleEntity { Name = "MongoEntity1", Value = 42 };
            await sampleRepo.AddAsync(sample);
            _logger.LogInformation("Mongo: Added SampleEntity Name={Name}, Value={Value}", sample.Name, sample.Value);
            var allSamples = await sampleRepo.GetAllAsync();
            _logger.LogInformation("Mongo: Total SampleEntities after add: {Count}", allSamples.Count);
            if (allSamples.Count > 0)
            {
                var first = allSamples[0];
                first.Value += 10;
                await sampleRepo.UpdateAsync(first);
                _logger.LogInformation("Mongo: Updated SampleEntity Id={Id} to Value={Value}", first.Id, first.Value);
                await sampleRepo.DeleteAsync(first.Id);
                _logger.LogInformation("Mongo: Deleted SampleEntity Id={Id}", first.Id);
            }

            // OtherEntity actions
            var other = new OtherEntity { Code = "MongoOther1", Amount = 100, IsActive = true };
            await otherRepo.AddAsync(other);
            _logger.LogInformation("Mongo: Added OtherEntity Code={Code}, Amount={Amount}, IsActive={IsActive}", other.Code, other.Amount, other.IsActive);
            var allOthers = await otherRepo.GetAllAsync();
            _logger.LogInformation("Mongo: Total OtherEntities after add: {Count}", allOthers.Count);
            if (allOthers.Count > 0)
            {
                var first = allOthers[0];
                first.Amount += 5;
                await otherRepo.UpdateAsync(first);
                _logger.LogInformation("Mongo: Updated OtherEntity Id={Id} to Amount={Amount}", first.Id, first.Amount);
                await otherRepo.DeleteAsync(first.Id);
                _logger.LogInformation("Mongo: Deleted OtherEntity Id={Id}", first.Id);
            }
        }
    }
}
