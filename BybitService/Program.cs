using BybitService.Services;
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
    c.SwaggerDoc("v1", new() { Title = "Bybit Service API", Version = "v1" });
});

// Конфигурация Redis
var redisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString")
    ?? "redis-crypto:6379";

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "BybitService";
});

// Конфигурация опций Bybit
builder.Services.Configure<BybitServiceOptions>(options =>
{
    options.BaseUrl = builder.Configuration["Bybit:BaseUrl"] ?? "https://api.bybit.com/";
    options.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("Bybit:TimeoutSeconds", 30));
    options.MaxRetries = builder.Configuration.GetValue<int>("Bybit:MaxRetries", 3);
    options.CacheDurationSeconds = builder.Configuration.GetValue<int>("Bybit:CacheDurationSeconds", 2);
});

// HTTP клиент
builder.Services.AddHttpClient<IBybitService, BybitServices>()
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        var options = serviceProvider.GetRequiredService<IOptions<BybitServiceOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl);
        client.Timeout = options.Timeout;
    });

// Сервисы приложения
builder.Services.AddScoped<IBybitService, BybitServices>();
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
    .AddCheck<BybitHealthCheck>("bybit-api")
    .AddRedis(redisConnectionString, name: "redis-cache");

var app = builder.Build();

// Конвейер middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bybit Service API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/", () => "Bybit Service is running! Visit /swagger for API documentation.");

app.Run();

// Health check
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
                ? HealthCheckResult.Healthy("Bybit API available")
                : HealthCheckResult.Unhealthy("Bybit API returned the unacceptable price");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Bybit API unavailable", ex);
        }
    }
}