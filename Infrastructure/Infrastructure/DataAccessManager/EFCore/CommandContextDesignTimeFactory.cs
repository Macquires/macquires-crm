using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.DataAccessManager.EFCore;

public sealed class CommandContextDesignTimeFactory : IDesignTimeDbContextFactory<CommandContext>
{
    public CommandContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CommandContext>();
        var connectionString = Environment.GetEnvironmentVariable("NSUITE_CONNECTION_STRING")
            ?? "Server=localhost;Database=nSuite;Trusted_Connection=True;TrustServerCertificate=True";

        optionsBuilder.UseSqlServer(connectionString);
        return new CommandContext(optionsBuilder.Options, new DesignTimeOperatorContext());
    }
}
