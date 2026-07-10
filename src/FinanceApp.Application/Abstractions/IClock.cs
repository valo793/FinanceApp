namespace FinanceApp.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
    DateOnly Today(string timezone = "America/Sao_Paulo");
}
