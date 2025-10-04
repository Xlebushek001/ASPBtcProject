using BybitService.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Настройка URL для HTTP
builder.WebHost.UseUrls("http://*:80");

// Добавление сервисов в контейнер
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Настройка HttpClient
builder.Services.AddHttpClient<IBybitService, BybitServices>(client =>
{
    client.BaseAddress = new Uri("https://api.bybit.com/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Регистрация сервисов
builder.Services.AddScoped<IBybitService, BybitServices>();

// Настройка логирования
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

// Добавление проверок здоровья
builder.Services.AddHealthChecks()
    .AddCheck<BybitHealthCheck>("bybit-api");

var app = builder.Build();

// Настройка конвейера HTTP запросов
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// Конечная точка проверки здоровья
app.MapHealthChecks("/health");

// Главная страница
app.MapGet("/", () => "Service Bybit is running! /swagger");

app.Run();

// Класс проверки здоровья
public class BybitHealthCheck : IHealthCheck
{
    private readonly IBybitService _bybitService;

    public BybitHealthCheck(IBybitService bybitService)
    {
        _bybitService = bybitService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var price = await _bybitService.GetPriceAsync();
            return price > 0
                ? HealthCheckResult.Healthy("API Bybit available")
                : HealthCheckResult.Unhealthy("API Bybit returned the unacceptable price");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("API Bybit unavailable", ex);
        }
    }
}