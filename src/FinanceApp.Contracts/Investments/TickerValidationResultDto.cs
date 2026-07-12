namespace FinanceApp.Contracts.Investments;

public sealed class TickerValidationResultDto
{
    public bool IsValid { get; init; }
    public string? Name { get; init; }
    public decimal CurrentPrice { get; init; }
    public string CurrencyCode { get; init; } = "BRL";
    public string AssetType { get; init; } = "stock"; // stock, fii, fund, crypto, etc.
}
