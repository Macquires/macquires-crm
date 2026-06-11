namespace Application.Common.Telecom.SellingLine;

public interface IDealerCodeValidator
{
    Task<bool> ExistsAsync(string dealerCode, CancellationToken cancellationToken);
}
