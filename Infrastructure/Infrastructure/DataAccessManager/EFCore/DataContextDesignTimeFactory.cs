using Application.Common.Security;
using Application.Common.Services.SecurityManager;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.DataAccessManager.EFCore;

public sealed class DataContextDesignTimeFactory : IDesignTimeDbContextFactory<DataContext>
{
    public DataContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
        var connectionString = Environment.GetEnvironmentVariable("NSUITE_CONNECTION_STRING")
            ?? "Server=localhost;Database=nSuite;Trusted_Connection=True;TrustServerCertificate=True";

        optionsBuilder.UseSqlServer(connectionString);
        return new DataContext(optionsBuilder.Options, new DesignTimeOperatorContext());
    }

    private sealed class DesignTimeOperatorContext : IOperatorContext
    {
        public string? UserId => "design-time";
        public IReadOnlyList<string> Roles { get; } = [TelecomEnterpriseRoleMatrix.RoleAdmin];
        public TelecomMenuPersona? EffectivePersona => null;
        public string? BranchId => null;
        public bool IsAuthenticated => true;
    }
}
