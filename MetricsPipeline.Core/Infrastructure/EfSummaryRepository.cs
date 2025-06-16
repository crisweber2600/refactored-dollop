namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Summary repository backed by Entity Framework Core.
/// </summary>
public class EfSummaryRepository : ISummaryRepository
{
    private readonly SummaryDbContext _db;

    /// <summary>
    /// Initializes the repository with the provided context.
    /// </summary>
    /// <param name="db">Database context.</param>
    public EfSummaryRepository(SummaryDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<PipelineResult<double>> GetLastCommittedAsync(string pipelineName, CancellationToken ct = default)
    {
        var rec = await _db.Summaries
                           .Where(s => s.PipelineName == pipelineName)
                           .OrderByDescending(s => s.Timestamp)
                           .FirstOrDefaultAsync(ct);

        return rec is null
            ? PipelineResult<double>.Failure("NoPriorSummary")
            : PipelineResult<double>.Success(rec.Value);
    }

    /// <inheritdoc />
    public async Task<PipelineResult<Unit>> SaveAsync(string pipelineName, Uri source, double summary, DateTime ts, CancellationToken ct = default)
    {
        try
        {
            _db.Summaries.Add(new SummaryRecord { PipelineName = pipelineName, Source = source, Value = summary, Timestamp = ts });
            await _db.SaveChangesAsync(ct);
            return PipelineResult<Unit>.Success(new Unit());
        }
        catch (Exception ex)
        {
            return PipelineResult<Unit>.Failure(ex.Message);
        }
    }
}
