using HslCommunication.Instrument.DLT;
using HslCommunication.ModBus;
using KEDA_EdgeServices.Configs;
using KEDA_EdgeServices.Models;
using KEDA_EdgeServices.Protocols.Attributes;
using KEDA_EdgeServices.Protocols.Base;
using System.IO.Ports;

namespace KEDA_EdgeServices.Protocols;

[ProtocolType("DLT645_2007Serial")]
[ProtocolType("DLT6452007Serial")]
public class DLT645_2007SerialAdapter : DLT645_2007ProtocolAdapterBase<DLT645>
{
    public DLT645_2007SerialAdapter(ILogger<DLT645_2007SerialAdapter> logger, Global global) : base(logger, global)
    {
    }

    protected override string ProtocolType => "DLT645_2007Serial";

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
            _connection = new ();
            _connection.SerialPortInni(config.PortName, config.BaudRate, config.DataBits, config.StopBits, config.Parity);
            var openResult = _connection.Open();
            if (!openResult.IsSuccess)
            {
                if (protocol.IsLogPoints)
                    _logger.LogError($"DLT645_2007Serial串口打开失败: {openResult.Message}");
                throw new IOException($"DLT645_2007Serial串口打开失败: {openResult.Message}");
            }
            _lastConfig = config;
        }

        _connection.ReceiveTimeOut = int.Parse(protocol.ReceiveTimeOut);
    }
}
