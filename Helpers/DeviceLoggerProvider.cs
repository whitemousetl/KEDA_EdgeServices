using Serilog;
using System.Collections.Concurrent;
using ILogger = Serilog.ILogger;

namespace KEDA_EdgeServices.Helpers;
public static class DeviceLoggerProvider
{
    private static readonly ConcurrentDictionary<string, ILogger> _loggers = new();

    public static ILogger GetLogger(string equipmentId, string remark)
    {
        var date = DateTime.Now.ToString("yyyyMMdd");
        var logDir = Path.Combine(AppContext.BaseDirectory, "Log_Devices", date);
        Directory.CreateDirectory(logDir);

        var logFileName = $"{remark}_{equipmentId}_{date}.txt";
        var logFilePath = Path.Combine(logDir, logFileName);

        // 以remark+equipmentId+date为key，确保同一天同设备同备注只创建一个logger
        var loggerKey = $"{remark}_{equipmentId}_{date}";

        return _loggers.GetOrAdd(loggerKey, _ =>
        {
            return new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(
                    path: logFilePath,
                    rollingInterval: RollingInterval.Infinite, // 不再按天分割，文件名已包含日期
                    retainedFileCountLimit: 3,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    buffered: false)
                .CreateLogger();
        });
    }
}
