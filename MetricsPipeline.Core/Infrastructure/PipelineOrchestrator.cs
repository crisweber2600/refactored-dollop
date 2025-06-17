namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;

/// <summary>
/// Orchestrates the stages of the metrics pipeline.
/// </summary>
public class PipelineOrchestrator : IPipelineOrchestrator
{
    private readonly IWorkerService _worker;
    private readonly ISummarizationService _sum;
    private readonly ISummaryRepository _repo;
    private readonly IValidationService _val;
    private readonly ICommitService _commit;
    private readonly IDiscardHandler _discard;

    /// <summary>
    /// Initializes the orchestrator with the required dependencies.
    /// </summary>
    public PipelineOrchestrator(
        IWorkerService worker,
        ISummarizationService sum,
        ISummaryRepository repo,
        IValidationService val,
        ICommitService commit,
        IDiscardHandler discard)
    {
        _worker = worker; _sum = sum; _repo = repo; _val = val; _commit = commit; _discard = discard;
    }

    /// <inheritdoc />
    public async Task<PipelineResult<PipelineState<T>>> ExecuteAsync<T>(
        string pipelineName,
        Uri source,
        Func<T, double> selector,
        SummaryStrategy strategy,
        double threshold,
        CancellationToken ct = default,
        string workerMethod = nameof(IWorkerService.FetchAsync))
    {
        var now = DateTime.UtcNow;

        var method = _worker.GetType().GetMethod(workerMethod);
        if (method == null)
            return PipelineResult<PipelineState<T>>.Failure("InvalidGatherMethod");
        if (method.IsGenericMethodDefinition)
            method = method.MakeGenericMethod(typeof(T));
        var fetchTask = method.Invoke(_worker, new object[] { source, ct }) as Task<PipelineResult<IReadOnlyList<T>>>;
        if (fetchTask == null)
            return PipelineResult<PipelineState<T>>.Failure("InvalidGatherMethod");
        var fetch = await fetchTask;
        if (!fetch.IsSuccess) return PipelineResult<PipelineState<T>>.Failure(fetch.Error!);
        var values = fetch.Value!.Select(selector).ToList();
        var summ = _sum.Summarize(values, strategy);
        if (!summ.IsSuccess) return PipelineResult<PipelineState<T>>.Failure(summ.Error!);

        var last = await _repo.GetLastCommittedAsync(pipelineName, ct);
        bool firstRun = false;
        if (!last.IsSuccess)
        {
            if (last.Error == "NoPriorSummary")
            {
                last = PipelineResult<double>.Success(0);
                firstRun = true;
            }
            else
                return PipelineResult<PipelineState<T>>.Failure(last.Error!);
        }

        var validation = firstRun
            ? PipelineResult<bool>.Success(true)
            : _val.IsWithinThreshold(fetch.Value!, selector, strategy, last.Value!, threshold);
        if (!validation.IsSuccess) return PipelineResult<PipelineState<T>>.Failure(validation.Error!);
        var state = new PipelineState<T>(pipelineName, source, fetch.Value!, summ.Value, last.Value, threshold, now);

        if (validation.Value!)
        {
            var commitRes = await _commit.CommitAsync(pipelineName, source, summ.Value!, now, ct);
            return commitRes.IsSuccess
                ? PipelineResult<PipelineState<T>>.Success(state)
                : new PipelineResult<PipelineState<T>>(state, false, commitRes.Error!);
        }
        else
        {
            await _discard.HandleDiscardAsync(summ.Value!, "Delta exceeds threshold", ct);
            return new PipelineResult<PipelineState<T>>(state, false, "ValidationFailed");
        }
    }
}
