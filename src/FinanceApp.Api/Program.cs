using System.Text;
using Asp.Versioning;
using FinanceApp.Api.Middleware;
using FinanceApp.Application.Validation;
using FinanceApp.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console();
});

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

builder.Services.AddScoped<IValidator<FinanceApp.Contracts.Auth.LoginRequest>, LoginRequestValidator>();
builder.Services.AddScoped<IValidator<FinanceApp.Contracts.Accounts.CreateAccountRequest>, CreateAccountRequestValidator>();
builder.Services.AddScoped<IValidator<FinanceApp.Contracts.Transactions.UpsertTransactionRequest>, UpsertTransactionRequestValidator>();

var key = builder.Configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("JWT signing key not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            NameClaimType = "sub"
        };
    });

builder.Services.AddAuthorization();

// builder.Services.AddOpenTelemetry()
//     .ConfigureResource(resource => resource.AddService("FinanceApp.Api"))
//     .WithTracing(tracing =>
//     {
//         tracing
//             .AddAspNetCoreInstrumentation()
//             .AddHttpClientInstrumentation()
//             .AddConsoleExporter();
//     })
//     .WithMetrics(metrics =>
//     {
//         metrics
//             .AddAspNetCoreInstrumentation()
//             .AddRuntimeInstrumentation()
//             .AddConsoleExporter();
//     });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Type = "https://httpstatuses.com/400"
        };

        return new BadRequestObjectResult(problem);
    };
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapOpenApi();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
