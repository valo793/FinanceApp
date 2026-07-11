using FinanceApp.Application.Abstractions;
using FinanceApp.Infrastructure.Persistence;
using FinanceApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<FinanceDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IRecurringService, RecurringService>();
        services.AddScoped<IInvestmentService, InvestmentService>();
        services.AddScoped<IUserPreferenceService, UserPreferenceService>();
        services.AddScoped<IProjectionService, ProjectionService>();
        services.AddHttpClient<IAssetPriceService, YahooFinancePriceService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();

        return services;
    }
}

