using System;

namespace FinanceApp.Desktop.Exceptions;

public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException() : base("Os dados foram modificados por outro usuário ou sessão. Por favor, recarregue a página.")
    {
    }

    public ConcurrencyConflictException(string message) : base(message)
    {
    }

    public ConcurrencyConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
