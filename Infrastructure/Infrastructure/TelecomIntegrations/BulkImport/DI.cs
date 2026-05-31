using Application.Common.BulkImport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.TelecomIntegrations.BulkImport;

public static class BulkImportDI
{
    public static IServiceCollection RegisterBulkImport(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BulkImportOptions>(configuration.GetSection(BulkImportOptions.SectionName));
        services.AddSingleton<IBulkImportFileStore, BulkImportFileStore>();
        services.AddSingleton<IBulkImportErrorSanitizer, BulkImportErrorSanitizer>();
        services.AddScoped<IBulkImportJobProcessor, MsisdnAssetBulkImportProcessor>();
        services.AddScoped<IBulkImportJobProcessor, CustomerProfilesBulkImportProcessor>();
        services.AddScoped<IBulkImportJobProcessor, PackageMigrationBulkImportProcessor>();
        services.AddScoped<IBulkImportProcessorResolver, BulkImportProcessorResolver>();
        return services;
    }
}
