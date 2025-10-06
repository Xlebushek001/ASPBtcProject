using BinanceService.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Конфигурация
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// Используем только HTTP
builder.WebHost.UseUrls("http://*:80");

// Сервисы
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Binance Service API", Version = "v1" });
});

// Конфигурация Redis
var redisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString")
    ?? "localhost:6379";

// Добавляем распределенный кэш Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "BinanceService";
});

// Конфигурация опций Binance
builder.Services.Configure<BinanceServiceOptions>(options =>
{
    options.ApiKey = builder.Configuration["Binance:ApiKey"]
        ?? "ijWwhpqpPJ7OukY5Z3J7o0eplhYgPAC2byj5ndFiAxvBZM9MnesVYOemEvvgsDO4";
    options.BaseUrl = builder.Configuration["Binance:BaseUrl"] ?? "https://api.binance.com/";
    options.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("Binance:TimeoutSeconds", 30));
    options.MaxRetries = builder.Configuration.GetValue<int>("Binance:MaxRetries", 3);
    options.CacheDurationSeconds = builder.Configuration.GetValue<int>("Binance:CacheDurationSeconds", 2);
    options.CacheDurationSeconds = builder.Configuration.GetValue<int>("Binance:CacheDurationSeconds", 5);
});

// HTTP клиент
builder.Services.AddHttpClient<IBinanceService, BinanceServices>()
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        var options = serviceProvider.GetRequiredService<IOptions<BinanceServiceOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl);
        client.DefaultRequestHeaders.Add("X-MBX-APIKEY", options.ApiKey);
        client.Timeout = options.Timeout;
    });

// Сервисы приложения
builder.Services.AddScoped<IBinanceService, BinanceServices>();
builder.Services.AddScoped<IRedisService, RedisService>();

// Логирование
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
});

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck<BinanceHealthCheck>("binance-api")
    .AddRedis(redisConnectionString, name: "redis-cache");

var app = builder.Build();

// Конвейер middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Binance Service API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/", () => "Binance Service is running! Visit /swagger for API documentation.");

app.Run();

// Health check
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