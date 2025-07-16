using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using WorkerService1.Repositories;

namespace WorkerService1
{
    public class MigrationWorker : BackgroundService
    {
        public const string ActivitySourceName = "Migrations";
        private static readonly ActivitySource s_activitySource = new(ActivitySourceName);
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        public static TaskCompletionSource MigrationCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public MigrationWorker(IServiceProvider serviceProvider, IHostApplicationLifetime hostApplicationLifetime)
        {
            _serviceProvider = serviceProvider;
            _hostApplicationLifetime = hostApplicationLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            using var activity = s_activitySource.StartActivity("Migrating database", ActivityKind.Client);
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
                await RunMigrationAsync(dbContext, cancellationToken);
            }
            catch (Exception ex)
            {
                if (activity != null)
                {
                    activity.SetTag("exception.type", ex.GetType().FullName);
                    activity.SetTag("exception.message", ex.Message);
                    activity.SetTag("exception.stacktrace", ex.StackTrace);
                }
                MigrationCompleted.TrySetException(ex);
                throw;
            }
            MigrationCompleted.TrySetResult();
            _hostApplicationLifetime.StopApplication();
        }

        private static async Task RunMigrationAsync(SampleDbContext dbContext, CancellationToken cancellationToken)
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
            });
        }
    }
}
