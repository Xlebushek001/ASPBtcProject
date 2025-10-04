using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Отключение HTTPS
builder.WebHost.UseUrls("http://*:80");

// Добавление сервисов в контейнер
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Добавление Ocelot
builder.Services.AddOcelot();
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();
 
await app.UseOcelot();

app.Run();