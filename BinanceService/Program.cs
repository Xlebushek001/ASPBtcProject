using BinanceService.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Используем только HTTP
builder.WebHost.UseUrls("http://*:80");

// Добавляем сервисы в контейнер
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Настраиваем HttpClient
builder.Services.AddHttpClient<IBinanceService, BinanceServices>(client =>
{
client.BaseAddress = new Uri("https://api.binance.com/");
client.DefaultRequestHeaders.Add("Accept", "application/json");
client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<IBinanceService, BinanceServices>();

// Настраиваем логирование
builder.Services.AddLogging(logging =>
{
logging.AddConsole();
logging.AddDebug();
});

// Добавляем проверки здоровья
builder.Services.AddHealthChecks()
    .AddCheck<BinanceHealthCheck>("binance-api");

var app = builder.Build();

// Настраиваем конвейер HTTP запросов
if (app.Environment.IsDevelopment())
{
app.UseSwagger();
app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// Эндпоинт для проверки здоровья
app.MapHealthChecks("/health");

// Добавляем этот эндпоинт для проверки
app.MapGet("/", () => "Binance Service is running! /swagger");

app.Run();

// Класс проверки здоровья
public class BinanceHealthCheck : IHealthCheck
{
    private readonly IBinanceService _binanceService;

    public BinanceHealthCheck(IBinanceService binanceService)
    {
        _binanceService = binanceService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var price = await _binanceService.GetPriceAsync();
            return price > 0
                ? HealthCheckResult.Healthy("Binance API available")
                : HealthCheckResult.Unhealthy("Binance API returned the unacceptable price"); 
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Binance API unavailable", ex);
        }
    }
}