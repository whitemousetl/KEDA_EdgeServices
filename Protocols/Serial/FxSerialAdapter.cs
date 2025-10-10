using HslCommunication.ModBus;
using HslCommunication.Profinet.Melsec;
using KEDA_EdgeServices.Configs;
using KEDA_EdgeServices.Models;
using KEDA_EdgeServices.Protocols.Attributes;
using KEDA_EdgeServices.Protocols.Base;
using System.IO.Ports;

namespace KEDA_EdgeServices.Protocols.Serial;

[ProtocolType("FxSerial")]
public class FxSerialAdapter : ProtocolAdapterBase<MelsecFxSerial>
{
    public FxSerialAdapter(ILogger<FxSerialAdapter> logger, Global global) : base(logger, global)
    {
    }

    protected override string ProtocolType => "FxSerial";

    private SerialPortConfig? _lastConfig;
    protected override void InitOrReset(Protocol protocol)
    {
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
            _connection = new();
            _connection.SerialPortInni(config.PortName, config.BaudRate, config.DataBits, config.StopBits, config.Parity);
            var openResult = _connection.Open();
            if (!openResult.IsSuccess)
            {
                if (protocol.IsLogPoints)
                    _logger.LogError($"FxSerial串口打开失败: {openResult.Message}");
                throw new IOException($"FxSerial串口打开失败: {openResult.Message}");
            }
            _lastConfig = config;
        }

        _connection.ReceiveTimeOut = int.Parse(protocol.ReceiveTimeOut);
    }
}
