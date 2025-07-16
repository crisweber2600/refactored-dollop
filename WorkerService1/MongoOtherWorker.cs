using WorkerService1.Models;
using WorkerService1.Repositories;

namespace WorkerService1
{
    /// <summary>
    /// Demonstrates MongoDB repositories with integrated ExampleLib validation.
    /// </summary>
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
            // Wait for migration to complete before starting
            await MigrationWorker.MigrationCompleted.Task;
            using var scope = _scopeFactory.CreateScope();
            
            // Get MongoDB repositories with integrated validation
            var sampleRepo = scope.ServiceProvider.GetRequiredService<MongoRepository<SampleEntity>>();
            var otherRepo = scope.ServiceProvider.GetRequiredService<MongoRepository<OtherEntity>>();

            // OtherEntity actions with MongoDB - validation happens automatically
            var other = new OtherEntity { Code = "MongoOtherWorker1", Amount = 200, IsActive = true };
            await otherRepo.AddAsync(other); // <- ValidationRunner.ValidateAsync called automatically
            _logger.LogInformation("MongoOther: Added OtherEntity Code={Code}, Amount={Amount}, IsActive={IsActive}", other.Code, other.Amount, other.IsActive);
            var allOthers = await otherRepo.GetAllAsync();
            _logger.LogInformation("MongoOther: Total OtherEntities after add: {Count}", allOthers.Count);

            // SampleEntity actions with MongoDB - validation happens automatically
            var sample = new SampleEntity { Name = "MongoOtherWorkerSample", Value = 150 };
            await sampleRepo.AddAsync(sample); // <- ValidationRunner.ValidateAsync called automatically
            _logger.LogInformation("MongoOther: Added SampleEntity Name={Name}, Value={Value}", sample.Name, sample.Value);
            var allSamples = await sampleRepo.GetAllAsync();
            _logger.LogInformation("MongoOther: Total SampleEntities after add: {Count}", allSamples.Count);

            // Demonstrate GetLastAsync functionality
            var lastSample = await sampleRepo.GetLastAsync();
            if (lastSample != null)
            {
                _logger.LogInformation("MongoOther: Last SampleEntity Name={Name}, Value={Value}", lastSample.Name, lastSample.Value);
            }

            var lastOther = await otherRepo.GetLastAsync();
            if (lastOther != null)
            {
                _logger.LogInformation("MongoOther: Last OtherEntity Code={Code}, Amount={Amount}", lastOther.Code, lastOther.Amount);
            }
        }
    }
}
