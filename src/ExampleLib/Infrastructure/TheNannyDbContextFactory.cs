using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ExampleLib.Infrastructure;

public class TheNannyDbContextFactory : IDesignTimeDbContextFactory<TheNannyDbContext>
{
    public TheNannyDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TheNannyDbContext>();
        // Use in-memory database for design-time and test scenarios
        optionsBuilder.UseInMemoryDatabase("TheNannyDb");
        return new TheNannyDbContext(optionsBuilder.Options);
    }
}
