using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Services;

public static class BalanceCalculator
{
    public static decimal CalculateConfirmedDelta(IEnumerable<Transaction> transactions, Guid accountId)
    {
        decimal sum = 0m;

        foreach (var transaction in transactions.Where(x => x.Status == TransactionStatuses.Confirmed && !x.IsDeleted))
        {
            if (transaction.TransactionType == TransactionTypes.Transfer)
            {
                if (transaction.AccountId == accountId)
                    sum -= transaction.EffectiveAmount;
                if (transaction.DestinationAccountId == accountId)
                    sum += transaction.EffectiveAmount;
                continue;
            }

            if (transaction.AccountId == accountId)
                sum += transaction.SignedAmount();
        }

        return sum;
    }
}
