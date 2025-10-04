using ComparisonService.Services;

var builder = WebApplication.CreateBuilder(args);

// Настройка URL для HTTP
builder.WebHost.UseUrls("http://*:80");

// Добавление сервисов в контейнер
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Настройка HttpClient для сервисов
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

// Регистрация сервисов
builder.Services.AddScoped<IComparisonService, ComparisonServices>();

// Настройка логирования
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

// Добавление проверок здоровья
builder.Services.AddHealthChecks();

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
app.MapGet("/", () => "Service is running! /swagger");

app.Run();