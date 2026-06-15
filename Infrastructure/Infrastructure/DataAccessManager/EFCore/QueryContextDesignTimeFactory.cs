using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.DataAccessManager.EFCore;

public sealed class QueryContextDesignTimeFactory : IDesignTimeDbContextFactory<QueryContext>
{
    public QueryContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<QueryContext>();
        var connectionString = Environment.GetEnvironmentVariable("NSUITE_CONNECTION_STRING")
            ?? "Server=localhost;Database=nSuite;Trusted_Connection=True;TrustServerCertificate=True";

        optionsBuilder.UseSqlServer(connectionString);
        return new QueryContext(optionsBuilder.Options, new DesignTimeOperatorContext());
    }
}
