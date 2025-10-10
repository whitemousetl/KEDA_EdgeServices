using HslCommunication.Instrument.CJT;
using HslCommunication.ModBus;
using KEDA_EdgeServices.Configs;
using KEDA_EdgeServices.Models;
using KEDA_EdgeServices.Protocols.Attributes;
using KEDA_EdgeServices.Protocols.Base;
using System.IO.Ports;

namespace KEDA_EdgeServices.Protocols.Serial;

[ProtocolType("CJT188Serial_2004")]
[ProtocolType("CJT188Serial2004")]
[ProtocolType("CJT188Serial")]
public class CJT188Serial_2004Adapter : CJT188ProtocolAdapterBase<CJT188>
{
    public CJT188Serial_2004Adapter(ILogger<CJT188Serial_2004Adapter> logger, Global global) : base(logger, global)
    {
    }

    protected override string ProtocolType => "CJT188Serial_2004";

    private SerialPortConfig? _lastConfig;
    protected override void InitOrReset(Protocol protocol)
    {
        byte instrumentType = protocol.InstrumentType.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                                  ? Convert.ToByte(protocol.InstrumentType.Replace("0x", ""), 16)
                                  : byte.Parse(protocol.InstrumentType);

        // 构造当前参数
        var config = new SerialPortConfig
        {
            PortName = protocol!.PortName,
            BaudRate = int.Parse(protocol.BaudRate),
            DataBits = int.Parse(protocol.DataBits),
            StopBits = Enum.Parse<StopBits>(protocol.StopBits),
            Parity = Enum.Parse<Parity>(protocol.Parity),
        };

        if (_connection == null || _lastConfig == null || !_lastConfig.Equals(config) || protocol.ResetConnection)
        {
            _connection?.Close();
            _connection = new("1");
            _connection.SerialPortInni(config.PortName, config.BaudRate, config.DataBits, config.StopBits, config.Parity);
            var openResult = _connection.Open();
            if (!openResult.IsSuccess)
            {
                if (protocol.IsLogPoints)
                    _logger.LogError($"{ProtocolType}串口打开失败: {openResult.Message}");
                throw new IOException($"{ProtocolType}串口打开失败: {openResult.Message}");
            }
            _lastConfig = config;
        }

        _connection.ReceiveTimeOut = int.Parse(protocol.ReceiveTimeOut);
        _connection.InstrumentType = instrumentType;
    }
}
