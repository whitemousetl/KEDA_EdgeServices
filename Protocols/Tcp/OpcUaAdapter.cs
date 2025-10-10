using HslCommunication;
using KEDA_EdgeServices.Configs;
using KEDA_EdgeServices.Enums;
using KEDA_EdgeServices.Helpers;
using KEDA_EdgeServices.Models;
using KEDA_EdgeServices.Protocols.Attributes;
using KEDA_EdgeServices.Protocols.Base;
using KEDA_EdgeServices.Protocols.Interfaces;
using OpcUaHelper;
using System.Data.Common;
using System.Net.Sockets;

namespace KEDA_EdgeServices.Protocols.Tcp;

[ProtocolType("OpcUa")]
public class OpcUaAdapter : IProtocolAdapter
{
    private OpcUaClient _connection = default!;
    private Global _global;
    private ILogger _logger;
    private string _protocolType = "OpcUa";
    public OpcUaAdapter(ILogger<OpcUaAdapter> logger, Global global)
    {
        _logger = logger;
        _global = global;
    }

    public async Task<List<DeviceDataResult>> ReadOrWriteAsync(Protocol protocol, CancellationToken ct)
    {
        await InitOrReset(protocol);

        if (_connection == null)
        {
            if (protocol.IsLogPoints)
                _logger.LogError($"初始化或重置{_protocolType}连接失败，请检查。");
            return [];
        }

        var result = new List<DeviceDataResult>();

        var writeTask = protocol.Devices.Select(d => new Device
        {
            EquipmentID = d.EquipmentID,
            StationNo = d.StationNo,
            Points = d.Points.Where(p => !string.IsNullOrWhiteSpace(p.WriteValue)).ToList(),
        })
        .Where(d => d.Points.Count > 0)
        .ToList();

        foreach (var device in writeTask)
        {
            var res = await WriteDevicePoints(device, protocol.PortName);
            if (res != null) result.Add(res);
        }

        foreach (var device in protocol.Devices)
        {
            var deviceReadResult = result.FirstOrDefault(d => d.DeviceId == device.EquipmentID);

            if (deviceReadResult == null)
            {
                deviceReadResult = new DeviceDataResult
                {
                    DeviceId = device.EquipmentID,
                    ReadPointResults = [],
                    WritePointResults = []
                };
            }

            bool skipDevice = false;
            foreach (var point in device.Points)
            {
                ct.ThrowIfCancellationRequested();
                var logger = DeviceLoggerProvider.GetLogger(device.EquipmentID, protocol.PortName);

                bool success = false;
                for (int attempt = 1; attempt <= 2; attempt++)
                {
                    success = EvaluatePointData(deviceReadResult, point, logger, protocol.IsLogPoints);
                    if (success) break;
                    if (protocol.IsLogPoints)
                        logger.Warning($"设备[{device.EquipmentID}]点[{point.Label}]第{attempt}次读取失败。");
                }

                if (!success)
                {
                    logger.Error($"设备[{device.EquipmentID}]点[{point.Label}]连续两次读取失败，跳过该设备后续点读取。");
                    skipDevice = true;
                    break;
                }
            }

            if (!skipDevice && deviceReadResult != null)
                result.Add(deviceReadResult);

            await Task.Delay(_global.ReadDelay, ct);
        }
        return result;
    }

    private LanConfig? _lastConfig;
    private async Task InitOrReset(Protocol protocol)
    {
        // 构造当前参数
        var config = new LanConfig
        {
            Ip = protocol.IPAddress,
            Port = int.Parse(protocol.ProtocolPort),
        };

        if (_connection == null || _lastConfig == null || !_lastConfig.Equals(config) || protocol.ResetConnection)
        {
            _connection = new();

            await _connection.ConnectServer($"opc.tcp://{config.Ip}:{config.Port}");

            _lastConfig = config;
        }
    }

    private bool EvaluatePointData(DeviceDataResult deviceResult, Point point, Serilog.ILogger logger, bool isLogPoints)
    {
        var readPointResult = new ReadPointResult { Label = point.Label };
        try
        {
            var dataType = Enum.Parse<DataType>(point.DataType);

            object? res = dataType switch
            {
                DataType.Bool => _connection.ReadNode<bool>(point.Address),
                DataType.Short => _connection.ReadNode<short>(point.Address),
                DataType.UShort => _connection.ReadNode<ushort>(point.Address),
                DataType.Int => _connection.ReadNode<int>(point.Address),
                DataType.UInt => _connection.ReadNode<uint>(point.Address),
                DataType.Float => _connection.ReadNode<float>(point.Address),
                DataType.Double => _connection.ReadNode<double>(point.Address),
                DataType.String => _connection.ReadNode<string>(point.Address),
                _ => null
            };

            if (res == null)
            {
                if (isLogPoints)
                    logger.Error($"EvaluatePointData读出来的res为null,参数名{point.Label}");
                return false;
            }

            if (res is OperateResult<bool> boolRes)
            {
                readPointResult.ReadIsSuccess = boolRes.IsSuccess;
                if (boolRes.IsSuccess)
                {
                    readPointResult.ReadValue = boolRes.Content;
                    if (isLogPoints)
                        logger.Information(messageTemplate: $"{_protocolType}执行读取成功,参数名{point.Label},读取值{boolRes.Content},信息{boolRes.Message}");
                }
                else
                {
                    if (isLogPoints)
                        logger.Information(messageTemplate: $"{_protocolType}执行读取失败,参数名{point.Label},读取值{boolRes.Content},信息{boolRes.Message}");

                    return false;
                }

            }
            else if (res is OperateResult<short> shortRes)
            {
                var re = HandleNumericResult(shortRes, point, readPointResult, logger, isLogPoints);
                if (!re)
                    return false;
            }

            else if (res is OperateResult<ushort> ushortRes)
            {
                var re = HandleNumericResult(ushortRes, point, readPointResult, logger, isLogPoints);
                if (!re)
                    return false;
            }
            else if (res is OperateResult<int> intRes)
            {
                var re = HandleNumericResult(intRes, point, readPointResult, logger, isLogPoints);
                if (!re)
                    return false;
            }

            else if (res is OperateResult<uint> uintRes)
            {
                var re = HandleNumericResult(uintRes, point, readPointResult, logger, isLogPoints);
                if (!re)
                    return false;
            }

            else if (res is OperateResult<float> floatRes)
            {
                var re = HandleNumericResult(floatRes, point, readPointResult, logger, isLogPoints);
                if (!re)
                    return false;
            }

            else if (res is OperateResult<double> doubleRes)
            {
                var re = HandleNumericResult(doubleRes, point, readPointResult, logger, isLogPoints);
                if (!re)
                    return false;
            }

            else if (res is OperateResult<string> strRes)
            {
                readPointResult.ReadIsSuccess = strRes.IsSuccess;
                if (strRes.IsSuccess)
                {
                    readPointResult.ReadValue = strRes.Content;
                    if (isLogPoints)
                        logger.Information(messageTemplate: $"{_protocolType}执行读取成功,参数名{point.Label},读取值{strRes.Content},信息{strRes.Message}");
                }
                else
                {
                    if (isLogPoints)
                        logger.Error(messageTemplate: $"{_protocolType}执行读取失败,参数名{point.Label},读取值{strRes.Content},信息{strRes.Message}");
                    return false;
                }

            }
            else
            {
                readPointResult.ReadIsSuccess = false;
                if (isLogPoints)
                    logger.Error(messageTemplate: $"{_protocolType}执行读取失败,参数名{point.Label},无法解析的数据类型");
                return false;
            }
        }
        catch (Exception ex)
        {
            readPointResult.ReadIsSuccess = false;
            if (isLogPoints)
                logger.Error(messageTemplate: $"{_protocolType}执行读取失败,参数名{point.Label},异常信息{ex}");
            return false;
        }

        deviceResult.ReadPointResults.Add(readPointResult);
        return true;
    }

    private bool HandleNumericResult<T>(OperateResult<T> result, Point point, ReadPointResult readPointResult, Serilog.ILogger logger, bool isLogPoints)
        where T : struct, IConvertible
    {
        readPointResult.ReadIsSuccess = result.IsSuccess;
        if (result.IsSuccess)
        {
            readPointResult.ReadValue = result.Content;
            if (!string.IsNullOrWhiteSpace(point.Change) && ExpressionHelper.IsNumericType(readPointResult.ReadValue))
            {
                double x = Convert.ToDouble(readPointResult.ReadValue);
                double y = ExpressionHelper.Eval(point.Change, x);
                readPointResult.ReadValue = Math.Round(y, 2);
            }
            if (isLogPoints)
                logger.Information(messageTemplate: $"{_protocolType}执行读取成功,参数名{point.Label},读取值{result.Content},实际值{readPointResult.ReadValue}，信息{result.Message}");
            return true;
        }
        else
        {
            if (isLogPoints)
                logger.Error(messageTemplate: $"{_protocolType}执行读取失败,参数名{point.Label},信息{result.Message}");
            return false;
        }
    }

    private async Task<DeviceDataResult?> WriteDevicePoints(Device device, string remark)
    {
        await Task.Delay(_global.WriteDelay);

        var logger = DeviceLoggerProvider.GetLogger(device.EquipmentID, remark);

        if (_connection == null)
        {
            logger.Error("连接为空，请检查");
            return null;
        }

        var deviceResult = new DeviceDataResult
        {
            DeviceId = device.EquipmentID,
            ReadPointResults = [],
            WritePointResults = []
        };

        foreach (var point in device.Points)
        {
            var writePointResult = new WritePointResult
            {
                Label = point.Label,
                OriginalValue = point.WriteValue
            };

            deviceResult.WritePointResults.Add(writePointResult);

            if (string.IsNullOrWhiteSpace(point.WriteValue))
            {
                logger.Error($"{_protocolType}的写入值为空，请检查, 参数名{point.Label}");
                continue; // 跳过读点
            }

            if (!Enum.TryParse(point.DataType, out DataType dataType))
            {
                logger.Error($"{_protocolType}的数据类型异常，请假查,参数名{point.Label},写入值{point.WriteValue}");
                continue;
            }

            try
            {
                HandleWriteOperation(point, writePointResult, dataType, logger);
            }
            catch (Exception ex)
            {
                logger.Error($"{_protocolType}执行写操作发生异常,参数名{point.Label},写入值{point.WriteValue},异常信息{ex}");
            }
        }

        return deviceResult;
    }

    private void HandleWriteOperation(Point point, WritePointResult writePointResult, DataType dataType, Serilog.ILogger logger)
    {
        try
        {
            switch (dataType)
            {
                case DataType.Bool:
                    if (bool.TryParse(point.WriteValue, out bool writeBool))
                    {
                        var res = _connection.WriteNode(point.Address, writeBool);
                        writePointResult.WriteIsSuccess = res;
                        if (res)
                        {
                            var readRes = _connection.ReadNode<bool>(point.Address);
                            writePointResult.WriteResult = readRes.ToString();
                            logger.Information($"{_protocolType}执行写入成功,参数名{point.Label},写入值{point.WriteValue},实际值{readRes}");
                        }
                    }
                    break;
                case DataType.Short:
                    {
                        double writeVal;
                        if (!string.IsNullOrWhiteSpace(point.Change))
                        {
                            double y = Convert.ToDouble(point.WriteValue);
                            writeVal = ExpressionHelper.InverseEval(point.Change, y);
                        }
                        else
                            writeVal = Convert.ToDouble(point.WriteValue);
                        short writeShort = (short)Math.Round(writeVal);
                        var res = _connection.WriteNode(point.Address, writeShort);
                        writePointResult.WriteIsSuccess = res;
                        if (res)
                        {
                            var readRes = _connection.ReadNode<short>(point.Address);
                            writePointResult.WriteResult = readRes.ToString();
                            logger.Information($"{_protocolType}执行写入成功,参数名{point.Label},写入值{point.WriteValue},实际值{readRes}");
                        }
                    }
                    break;
                case DataType.UShort:
                    {
                        double writeVal;
                        if (!string.IsNullOrWhiteSpace(point.Change))
                        {
                            double y = Convert.ToDouble(point.WriteValue);
                            writeVal = ExpressionHelper.InverseEval(point.Change, y);
                        }
                        else
                            writeVal = Convert.ToDouble(point.WriteValue);
                        ushort writeUShort = (ushort)Math.Round(writeVal);
                        var res = _connection.WriteNode(point.Address, writeUShort);
                        writePointResult.WriteIsSuccess = res;
                        if (res)
                        {
                            var readRes = _connection.ReadNode<ushort>(point.Address);
                            writePointResult.WriteResult = readRes.ToString();
                            logger.Information($"{_protocolType}执行写入成功,参数名{point.Label},写入值{point.WriteValue},实际值{readRes}");
                        }
                    }
                    break;
                case DataType.Int:
                    {
                        double writeVal;
                        if (!string.IsNullOrWhiteSpace(point.Change))
                        {
                            double y = Convert.ToDouble(point.WriteValue);
                            writeVal = ExpressionHelper.InverseEval(point.Change, y);
                        }
                        else
                            writeVal = Convert.ToDouble(point.WriteValue);
                        int writeInt = (int)Math.Round(writeVal);
                        var res = _connection.WriteNode(point.Address, writeInt);
                        writePointResult.WriteIsSuccess = res;
                        if (res)
                        {
                            var readRes = _connection.ReadNode<int>(point.Address);
                            writePointResult.WriteResult = readRes.ToString();
                            logger.Information($"{_protocolType}执行写入成功,参数名{point.Label},写入值{point.WriteValue},实际值{readRes}");
                        }
                    }
                    break;
                case DataType.UInt:
                    {
                        double writeVal;
                        if (!string.IsNullOrWhiteSpace(point.Change))
                        {
                            double y = Convert.ToDouble(point.WriteValue);
                            writeVal = ExpressionHelper.InverseEval(point.Change, y);
                        }
                        else
                            writeVal = Convert.ToDouble(point.WriteValue);
                        uint writeUint = (uint)Math.Round(writeVal);
                        var res = _connection.WriteNode(point.Address, writeUint);
                        writePointResult.WriteIsSuccess = res;
                        if (res)
                        {
                            var readRes = _connection.ReadNode<uint>(point.Address);
                            writePointResult.WriteResult = readRes.ToString();
                            logger.Information($"{_protocolType}执行写入成功,参数名{point.Label},写入值{point.WriteValue},实际值{readRes}");
                        }
                    }
                    break;
                case DataType.Float:
                    {
                        double writeVal;
                        if (!string.IsNullOrWhiteSpace(point.Change))
                        {
                            double y = Convert.ToDouble(point.WriteValue);
                            writeVal = ExpressionHelper.InverseEval(point.Change, y);
                        }
                        else
                            writeVal = Convert.ToDouble(point.WriteValue);
                        float writeFloat = (float)Math.Round(writeVal, 2);
                        var res = _connection.WriteNode(point.Address, writeFloat);
                        writePointResult.WriteIsSuccess = res;
                        if (res)
                        {
                            var readRes = _connection.ReadNode<float>(point.Address);
                            writePointResult.WriteResult = readRes.ToString();
                            logger.Information($"{_protocolType}执行写入成功,参数名{point.Label},写入值{point.WriteValue},实际值{readRes}");
                        }
                    }
                    break;
                case DataType.Double:
                    {
                        double writeVal;
                        if (!string.IsNullOrWhiteSpace(point.Change))
                        {
                            double y = Convert.ToDouble(point.WriteValue);
                            writeVal = ExpressionHelper.InverseEval(point.Change, y);
                        }
                        else
                            writeVal = Convert.ToDouble(point.WriteValue);
                        double writeDouble = Math.Round(writeVal, 2);
                        var res = _connection.WriteNode(point.Address, writeDouble);
                        writePointResult.WriteIsSuccess = res;
                        if (res)
                        {
                            var readRes = _connection.ReadNode<double>(point.Address);
                            writePointResult.WriteResult = readRes.ToString();
                            logger.Information($"{_protocolType}执行写入成功,参数名{point.Label},写入值{point.WriteValue},实际值{readRes}");
                        }
                    }
                    break;
                case DataType.String:
                    var resStr = _connection.WriteNode(point.Address, point.WriteValue);
                    writePointResult.WriteIsSuccess = resStr;
                    if (resStr)
                    {
                        var readRes = _connection.ReadNode<string>(point.Address);
                        writePointResult.WriteResult = readRes;
                        logger.Information($"{_protocolType}执行写入成功,参数名{point.Label},写入值{point.WriteValue},实际值{readRes}");
                    }
                    break;
                default:
                    logger.Error($"{_protocolType}执行写入失败，数据类型不存在,参数名{point.Label},写入值{point.WriteValue}");
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.Error($"{_protocolType}执行写入异常,参数名{point.Label},写入值{point.WriteValue},异常信息{ex}");
            throw;
        }
    }
}
