using KEDA_EdgeServices.Configs;
using KEDA_EdgeServices.Extensions;
using KEDA_EdgeServices.Helpers;
using KEDA_EdgeServices.Protocols;
using KEDA_EdgeServices.Protocols.Attributes;
using KEDA_EdgeServices.Protocols.Factory;
using KEDA_EdgeServices.Protocols.FreeProtocols;
using KEDA_EdgeServices.Protocols.Interfaces;
using KEDA_EdgeServices.Services;
using Serilog;

namespace KEDA_EdgeServices;
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
               .MinimumLevel.Information()
               .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
               .MinimumLevel.Override("System.Net.Http.HttpClient", Serilog.Events.LogEventLevel.Warning)
               .WriteTo.File(
                   path: Path.Combine(AppContext.BaseDirectory, "Logs", "log-edge-.txt"),
                   rollingInterval: RollingInterval.Day,
                   retainedFileCountLimit: 7,
                   outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
               .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
        });

        builder.WebHost.ConfigureKestrel((context, options) =>
        {
            var kestrelConfig = context.Configuration.GetSection("Kestrel");
            options.Configure(kestrelConfig);
        });

        builder.Services.AddAuthorization();

        var adapterTypes = typeof(IProtocolAdapter).Assembly
            .GetTypes()
            .Where(t => typeof(IProtocolAdapter).IsAssignableFrom(t)
            && !t.IsAbstract
            && t.GetCustomAttributes(typeof(ProtocolTypeAttribute), false).Length != 0);

        foreach(var type in adapterTypes)
        {
            builder.Services.AddTransient(type);
        }

        builder.Services.AddSingleton<ValidateHelper>();
        builder.Services.AddSingleton<Global>();
        builder.Services.AddSingleton<ProtocolAdapterFactory>();
        builder.Services.AddSingleton<ProtocolEngineService>();

        var app = builder.Build();

        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        if (!ActiveHsl(builder.Configuration, logger))
        {
            Console.WriteLine("Hsl认证失败!");
            return;
        }

        logger.LogInformation("应用程序启动");

        app.MapProtocolEngineApis();

        app.UseAuthorization();

        app.Run();
    }

    private static bool ActiveHsl(ConfigurationManager configuration, Microsoft.Extensions.Logging.ILogger logger)
    {
        var hslAuthCode = configuration["HslCommunication:Auth"];
        if (!HslCommunication.Authorization.SetAuthorizationCode(hslAuthCode))
        {
            logger.LogError("----------------------Hsl验证失败----------------------");
            return false;
        }
        else
        {
            logger.LogInformation("----------------------Hsl验证成功----------------------");
            return true;
        }
    }
}
