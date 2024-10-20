using System.Net;
using System.Net.WebSockets;
using System.Text;
using Microsoft.EntityFrameworkCore;
using webapp.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)
);

builder.Services.AddScoped(provider => new Orquestrator(
    builder.Configuration.GetConnectionString("RedisConnection")
     ?? Environment.GetEnvironmentVariable("RedisConnection")
        ?? throw new InvalidOperationException("Redis erro")
));

var app = builder.Build();
app.UseWebSockets();
app.AddWebSocketServices();

await app.RunAsync();
