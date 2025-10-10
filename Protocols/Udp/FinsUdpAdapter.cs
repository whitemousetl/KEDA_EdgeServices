using HslCommunication.Profinet.Omron;
using KEDA_EdgeServices.Configs;
using KEDA_EdgeServices.Models;
using KEDA_EdgeServices.Protocols.Attributes;
using KEDA_EdgeServices.Protocols.Base;

namespace KEDA_EdgeServices.Protocols.Udp;

[ProtocolType("FinsUdp")]
public class FinsUdpAdapter : ProtocolAdapterBase<OmronFinsUdp>
{
    public FinsUdpAdapter(ILogger<FinsUdpAdapter> logger, Global global) : base(logger, global)
    {
    }

    protected override string ProtocolType => "FinsUdp";


    private LanConfig? _lastConfig;
    protected override void InitOrReset(Protocol protocol)
    {
        //// 构造当前参数
        //var config = new LanConfig
        //{
        //    Ip = protocol.IPAddress,
        //    Port = int.Parse(protocol.ProtocolPort),
        //    ConnectTimeOut = int.Parse(protocol.ConnectTimeOut),
        //    ReceiveTimeOut = int.Parse(protocol.ReceiveTimeOut),
        //};

        //if (_connection == null || _lastConfig == null || !_lastConfig.Equals(config) || protocol.ResetConnection)
        //{
        //    _connection = new OmronFinsUdp(config.Ip, config.Port)
        //    {
        //        ReceiveTimeOut = config.ReceiveTimeOut,
        //    };

        //    var res = _connection.ConnectServer();

        //    if (!res.IsSuccess)
        //    {
        //        if (protocol.IsLogPoints)
        //            _logger.LogError($"{ProtocolType}连接失败: {res.Message}");
        //        throw new IOException($"{ProtocolType}连接失败: {res.Message}");
        //    }
        //    _lastConfig = config;
        //}
    }
}
