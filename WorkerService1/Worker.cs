using WorkerService1.Models;
using WorkerService1.Repositories;
using WorkerService1.Services;

namespace WorkerService1
{
    /// <summary>
    /// Demonstrates how existing workers can use repositories with integrated ExampleLib validation.
    /// </summary>
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
            
            // Get repositories with integrated validation
            var sampleRepo = scope.ServiceProvider.GetRequiredService<IRepository<SampleEntity>>();
            var otherRepo = scope.ServiceProvider.GetRequiredService<IRepository<OtherEntity>>();
            var _service = scope.ServiceProvider.GetRequiredService<ISampleEntityService>();
            var _otherService = scope.ServiceProvider.GetRequiredService<IOtherEntityService>();

            // SampleEntity actions - validation happens automatically in repository
            var sample = new SampleEntity { Name = "WorkerSample", Value = 10 };
            await sampleRepo.AddAsync(sample); // <- ValidationRunner.ValidateAsync called automatically
            _logger.LogInformation("Worker: Added SampleEntity Name={Name}, Value={Value}", sample.Name, sample.Value);
            var allSamples = await sampleRepo.GetAllAsync();
            _logger.LogInformation("Worker: Total SampleEntities after add: {Count}", allSamples.Count);

            // OtherEntity actions - validation happens automatically in repository
            var other = new OtherEntity { Code = "WorkerOther", Amount = 20, IsActive = true };
            await otherRepo.AddAsync(other); // <- ValidationRunner.ValidateAsync called automatically
            _logger.LogInformation("Worker: Added OtherEntity Code={Code}, Amount={Amount}, IsActive={IsActive}", other.Code, other.Amount, other.IsActive);
            var allOthers = await otherRepo.GetAllAsync();
            _logger.LogInformation("Worker: Total OtherEntities after add: {Count}", allOthers.Count);

            // Service usage (which also uses validated repositories)
            (bool addSuccess, int addCount) = await _service.AddAndCountAsync("ValidName", 15);
            _logger.LogInformation("AddAndCount (valid): Success={Success}, Count={Count}", addSuccess, addCount);
            (bool addFail, int addFailCount) = await _service.AddAndCountAsync("InvalidName", 5);
            _logger.LogInformation("AddAndCount (invalid): Success={Success}, Count={Count}", addFail, addFailCount);
            (int manyCount, int manyValid, int manyInvalid) = await _service.AddManyAndCountAsync(new[] {
                ("A", 5.0), ("Valid2", 20.0), ("", 0.0), ("Valid3", 9.0)
            });
            _logger.LogInformation("AddMany: Count={Count}, Valid={Valid}, Invalid={Invalid}", manyCount, manyValid, manyInvalid);

            // IOtherEntityService logic
            (bool otherAddSuccess, int otherAddCount) = await _otherService.AddAndCountAsync("Code1", 25, true);
            _logger.LogInformation("OtherEntity AddAndCount (valid): Success={Success}, Count={Count}", otherAddSuccess, otherAddCount);
            (bool otherAddFail, int otherAddFailCount) = await _otherService.AddAndCountAsync("", -10, false);
            _logger.LogInformation("OtherEntity AddAndCount (invalid): Success={Success}, Count={Count}", otherAddFail, otherAddFailCount);
            (int otherManyCount, int otherManyValid, int otherManyInvalid) = await _otherService.AddManyAndCountAsync(new[] {
                ("X", 30, true), ("", 0, false), ("Y", 40, true)
            });
            _logger.LogInformation("OtherEntity AddMany: Count={Count}, Valid={Valid}, Invalid={Invalid}", otherManyCount, otherManyValid, otherManyInvalid);
        }
    }
}
