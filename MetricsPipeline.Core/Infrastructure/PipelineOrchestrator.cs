namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;

/// <summary>
/// Orchestrates the stages of the metrics pipeline.
/// </summary>
public class PipelineOrchestrator : IPipelineOrchestrator
{
    private readonly IGatherService _gather;
    private readonly ISummarizationService _sum;
    private readonly ISummaryRepository _repo;
    private readonly IValidationService _val;
    private readonly ICommitService _commit;
    private readonly IDiscardHandler _discard;

    /// <summary>
    /// Initializes the orchestrator with the required dependencies.
    /// </summary>
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

    /// <inheritdoc />
    public async Task<PipelineResult<PipelineState>> ExecuteAsync(
        string pipelineName,
        Uri source,
        SummaryStrategy strategy,
        double threshold,
        CancellationToken ct = default,
        string gatherMethodName = nameof(IGatherService.FetchMetricsAsync))
    {
        var now = DateTime.UtcNow;

        var method = _gather.GetType().GetMethod(gatherMethodName);
        if (method == null)
            return PipelineResult<PipelineState>.Failure("InvalidGatherMethod");
        var fetchTask = method.Invoke(_gather, new object[] { source, ct }) as Task<PipelineResult<IReadOnlyList<double>>>;
        if (fetchTask == null)
            return PipelineResult<PipelineState>.Failure("InvalidGatherMethod");
        var fetch = await fetchTask;
        if (!fetch.IsSuccess) return PipelineResult<PipelineState>.Failure(fetch.Error!);

        var summ = _sum.Summarize(fetch.Value!, strategy);
        if (!summ.IsSuccess) return PipelineResult<PipelineState>.Failure(summ.Error!);

        var last = await _repo.GetLastCommittedAsync(pipelineName, ct);
        if (!last.IsSuccess)
        {
            if (last.Error == "NoPriorSummary")
                last = PipelineResult<double>.Success(0);
            else
                return PipelineResult<PipelineState>.Failure(last.Error!);
        }

        var validation = _val.IsWithinThreshold(summ.Value!, last.Value!, threshold);
        if (!validation.IsSuccess) return PipelineResult<PipelineState>.Failure(validation.Error!);
        var state = new PipelineState(pipelineName, source, fetch.Value!, summ.Value, last.Value, threshold, now);

        if (validation.Value!)
        {
            var commitRes = await _commit.CommitAsync(pipelineName, source, summ.Value!, now, ct);
            return commitRes.IsSuccess
                ? PipelineResult<PipelineState>.Success(state)
                : new PipelineResult<PipelineState>(state, false, commitRes.Error!);
        }
        else
        {
            await _discard.HandleDiscardAsync(summ.Value!, "Delta exceeds threshold", ct);
            return new PipelineResult<PipelineState>(state, false, "ValidationFailed");
        }
    }
}
