using BybitService.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// ��������� URL ��� HTTP
builder.WebHost.UseUrls("http://*:80");

// ���������� �������� � ���������
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ��������� HttpClient
builder.Services.AddHttpClient<IBybitService, BybitServices>(client =>
{
    client.BaseAddress = new Uri("https://api.bybit.com/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ����������� ��������
builder.Services.AddScoped<IBybitService, BybitServices>();

// ��������� �����������
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

// ���������� �������� ��������
builder.Services.AddHealthChecks()
    .AddCheck<BybitHealthCheck>("bybit-api");

var app = builder.Build();

// ��������� ��������� HTTP ��������
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// �������� ����� �������� ��������
app.MapHealthChecks("/health");

// ������� ��������
app.MapGet("/", () => "Service Bybit is running! /swagger");

app.Run();

// ����� �������� ��������
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