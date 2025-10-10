namespace KEDA_EdgeServices.Models;

public class Workstation
{
    #region 额外增加的两个属性，用于排序
    public string Time { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    #endregion
    public string EdgeID { get; set; } = string.Empty;
    public string EdgeName { get; set; } = string.Empty;
    public List<Protocol> Protocols { get; set; } = [];
}
public class Protocol
{
    public string ProtocolID { get; set; } = string.Empty;
    public string Interface { get; set; } = string.Empty;
    public string ProtocolType { get; set; } = string.Empty;
    public string IPAddress { get; set; } = string.Empty;
    public string Gateway { get; set; } = string.Empty;
    public string ProtocolPort { get; set; } = string.Empty;
    public string StationNo { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string PortName { get; set; } = string.Empty;
    public string BaudRate { get; set; } = string.Empty;
    public string DataBits { get; set; } = string.Empty;
    public string StopBits { get; set; } = string.Empty;
    public string Parity { get; set; } = string.Empty;
    public string AddressStartWithZero { get; set; } = string.Empty;
    public string Remark { get; set; } = string.Empty;
    public string InstrumentType { get; set; } = string.Empty;
    public string CollectCycle { get; set; } = string.Empty;
    public string ReceiveTimeOut { get; set; } = string.Empty;
    public string ConnectTimeOut { get; set; } = string.Empty;
    public string RemoteRemark { get; set; } = string.Empty;
    public string EdgeID { get; set; } = string.Empty;
    public List<Device> Devices { get; set; } = [];
    public bool ResetConnection { get; set; } = false;
    public bool IsLogPoints { get; set; } = false;
}

public class Device
{
    public string EquipmentID { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string EdgeID { get; set; } = string.Empty;
    public string ProtocolID { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<Point> Points { get; set; } = [];
    public string StationNo { get; set; } = string.Empty;
}

public class Point
{
    public string Label { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string MemoryType { get; set; } = string.Empty;
    public string MemoryArea { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string BitAdderss { get; set; } = string.Empty;
    public string Length { get; set; } = string.Empty;
    public string Default { get; set; } = string.Empty;
    public string Cycle { get; set; } = string.Empty;
    public string Change { get; set; } = string.Empty;
    public string RChange { get; set; } = string.Empty;
    public string MinValue { get; set; } = string.Empty;
    public string MaxValue { get; set; } = string.Empty;
    public string TableType { get; set; } = string.Empty;
    public string FieldTable { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public string IsOpc { get; set; } = string.Empty;
    public string IsWrite { get; set; } = string.Empty;
    public string IsControl { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string WriteValue { get; set; } = string.Empty;
}

