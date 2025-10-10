using HslCommunication.Profinet.Siemens;
using KEDA_EdgeServices.Configs;
using KEDA_EdgeServices.Models;
using KEDA_EdgeServices.Protocols.Attributes;
using KEDA_EdgeServices.Protocols.Base;

namespace KEDA_EdgeServices.Protocols;

[ProtocolType("SiemensS200Smart")]
[ProtocolType("S200Smart")]
public class SiemensS200SmartAdapter : ProtocolAdapterBase<SiemensS7Net>
{
    public SiemensS200SmartAdapter(ILogger<SiemensS200SmartAdapter> logger, Global global) : base(logger, global)
    {
    }

    protected override string ProtocolType => "SiemensS71200";

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
            _connection = new(SiemensPLCS.S200Smart, config.Ip)
            {
                Port = config.Port
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

        _connection.ReceiveTimeOut = int.Parse(protocol.ReceiveTimeOut);
        _connection.ConnectTimeOut = int.Parse(protocol.ConnectTimeOut);
    }
}
