using FinanceApp.Infrastructure;
using FinanceApp.Worker;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<RecurringMaterializationWorker>();

builder.Services.AddSerilog(config => config.WriteTo.Console());

var host = builder.Build();
await host.RunAsync();
