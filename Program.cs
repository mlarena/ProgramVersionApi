using ProgramVersionApi.Services;
using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Настройка Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    // Логи событий (все информационные логи)
    .WriteTo.Logger(lc => lc
        .Filter.ByExcluding("Level = LogEventLevel.Error or Level = LogEventLevel.Fatal")
        .WriteTo.File(
            path: "logs/event/.log",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"))
    // Логи ошибок
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly("Level = LogEventLevel.Error or Level = LogEventLevel.Fatal")
        .WriteTo.File(
            path: "logs/error/err_.log",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"))
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ProgramService>();

var app = builder.Build();

// Глобальная обработка исключений
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
        if (contextFeature != null)
        {
            var remoteIp = context.Connection.RemoteIpAddress?.ToString();
            Log.Error(contextFeature.Error, "Unhandled exception occurred. IP: {RemoteIP}, Path: {Path}", 
                remoteIp, context.Request.Path);

            await context.Response.WriteAsJsonAsync(new
            {
                StatusCode = context.Response.StatusCode,
                Message = "Internal Server Error. Please try again later.",
                Detailed = app.Environment.IsDevelopment() ? contextFeature.Error.Message : null
            });
        }
    });
});

// Middleware для логирования IP и деталей запроса
app.Use(async (context, next) =>
{
    var remoteIp = context.Connection.RemoteIpAddress?.ToString();
    using (Serilog.Context.LogContext.PushProperty("RemoteIP", remoteIp))
    using (Serilog.Context.LogContext.PushProperty("UserAgent", context.Request.Headers["User-Agent"].ToString()))
    {
        await next();
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

try
{
    Log.Information("Starting web host");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}