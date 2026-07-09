using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence;

public class BiblioBotDbContextFactory : IDesignTimeDbContextFactory<BiblioBotDbContext>
{
    public BiblioBotDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BiblioBotDbContext>();
        optionsBuilder.UseNpgsql(GetConnectionString());

        return new BiblioBotDbContext(optionsBuilder.Options);
    }

    private static string GetConnectionString()
    {
        return Environment.GetEnvironmentVariable("BIBLIOBOT_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=bibliobot;Username=postgres;Password=postgres";
    }
}
