using HslCommunication.Instrument.CJT;
using HslCommunication.Profinet.Omron;
using KEDA_EdgeServices.Configs;
using KEDA_EdgeServices.Models;
using KEDA_EdgeServices.Protocols.Attributes;
using KEDA_EdgeServices.Protocols.Base;

namespace KEDA_EdgeServices.Protocols.Tcp;

[ProtocolType("CJT188OverTcp_2004")]
[ProtocolType("CJT188OverTcp2004")]
[ProtocolType("CJT188OverTcp")]
public class CJT188OverTcp_2004Adapter : CJT188ProtocolAdapterBase<CJT188OverTcp>
{
    public CJT188OverTcp_2004Adapter(ILogger<CJT188OverTcp_2004Adapter> logger, Global global) : base(logger, global)
    {
    }

    protected override string ProtocolType => "CJT188OverTcp_2004";

    private LanConfig? _lastConfig;
    protected override void InitOrReset(Protocol protocol)
    {
        byte instrumentType = protocol.InstrumentType.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                                   ? Convert.ToByte(protocol.InstrumentType.Replace("0x", ""), 16)
                                   : byte.Parse(protocol.InstrumentType);
        // 构造当前参数
        var config = new LanConfig
        {
            Ip = protocol.IPAddress,
            Port = int.Parse(protocol.ProtocolPort),
        };

        if (_connection == null || _lastConfig == null || !_lastConfig.Equals(config) || protocol.ResetConnection)
        {
            _connection = new("1")
            {
                IpAddress = config.Ip,
                Port = config.Port,
            };

            var res = _connection.ConnectServer();

            if (!res.IsSuccess)
            {
                if (protocol.IsLogPoints)
                    _logger.LogError($"{ProtocolType}连接失败: {res.Message}");
                throw new IOException($"{ProtocolType}连接失败: {res.Message}");
            }
            _lastConfig = config;
        }

        _connection.InstrumentType = instrumentType;
        _connection.ReceiveTimeOut = int.Parse(protocol.ReceiveTimeOut);
        _connection.ConnectTimeOut = int.Parse(protocol.ConnectTimeOut);
    }
}
