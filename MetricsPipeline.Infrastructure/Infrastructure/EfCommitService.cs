namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;

public class EfCommitService : ICommitService
{
    private readonly ISummaryRepository _repo;
    public EfCommitService(ISummaryRepository repo) => _repo = repo;

    public Task<PipelineResult<Unit>> CommitAsync(double summary, DateTime ts, CancellationToken ct = default) =>
        _repo.SaveAsync(summary, ts, ct);
}
