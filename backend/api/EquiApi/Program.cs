using EquiApi;
using EquiApi.BackgroundServices;
using EquiApi.Shared;
using EquiApi.Util;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

bool isDev = builder.Environment.IsDevelopment();
var configurationManager = builder.Configuration;
var settings = builder.Services.LoadAndConfigureSettings(configurationManager);

builder.AddLogging();
builder.Services.AddApplicationServices(configurationManager, isDev);
builder.Services.AddOpenApi();
builder.Services.AddCors(settings);
builder.Services.AddKeycloakAuthentication(configurationManager);
builder.Services.AddProblemDetails();
builder.Services.AddControllers(o => { o.ModelBinderProviders.Insert(0, new NodaTimeModelBinderProvider()); })
       .AddJsonOptions(o => ConfigureJsonSerialization(o, isDev));
builder.Services.ConfigureAdditionalRouteConstraints();

builder.Services.AddOptions<BluetoothOptions>().BindConfiguration(BluetoothOptions.SectionName);
builder.Services.AddSingleton<IBluetoothDataHandler, LoggingBluetoothDataHandler>();
builder.Services.AddHostedService<BluetoothConnectionService>();

var app = builder.Build();

// not using HTTPS, because all production backends _have_ to be behind a reverse proxy which will handle SSL termination

app.UseCors(Setup.CorsPolicyName);
app.UseMiddleware<ExceptionHandlingMiddleware>();
// chat events are pushed to the clients over websockets, the interval keeps idle connections open behind proxies
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (isDev && settings.SeedDevelopmentData)
{
    await app.SeedDevelopmentDataAsync();
}

await app.RunAsync();

return;

static void ConfigureJsonSerialization(JsonOptions options, bool isDev)
{
    JsonConfig.ConfigureJsonSerialization(options.JsonSerializerOptions, isDev);
}

// used for integration testing
public partial class Program;