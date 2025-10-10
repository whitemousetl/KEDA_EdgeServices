using HslCommunication.Instrument.DLT;
using HslCommunication.Profinet.Omron;
using KEDA_EdgeServices.Configs;
using KEDA_EdgeServices.Models;
using KEDA_EdgeServices.Protocols.Attributes;
using KEDA_EdgeServices.Protocols.Base;

namespace KEDA_EdgeServices.Protocols.Tcp;

[ProtocolType("DLT645_2007OverTcp")]
[ProtocolType("DLT6452007OverTcp")]
public class DLT645_2007OverTcpAdapter : DLT645_2007ProtocolAdapterBase<DLT645OverTcp>
{
    public DLT645_2007OverTcpAdapter(ILogger<DLT645_2007OverTcpAdapter> logger, Global global) : base(logger, global)
    {
    }

    protected override string ProtocolType => "DLT645_2007OverTcp";

    private LanConfig? _lastConfig;
    protected override void InitOrReset(Protocol protocol)
    {
        // 构造当前参数
        var config = new LanConfig
        {
            Ip = protocol.IPAddress,
            Port = int.Parse(protocol.ProtocolPort),
        };

        if (_connection == null || _lastConfig == null || !_lastConfig.Equals(config) || protocol.ResetConnection)
        {
            _connection = new(config.Ip, config.Port);

            var res = _connection.ConnectServer();

            if (!res.IsSuccess)
            {
                if (protocol.IsLogPoints)
                    _logger.LogError($"{ProtocolType}连接失败: {res.Message}");
                throw new IOException($"{ProtocolType}连接失败: {res.Message}");
            }
            _lastConfig = config;
        }
        _connection.ReceiveTimeOut = int.Parse(protocol.ReceiveTimeOut);
        _connection.ConnectTimeOut = int.Parse(protocol.ConnectTimeOut);
    }
}
