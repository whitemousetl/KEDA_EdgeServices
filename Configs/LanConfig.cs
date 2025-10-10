namespace KEDA_EdgeServices.Configs;

public class LanConfig
{
    public string Ip { get; set; } = string.Empty;
    public int Port { get; set; }


    public override bool Equals(object? obj)
    {
        if (obj is not LanConfig other) return false;
        return Ip == other.Ip &&
            Port == other.Port;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Ip, Port);
    }
}
