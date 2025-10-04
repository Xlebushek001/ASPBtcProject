using ComparisonService.Services;

var builder = WebApplication.CreateBuilder(args);

// ��������� URL ��� HTTP
builder.WebHost.UseUrls("http://*:80");

// ���������� �������� � ���������
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ��������� HttpClient ��� ��������
builder.Services.AddHttpClient("BinanceService", client =>
{
    client.BaseAddress = new Uri("http://binance-service/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("BybitService", client =>
{
    client.BaseAddress = new Uri("http://bybit-service/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ����������� ��������
builder.Services.AddScoped<IComparisonService, ComparisonServices>();

// ��������� �����������
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

// ���������� �������� ��������
builder.Services.AddHealthChecks();

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
app.MapGet("/", () => "Service is running! /swagger");

app.Run();