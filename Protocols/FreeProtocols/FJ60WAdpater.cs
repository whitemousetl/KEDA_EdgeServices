using HslCommunication.Profinet.Freedom;
using KEDA_EdgeServices.Configs;
using KEDA_EdgeServices.Models;
using KEDA_EdgeServices.Protocols.Attributes;
using KEDA_EdgeServices.Protocols.Interfaces;
using System.Text;

namespace KEDA_EdgeServices.Protocols.FreeProtocols;

[ProtocolType("FJ60W")]//激光机
public class FJ60WAdpater : IProtocolAdapter
{
    private FreedomTcpNet _connection = new();
    private string _protocolType => "FJ60W";
    private readonly ILogger _logger;
    public FJ60WAdpater(ILogger<FJ60WAdpater> logger)
    {
        _logger = logger;
    }

    public async Task<List<DeviceDataResult>> ReadOrWriteAsync(Protocol protocol, CancellationToken ct)
    {
        InitOrReset(protocol);

        if (_connection == null)
        {
            if (protocol.IsLogPoints)
                _logger.LogError($"初始化或重置{_protocolType}连接失败，请检查。");
            return [];
        }

        var result = new List<DeviceDataResult>();

        var points = protocol.Devices[0].Points;

        var msg = string.Empty;

        if (points.Count == 1)
            msg = points[0].Address;
        else
            msg = string.Join("\\,", points.Select(p => p.Address));

        msg = "SM" + msg + "\r\n";

        var data = Encoding.UTF8.GetBytes(msg);

        var res = await _connection.ReadFromCoreServerAsync(data);
        var devDataRes = new DeviceDataResult();

        if (res.IsSuccess)
            devDataRes.DeviceMsg = Encoding.UTF8.GetString(res.Content);
        else
        {
            devDataRes.DeviceMsg = res.Message;
            _logger.LogError(res.Message);
        }

        return result;
    }


    private LanConfig _lastConfig = null!;
    private void InitOrReset(Protocol protocol)
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
                    _logger.LogError($"{_protocolType}连接失败: {res.Message}");
                throw new IOException($"{_protocolType}连接失败: {res.Message}");
            }
            _lastConfig = config;
        }

        _connection.ReceiveTimeOut = int.Parse(protocol.ReceiveTimeOut);
        _connection.ConnectTimeOut = int.Parse(protocol.ConnectTimeOut);
    }
}
