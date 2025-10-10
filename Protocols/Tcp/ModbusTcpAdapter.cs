using HslCommunication.Core;
using HslCommunication.ModBus;
using KEDA_EdgeServices.Configs;
using KEDA_EdgeServices.Models;
using KEDA_EdgeServices.Protocols.Attributes;
using KEDA_EdgeServices.Protocols.Base;

namespace KEDA_EdgeServices.Protocols.Tcp;

[ProtocolType("ModbusTcp")]
[ProtocolType("Modbus")]
public class ModbusTcpAdapter : ModbusProtocolAdapterBase<ModbusTcpNet>
{
    protected override string ProtocolType => "ModbusTcp";
    public ModbusTcpAdapter(ILogger<ModbusTcpAdapter> logger, Global global) : base(logger, global)
    {
    }

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
                    _logger.LogError($"ModbusTcp连接失败: {res.Message}");
                throw new IOException($"ModbusTcp连接失败: {res.Message}");
            }
            _lastConfig = config;
        }

        _connection.AddressStartWithZero = bool.Parse(protocol.AddressStartWithZero);
        _connection.ReceiveTimeOut = int.Parse(protocol.ReceiveTimeOut);
        _connection.ConnectTimeOut = int.Parse(protocol.ConnectTimeOut);
        _connection.DataFormat = Enum.Parse<DataFormat>(protocol.Format);
    }
}
