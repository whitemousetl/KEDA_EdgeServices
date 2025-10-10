using HslCommunication.Profinet.Freedom;
using KEDA_EdgeServices.Configs;
using KEDA_EdgeServices.Helpers;
using KEDA_EdgeServices.Models;
using KEDA_EdgeServices.Protocols.Attributes;
using KEDA_EdgeServices.Protocols.FreeProtocols;
using KEDA_EdgeServices.Protocols.Interfaces;
using MySqlConnector;
using System.Drawing;

namespace KEDA_EdgeServices.Protocols.Sql;

[ProtocolType("MySql")]
public class MySqlAdapter : IProtocolAdapter
{
    private MySqlConnection _connection = new();
    private string _protocolType => "MySql";
    private readonly ILogger _logger;
    public MySqlAdapter(ILogger<MySqlAdapter> logger)
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

        foreach (var device in protocol.Devices)
        {
            var deviceReadResult = new DeviceDataResult
            {
                DeviceId = device.EquipmentID,
                ReadPointResults = [],
                WritePointResults = []
            };

            bool skipDevice = false;

            foreach (var point in device.Points)
            {
                ct.ThrowIfCancellationRequested();
                var readPointResult = new ReadPointResult { Label = point.Label };
                Serilog.ILogger logger = protocol.Interface == "LAN"
                    ? DeviceLoggerProvider.GetLogger(device.EquipmentID, protocol.IPAddress + ":" + protocol.ProtocolPort)
                    : DeviceLoggerProvider.GetLogger(device.EquipmentID, protocol.PortName);

                bool success = false;
                object? value = null;
                string? errorMsg = null;

                for (int attempt = 1; attempt <= 2; attempt++)
                {
                    try
                    {
                        using var cmd = new MySqlCommand(point.Address, _connection);
                        value = await cmd.ExecuteScalarAsync(ct);

                        if (value != null && value != DBNull.Value)
                        {
                            readPointResult.ReadIsSuccess = true;
                            readPointResult.ReadValue = value;
                            if (protocol.IsLogPoints)
                                logger.Information($"{_protocolType}读取成功, 设备[{device.EquipmentID}] 点[{point.Label}], 值: {value}");
                            success = true;
                            break;
                        }
                        else
                        {
                            readPointResult.ReadIsSuccess = false;
                            readPointResult.ReadValue = null;
                            errorMsg = "数据库返回空值";
                            if (protocol.IsLogPoints)
                                logger.Warning($"{_protocolType}读取失败, 设备[{device.EquipmentID}] 点[{point.Label}], 数据库返回空值");
                        }
                    }
                    catch (Exception ex)
                    {
                        readPointResult.ReadIsSuccess = false;
                        readPointResult.ReadValue = null;
                        errorMsg = ex.Message;
                        if (protocol.IsLogPoints)
                            logger.Error($"{_protocolType}读取异常, 设备[{device.EquipmentID}] 点[{point.Label}], 异常: {ex.Message}");
                    }
                }

                deviceReadResult.ReadPointResults.Add(readPointResult);

                if (!success)
                {
                    skipDevice = true;
                    break;
                }
            }

            if (!skipDevice)
                result.Add(deviceReadResult);
        }

        return result;
    }

    private string? _lastConnectionString;
    private void InitOrReset(Protocol protocol)
    {
        // 解析 Gateway 字段
        var gatewayParts = protocol.Gateway.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (gatewayParts.Length != 3)
            throw new InvalidOperationException("Gateway 字段格式错误，必须为 UserID,Password,Database");

        var userId = gatewayParts[0];
        var password = gatewayParts[1];
        var database = gatewayParts[2];

        // 组装连接字符串
        var connectionString = $"Server={protocol.IPAddress};Port={protocol.ProtocolPort};User ID={userId};Password={password};Database={database};Connection Timeout={protocol.ConnectTimeOut};";

        // 判断是否需要重连
        if (_connection == null || _lastConnectionString != connectionString || protocol.ResetConnection)
        {
            _connection?.Dispose();
            _connection = new MySqlConnection(connectionString);

            try
            {
                _connection.Open();
            }
            catch (Exception ex)
            {
                if (protocol.IsLogPoints)
                    _logger.LogError($"{_protocolType}连接失败: {ex.Message}");
                throw new IOException($"{_protocolType}连接失败: {ex.Message}");
            }
            _lastConnectionString = connectionString;
        }
    }
}
