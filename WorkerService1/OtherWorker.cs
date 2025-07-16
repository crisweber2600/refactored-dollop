using WorkerService1.Models;
using WorkerService1.Repositories;
using WorkerService1.Services;

namespace WorkerService1
{
    /// <summary>
    /// Demonstrates how existing workers can use repositories with integrated ExampleLib validation.
    /// </summary>
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
            
            // Get repositories with integrated validation
            var sampleRepo = scope.ServiceProvider.GetRequiredService<IRepository<SampleEntity>>();
            var otherRepo = scope.ServiceProvider.GetRequiredService<IRepository<OtherEntity>>();
            var _service = scope.ServiceProvider.GetRequiredService<ISampleEntityService>();
            var _otherService = scope.ServiceProvider.GetRequiredService<IOtherEntityService>();

            // OtherEntity actions - validation happens automatically in repository
            var other = new OtherEntity { Code = "OtherWorker", Amount = 77, IsActive = true };
            await otherRepo.AddAsync(other); // <- ValidationRunner.ValidateAsync called automatically
            _logger.LogInformation("OtherWorker: Added OtherEntity Code={Code}, Amount={Amount}, IsActive={IsActive}", other.Code, other.Amount, other.IsActive);
            var allOthers = await otherRepo.GetAllAsync();
            _logger.LogInformation("OtherWorker: Total OtherEntities after add: {Count}", allOthers.Count);

            // SampleEntity actions - validation happens automatically in repository
            var sample = new SampleEntity { Name = "OtherWorkerSample", Value = 88 };
            await sampleRepo.AddAsync(sample); // <- ValidationRunner.ValidateAsync called automatically
            _logger.LogInformation("OtherWorker: Added SampleEntity Name={Name}, Value={Value}", sample.Name, sample.Value);
            var allSamples = await sampleRepo.GetAllAsync();
            _logger.LogInformation("OtherWorker: Total SampleEntities after add: {Count}", allSamples.Count);

            // Service usage demonstrating validation integration
            (bool addSuccess, int addCount) = await _otherService.AddAndCountAsync("A1", 100, true);
            _logger.LogInformation("OtherWorker AddAndCount (valid): Success={Success}, Count={Count}", addSuccess, addCount);
            (bool addFail, int addFailCount) = await _otherService.AddAndCountAsync("", -5, false);
            _logger.LogInformation("OtherWorker AddAndCount (invalid): Success={Success}, Count={Count}", addFail, addFailCount);
            (int manyCount, int manyValid, int manyInvalid) = await _otherService.AddManyAndCountAsync(new[] {
                ("B", 10, true), ("", 0, false), ("C", 20, true)
            });
            _logger.LogInformation("OtherWorker AddMany: Count={Count}, Valid={Valid}, Invalid={Invalid}", manyCount, manyValid, manyInvalid);

            // ISampleEntityService logic
            (bool sampleAddSuccess, int sampleAddCount) = await _service.AddAndCountAsync("Other1", 55);
            _logger.LogInformation("OtherWorker SampleEntity AddAndCount (valid): Success={Success}, Count={Count}", sampleAddSuccess, sampleAddCount);
            (bool sampleAddFail, int sampleAddFailCount) = await _service.AddAndCountAsync("", -99);
            _logger.LogInformation("OtherWorker SampleEntity AddAndCount (invalid): Success={Success}, Count={Count}", sampleAddFail, sampleAddFailCount);
            (int sampleManyCount, int sampleManyValid, int sampleManyInvalid) = await _service.AddManyAndCountAsync(new[] {
                ("Q", 33.0), ("", 0.0), ("R", 44.0)
            });
            _logger.LogInformation("OtherWorker SampleEntity AddMany: Count={Count}, Valid={Valid}, Invalid={Invalid}", sampleManyCount, sampleManyValid, sampleManyInvalid);
        }
    }
}
