using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ���������� HTTPS
builder.WebHost.UseUrls("http://*:80");

// ���������� �������� � ���������
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ���������� Ocelot
builder.Services.AddOcelot();
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();
 
await app.UseOcelot();

app.Run();