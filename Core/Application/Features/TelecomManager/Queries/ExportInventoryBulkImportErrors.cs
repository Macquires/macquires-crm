using System.Globalization;
using System.Text;
using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public class ExportInventoryBulkImportErrorsResult
{
    public byte[] FileBytes { get; init; } = Array.Empty<byte>();
    public string FileName { get; init; } = "bulk-import-errors.csv";
    public string ContentType { get; init; } = "text/csv";
}

public class ExportInventoryBulkImportErrorsRequest : IRequest<ExportInventoryBulkImportErrorsResult>, IRequireAnyPermission
{
    public string JobId { get; init; } = "";
    public IReadOnlyList<string> PermissionKeys => BulkImportPermissionSets.MonitorAny;
}

public class ExportInventoryBulkImportErrorsValidator : AbstractValidator<ExportInventoryBulkImportErrorsRequest>
{
    public ExportInventoryBulkImportErrorsValidator() => RuleFor(x => x.JobId).NotEmpty();
}

public class ExportInventoryBulkImportErrorsHandler
    : IRequestHandler<ExportInventoryBulkImportErrorsRequest, ExportInventoryBulkImportErrorsResult>
{
    private readonly IQueryContext _query;
    private readonly IUserScopeService _userScope;
    private readonly IOperatorContext _operator;

    public ExportInventoryBulkImportErrorsHandler(
        IQueryContext query,
        IUserScopeService userScope,
        IOperatorContext operatorContext)
    {
        _query = query;
        _userScope = userScope;
        _operator = operatorContext;
    }

    public async Task<ExportInventoryBulkImportErrorsResult> Handle(
        ExportInventoryBulkImportErrorsRequest request,
        CancellationToken cancellationToken)
    {
        var job = await _query.InventoryBulkImportJob.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken);
        if (job == null)
        {
            return new ExportInventoryBulkImportErrorsResult();
        }

        var actorUserId = OperatorActor.RequireUserId(_operator);
        if (!string.IsNullOrEmpty(job.CreatedById)
            && !await _userScope.CanAccessUserAsync(actorUserId, job.CreatedById, cancellationToken)
            && !await _userScope.IsUnrestrictedAdminAsync(actorUserId, cancellationToken))
        {
            return new ExportInventoryBulkImportErrorsResult();
        }

        var errors = await _query.InventoryBulkImportError.AsNoTracking().IsDeletedEqualTo()
            .Where(e => e.JobId == request.JobId)
            .OrderBy(e => e.RowNumber)
            .Select(e => new { e.RowNumber, e.Identifier, e.ErrorMessageAr, e.ErrorMessageEn })
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("RowNumber,Identifier,ErrorMessageAr,ErrorMessageEn");
        foreach (var e in errors)
        {
            sb.Append(e.RowNumber.ToString(CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(CsvEscape(e.Identifier));
            sb.Append(',');
            sb.Append(CsvEscape(e.ErrorMessageAr));
            sb.Append(',');
            sb.Append(CsvEscape(e.ErrorMessageEn));
            sb.AppendLine();
        }

        var fileName = $"bulk-import-errors-{request.JobId[..Math.Min(8, request.JobId.Length)]}.csv";
        return new ExportInventoryBulkImportErrorsResult
        {
            FileBytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray(),
            FileName = fileName
        };
    }

    private static string CsvEscape(string? value)
    {
        var v = value ?? "";
        if (v.Contains('"') || v.Contains(',') || v.Contains('\n'))
        {
            return "\"" + v.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        return v;
    }
}
