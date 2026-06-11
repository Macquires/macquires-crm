namespace ASPNET.BackEnd.Common.Models;

public class ApiErrorResult
{
    public int? Code { get; init; }
    public string? Message { get; init; }
    public string? MessageAr { get; init; }
    public string? MessageEn { get; init; }
    public Error? Error { get; init; }
}
