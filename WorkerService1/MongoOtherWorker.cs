using WorkerService1.Models;
using WorkerService1.Repositories;

namespace WorkerService1
{
    public class MongoOtherWorker : BackgroundService
    {
        private readonly ILogger<MongoOtherWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public MongoOtherWorker(ILogger<MongoOtherWorker> logger, IServiceScopeFactory scopeFactory)
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

            // OtherEntity actions
            var entity = new OtherEntity { Code = "MongoA", Amount = 50, IsActive = true };
            await otherRepo.AddAsync(entity);
            _logger.LogInformation("MongoOtherWorker: Added OtherEntity Code={Code}, Amount={Amount}, IsActive={IsActive}", entity.Code, entity.Amount, entity.IsActive);
            var all = await otherRepo.GetAllAsync();
            _logger.LogInformation("MongoOtherWorker: Total OtherEntities after add: {Count}", all.Count);
            if (all.Count > 0)
            {
                var first = all[0];
                first.Amount += 10;
                await otherRepo.UpdateAsync(first);
                _logger.LogInformation("MongoOtherWorker: Updated OtherEntity Id={Id} to Amount={Amount}", first.Id, first.Amount);
                await otherRepo.DeleteAsync(first.Id);
                _logger.LogInformation("MongoOtherWorker: Deleted OtherEntity Id={Id}", first.Id);
            }

            // SampleEntity actions
            var sample = new SampleEntity { Name = "MongoOtherWorkerSample", Value = 99 };
            await sampleRepo.AddAsync(sample);
            _logger.LogInformation("MongoOtherWorker: Added SampleEntity Name={Name}, Value={Value}", sample.Name, sample.Value);
            var allSamples = await sampleRepo.GetAllAsync();
            _logger.LogInformation("MongoOtherWorker: Total SampleEntities after add: {Count}", allSamples.Count);
            if (allSamples.Count > 0)
            {
                var first = allSamples[0];
                first.Value += 1;
                await sampleRepo.UpdateAsync(first);
                _logger.LogInformation("MongoOtherWorker: Updated SampleEntity Id={Id} to Value={Value}", first.Id, first.Value);
                await sampleRepo.DeleteAsync(first.Id);
                _logger.LogInformation("MongoOtherWorker: Deleted SampleEntity Id={Id}", first.Id);
            }
        }
    }
}
