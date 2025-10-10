namespace KEDA_EdgeServices.Configs;

public class Global
{
    private readonly IConfiguration _config;
    public int LogInterval;
    public int ReadDelay;
    public int WriteDelay;
    public Global(IConfiguration config)
    {
        _config = config;
        LogInterval = _config.GetValue("LogInterval", 15);
        ReadDelay = _config.GetValue<int>("Delay:ReadDelay", 500);
        WriteDelay = _config.GetValue<int>("Delay:WriteDelay", 500);
    }
}
