using System.IO.Ports;

namespace KEDA_EdgeServices.Configs;

public class SerialPortConfig : IEquatable<SerialPortConfig>
{
    public string PortName { get; set; } = string.Empty;
    public int BaudRate { get; set; }
    public int DataBits { get; set; }
    public StopBits StopBits { get; set; }
    public Parity Parity { get; set; }

    public bool Equals(SerialPortConfig? other)
    {
        if (other == null) return false;
        return PortName == other.PortName
            && BaudRate == other.BaudRate
            && DataBits == other.DataBits
            && StopBits == other.StopBits
            && Parity == other.Parity;
    }

    public override bool Equals(object? obj) => Equals(obj as SerialPortConfig);
    public override int GetHashCode() => HashCode.Combine(PortName, BaudRate, DataBits, StopBits, Parity);
}