namespace FinanceApp.Application.Abstractions;

/// <summary>
/// Reconstructs investment positions from transaction history
/// using weighted average price (preço médio ponderado).
/// </summary>
public interface ICustodyService
{
    /// <summary>
    /// Recalculates quantity and average price of an investment from confirmed transactions.
    /// </summary>
    Task RecalculatePositionAsync(Guid userId, Guid investmentId, CancellationToken ct);

    /// <summary>
    /// Recalculates all investment positions for a user.
    /// </summary>
    Task RecalculateAllPositionsAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Returns total accumulated yield (dividends/interest) for an investment.
    /// </summary>
    Task<decimal> GetAccumulatedYieldAsync(Guid userId, Guid investmentId, CancellationToken ct);

    /// <summary>
    /// Calculates the annualized historical return of an investment based on snapshots.
    /// Returns null if insufficient history (&lt;30 days).
    /// </summary>
    Task<decimal?> GetHistoricalAnnualReturnAsync(Guid userId, Guid investmentId, CancellationToken ct);
}
