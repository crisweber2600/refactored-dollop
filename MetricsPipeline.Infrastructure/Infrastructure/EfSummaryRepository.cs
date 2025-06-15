namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;
using Microsoft.EntityFrameworkCore;

public class EfSummaryRepository : ISummaryRepository
{
    private readonly SummaryDbContext _db;
    public EfSummaryRepository(SummaryDbContext db) => _db = db;

    public async Task<PipelineResult<double>> GetLastCommittedAsync(Uri source, CancellationToken ct = default)
    {
        var rec = await _db.Summaries
                           .Where(s => s.Source == source)
                           .OrderByDescending(s => s.Timestamp)
                           .FirstOrDefaultAsync(ct);

        return rec is null
            ? PipelineResult<double>.Failure("NoPriorSummary")
            : PipelineResult<double>.Success(rec.Value);
    }

    public async Task<PipelineResult<Unit>> SaveAsync(double summary, DateTime ts, CancellationToken ct = default)
    {
        try
        {
            _db.Summaries.Add(new SummaryRecord { Source = new Uri("https://api.example.com/data"), Value = summary, Timestamp = ts });
            await _db.SaveChangesAsync(ct);
            return PipelineResult<Unit>.Success(new Unit());
        }
        catch (Exception ex)
        {
            return PipelineResult<Unit>.Failure(ex.Message);
        }
    }
}
