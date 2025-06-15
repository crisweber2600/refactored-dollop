namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;

public class PipelineOrchestrator : IPipelineOrchestrator
{
    private readonly IGatherService _gather;
    private readonly ISummarizationService _sum;
    private readonly ISummaryRepository _repo;
    private readonly IValidationService _val;
    private readonly ICommitService _commit;
    private readonly IDiscardHandler _discard;

    public PipelineOrchestrator(
        IGatherService gather,
        ISummarizationService sum,
        ISummaryRepository repo,
        IValidationService val,
        ICommitService commit,
        IDiscardHandler discard)
    {
        _gather = gather; _sum = sum; _repo = repo; _val = val; _commit = commit; _discard = discard;
    }

    public async Task<PipelineResult<PipelineState>> ExecuteAsync(
        Uri source, SummaryStrategy strategy, double threshold, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var fetch = await _gather.FetchMetricsAsync(source, ct);
        if (!fetch.IsSuccess) return PipelineResult<PipelineState>.Failure(fetch.Error!);

        var summ = _sum.Summarize(fetch.Value!, strategy);
        if (!summ.IsSuccess) return PipelineResult<PipelineState>.Failure(summ.Error!);

        var last = await _repo.GetLastCommittedAsync(source, ct);
        if (!last.IsSuccess) return PipelineResult<PipelineState>.Failure(last.Error!);

        var validation = _val.IsWithinThreshold(summ.Value!, last.Value!, threshold);
        if (!validation.IsSuccess) return PipelineResult<PipelineState>.Failure(validation.Error!);
        var state = new PipelineState(source, fetch.Value!, summ.Value, last.Value, threshold, now);

        if (validation.Value!)
        {
            var commitRes = await _commit.CommitAsync(summ.Value!, now, ct);
            return commitRes.IsSuccess
                ? PipelineResult<PipelineState>.Success(state)
                : PipelineResult<PipelineState>.Failure(commitRes.Error!);
        }
        else
        {
            await _discard.HandleDiscardAsync(summ.Value!, "Delta exceeds threshold", ct);
            return PipelineResult<PipelineState>.Failure("ValidationFailed");
        }
    }
}
