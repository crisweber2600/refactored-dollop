using System.Text.Json;
using System.Linq.Expressions;
using MetricsPipeline.Core;
using MetricsPipeline.Infrastructure;

namespace MetricsPipeline.Seeding;

public class SeedDataService
{
    private readonly IGenericRepository<SummaryRecord> _repo;
    private readonly IUnitOfWork _uow;
    private readonly string _seedDir;

    public SeedDataService(IGenericRepository<SummaryRecord> repo, IUnitOfWork uow, string seedDir)
    {
        _repo = repo;
        _uow = uow;
        _seedDir = seedDir;
    }

    public async Task SeedAsync()
    {
        if (!Directory.Exists(_seedDir)) return;

        foreach (var file in Directory.GetFiles(_seedDir, "*.json"))
        {
            string json = await File.ReadAllTextAsync(file);
            List<SummaryRecord>? records;
            try
            {
                records = JsonSerializer.Deserialize<List<SummaryRecord>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                throw new SeedValidationException(Path.GetFileName(file), (int)(ex.LineNumber ?? 0), ex);
            }

            if (records == null) continue;
            foreach (var record in records)
            {
                var count = await _repo.GetCountAsync(new ExistsSpec(record.PipelineName, record.Source, record.Timestamp));
                if (count == 0)
                    await _repo.AddAsync(record);
            }
        }
        await _uow.SaveChangesAsync();
    }

    private record ExistsSpec(string Name, Uri Source, DateTime Timestamp) : ISpecification<SummaryRecord>
    {
        public Expression<Func<SummaryRecord, bool>> Criteria => r =>
            r.PipelineName == Name && r.Source == Source && r.Timestamp == Timestamp;
    }
}
