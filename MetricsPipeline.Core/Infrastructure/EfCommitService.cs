namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;

/// <summary>
/// Commit service that persists summaries using the repository implementation.
/// </summary>
public class EfCommitService : ICommitService
{
    private readonly ISummaryRepository _repo;

    /// <summary>
    /// Initializes a new instance of the service.
    /// </summary>
    /// <param name="repo">Summary repository used for persistence.</param>
    public EfCommitService(ISummaryRepository repo) => _repo = repo;

    /// <inheritdoc />
    public Task<PipelineResult<Unit>> CommitAsync(string pipelineName, Uri source, double summary, DateTime ts, CancellationToken ct = default) =>
        _repo.SaveAsync(pipelineName, source, summary, ts, ct);
}
