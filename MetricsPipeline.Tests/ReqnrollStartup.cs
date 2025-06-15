using MetricsPipeline.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

public class ReqnrollStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMetricsPipeline(o => o.UseInMemoryDatabase("summaries"));
    }
}
