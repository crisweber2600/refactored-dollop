using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ExampleLib.Infrastructure;

public class TheNannyDbContextFactory : IDesignTimeDbContextFactory<TheNannyDbContext>
{
    public TheNannyDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TheNannyDbContext>();
        // Use a local development connection string or in-memory for migration generation
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=TheNannyDb;Trusted_Connection=True;");
        return new TheNannyDbContext(optionsBuilder.Options);
    }
}
