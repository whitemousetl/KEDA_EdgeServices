using HslCommunication;
using HslCommunication.Core;
using HslCommunication.ModBus;
using KEDA_EdgeServices.Configs;
using KEDA_EdgeServices.Enums;
using KEDA_EdgeServices.Helpers;
using KEDA_EdgeServices.Models;
using KEDA_EdgeServices.Protocols.Attributes;
using KEDA_EdgeServices.Protocols.Base;
using KEDA_EdgeServices.Protocols.Interfaces;
using System.IO.Ports;

namespace KEDA_EdgeServices.Protocols.Serial;

[ProtocolType("ModbusRtu")]
public class ModbusRtuAdapter : ModbusProtocolAdapterBase<ModbusRtu>
{
    protected override string ProtocolType => "ModbusRtu";
    public ModbusRtuAdapter(ILogger<ModbusRtuAdapter> logger, Global global) : base(logger, global)
    {
    }

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
                    _logger.LogError($"ModbusRtu串口打开失败: {openResult.Message}");
                throw new IOException($"ModbusRtu串口打开失败: {openResult.Message}");
            }
            _lastConfig = config;
        }

        _connection.ReceiveTimeOut = int.Parse(protocol.ReceiveTimeOut);
        _connection.AddressStartWithZero = bool.Parse(protocol.AddressStartWithZero);
        _connection.DataFormat = Enum.Parse<DataFormat>(protocol.Format);
    }
}

//public class ModbusRtuAdapter : IProtocolAdapter
//{
//    private ModbusRtu _connection = null!;
//    private SerialPortConfig _lastConfig = null!;
//    private ILogger<ModbusRtuAdapter> _logger;
//    private Global _global;
//    public ModbusRtuAdapter(ILogger<ModbusRtuAdapter> logger, Global global)
//    {
//        _logger = logger;
//        _global = global;
//    }

//    public async Task<List<DeviceDataResult>> ReadOrWriteAsync(Protocol protocol, CancellationToken ct)
//    {
//        InitOrReset(protocol);

//        if (_connection == null)
//        {
//            if (protocol.IsLogPoints)
//                _logger.LogError($"初始化或重置ModbusRtu连接失败，请检查。串口：{_lastConfig.PortName}");
//            return [];
//        }

//        var result = new List<DeviceDataResult>();

//        var writeTask = protocol.Devices.Select(d => new Device
//        {
//            EquipmentID = d.EquipmentID,
//            StationNo = d.StationNo,
//            Points = d.Points.Where(p => !string.IsNullOrWhiteSpace(p.WriteValue)).ToList(),
//        })
//        .Where(d => d.Points.Count > 0)
//        .ToList();

//        foreach (var device in writeTask)
//        {
//            var res = await WriteDevicePoints(device, protocol.PortName);
//            if (res != null) result.Add(res);
//        }

//        foreach (var device in protocol.Devices)
//        {
//            _connection.Station = byte.Parse(device.StationNo);

//            var deviceReadResult = result.FirstOrDefault(d => d.DeviceId == device.EquipmentID);

//            if (deviceReadResult == null)
//            {
//                deviceReadResult = new DeviceDataResult
//                {
//                    DeviceId = device.EquipmentID,
//                    ReadPointResults = [],
//                    WritePointResults = []
//                };
//            }

//            bool skipDevice = false;
//            foreach (var point in device.Points)
//            {
//                ct.ThrowIfCancellationRequested();
//                var logger = DeviceLoggerProvider.GetLogger(device.EquipmentID, protocol.PortName);

//                bool success = false;
//                for (int attempt = 1; attempt <= 2; attempt++)
//                {
//                    success = EvaluatePointData(deviceReadResult, point, logger, protocol.IsLogPoints);
//                    if (success) break;
//                    if (protocol.IsLogPoints)
//                        logger.Warning($"设备[{device.EquipmentID}]点[{point.Label}]第{attempt}次读取失败。");
//                }

//                if (!success)
//                {
//                    logger.Error($"设备[{device.EquipmentID}]点[{point.Label}]连续两次读取失败，跳过该设备后续点读取。");
//                    skipDevice = true;
//                    break;
//                }
//            }

//            if (!skipDevice && deviceReadResult != null)
//                result.Add(deviceReadResult);

//            await Task.Delay(_global.ReadDelay, ct);
//        }
//        return result;
//    }

//    private void InitOrReset(Protocol protocol)
//    {
//        // 构造当前参数
//        var config = new SerialPortConfig
//        {
//            PortName = protocol!.PortName,
//            BaudRate = int.Parse(protocol.BaudRate),
//            DataBits = int.Parse(protocol.DataBits),
//            StopBits = Enum.Parse<StopBits>(protocol.StopBits),
//            Parity = Enum.Parse<Parity>(protocol.Parity)
//        };

//        if (_connection == null || _lastConfig == null || !_lastConfig.Equals(config) || protocol.ResetConnection)
//        {
//            _connection?.Close();
//            _connection = new ModbusRtu();
//            _connection.SerialPortInni(config.PortName, config.BaudRate, config.DataBits, config.StopBits, config.Parity);
//            _connection.ReceiveTimeOut = int.Parse(protocol.ReceiveTimeOut);
//            _connection.AddressStartWithZero = bool.Parse(protocol.AddressStartWithZero);
//            var openResult = _connection.Open();
//            if (!openResult.IsSuccess)
//            {
//                if (protocol.IsLogPoints)
//                    _logger.LogError($"串口打开失败: {openResult.Message}");
//                throw new IOException($"串口打开失败: {openResult.Message}");
//            }
//            _lastConfig = config;
//        }
//    }

//    private bool EvaluatePointData(DeviceDataResult deviceResult, Point point, Serilog.ILogger logger, bool isLogPoints)
//    {
//        var readPointResult = new ReadPointResult { Label = point.Label };
//        try
//        {

//            var dataType = Enum.Parse<DataType>(point.DataType);

//            object? res = dataType switch
//            {
//                DataType.Bool => _connection.ReadBool(point.Address),
//                DataType.Short => _connection.ReadInt16(point.Address),
//                DataType.UShort => _connection.ReadUInt16(point.Address),
//                DataType.Int => _connection.ReadInt32(point.Address),
//                DataType.UInt => _connection.ReadUInt32(point.Address),
//                DataType.Float => _connection.ReadFloat(point.Address),
//                DataType.Double => _connection.ReadDouble(point.Address),
//                DataType.String => _connection.ReadString(point.Address, ushort.Parse(point.Length)),
//                _ => null
//            };

//            if (res == null)
//            {
//                if (isLogPoints)
//                    logger.Error($"EvaluatePointData读出来的res为null,参数名{point.Label}");
//                return false;
//            }

//            if (res is OperateResult<bool> boolRes)
//            {
//                readPointResult.ReadIsSuccess = boolRes.IsSuccess;
//                if (boolRes.IsSuccess)
//                {
//                    readPointResult.ReadValue = boolRes.Content;
//                    if (isLogPoints)
//                        logger.Information(messageTemplate: $"ModbusRtu执行读取成功,参数名{point.Label},读取值{boolRes.Content},信息{boolRes.Message}");
//                }
//                else
//                {
//                    if (isLogPoints)
//                        logger.Information(messageTemplate: $"ModbusRtu执行读取失败,参数名{point.Label},读取值{boolRes.Content},信息{boolRes.Message}");

//                    return false;
//                }

//            }
//            else if (res is OperateResult<short> shortRes)
//            {
//                var re = HandleNumericResult(shortRes, point, readPointResult, logger, isLogPoints);
//                if (!re)
//                    return false;
//            }

//            else if (res is OperateResult<ushort> ushortRes)
//            {
//                var re = HandleNumericResult(ushortRes, point, readPointResult, logger, isLogPoints);
//                if (!re)
//                    return false;
//            }
//            else if (res is OperateResult<int> intRes)
//            {
//                var re = HandleNumericResult(intRes, point, readPointResult, logger, isLogPoints);
//                if (!re)
//                    return false;
//            }

//            else if (res is OperateResult<uint> uintRes)
//            {
//                var re = HandleNumericResult(uintRes, point, readPointResult, logger, isLogPoints);
//                if (!re)
//                    return false;
//            }

//            else if (res is OperateResult<float> floatRes)
//            {
//                var re = HandleNumericResult(floatRes, point, readPointResult, logger, isLogPoints);
//                if (!re)
//                    return false;
//            }

//            else if (res is OperateResult<double> doubleRes)
//            {
//                var re = HandleNumericResult(doubleRes, point, readPointResult, logger, isLogPoints);
//                if (!re)
//                    return false;
//            }

//            else if (res is OperateResult<string> strRes)
//            {
//                readPointResult.ReadIsSuccess = strRes.IsSuccess;
//                if (strRes.IsSuccess)
//                {
//                    readPointResult.ReadValue = strRes.Content;
//                    if (isLogPoints)
//                        logger.Information(messageTemplate: $"ModbusRtu执行读取成功,参数名{point.Label},读取值{strRes.Content},信息{strRes.Message}");
//                }
//                else
//                {
//                    if (isLogPoints)
//                        logger.Error(messageTemplate: $"ModbusRtu执行读取失败,参数名{point.Label},读取值{strRes.Content},信息{strRes.Message}");
//                    return false;
//                }

//            }
//            else
//            {
//                readPointResult.ReadIsSuccess = false;
//                if (isLogPoints)
//                    logger.Error(messageTemplate: $"ModbusRtu执行读取失败,参数名{point.Label},无法解析的数据类型");
//                return false;
//            }
//        }
//        catch (Exception ex)
//        {
//            readPointResult.ReadIsSuccess = false;
//            if (isLogPoints)
//                logger.Error(messageTemplate: $"ModbusRtu执行读取失败,参数名{point.Label},异常信息{ex}");
//            return false;
//        }

//        deviceResult.ReadPointResults.Add(readPointResult);
//        return true;
//    }

//    private static bool HandleNumericResult<T>(OperateResult<T> result, Point point, ReadPointResult readPointResult, Serilog.ILogger logger, bool isLogPoints)
//        where T : struct, IConvertible
//    {
//        readPointResult.ReadIsSuccess = result.IsSuccess;
//        if (result.IsSuccess)
//        {
//            readPointResult.ReadValue = result.Content;
//            if (!string.IsNullOrWhiteSpace(point.Change) && ExpressionHelper.IsNumericType(readPointResult.ReadValue))
//            {
//                double x = Convert.ToDouble(readPointResult.ReadValue);
//                double y = ExpressionHelper.Eval(point.Change, x);
//                readPointResult.ReadValue = Math.Round(y, 2);
//            }
//            if (isLogPoints)
//                logger.Information(messageTemplate: $"ModbusRtu执行读取成功,参数名{point.Label},读取值{result.Content},实际值{readPointResult.ReadValue}，信息{result.Message}");
//            return true;
//        }
//        else
//        {
//            if (isLogPoints)
//                logger.Error(messageTemplate: $"ModbusRtu执行读取失败,参数名{point.Label},信息{result.Message}");
//            return false;
//        }
//    }

//    private async Task<DeviceDataResult?> WriteDevicePoints(Device device, string remark)
//    {
//        await Task.Delay(_global.WriteDelay);

//        var logger = DeviceLoggerProvider.GetLogger(device.EquipmentID, remark);

//        if (_connection == null)
//        {
//            logger.Error("连接为空，请检查");
//            return null;
//        }

//        var deviceResult = new DeviceDataResult
//        {
//            DeviceId = device.EquipmentID,
//            ReadPointResults = [],
//            WritePointResults = []
//        };

//        if (!byte.TryParse(device.StationNo, out byte stationNo))
//        {
//            logger.Error("ModbusRtu的站号无法转换成byte类类型，请检查");
//            return null;
//        }

//        _connection.Station = stationNo;

//        foreach (var point in device.Points)
//        {
//            var writePointResult = new WritePointResult
//            {
//                Label = point.Label,
//                OriginalValue = point.WriteValue
//            };

//            deviceResult.WritePointResults.Add(writePointResult);

//            if (string.IsNullOrWhiteSpace(point.WriteValue))
//            {
//                logger.Error($"ModbusRtu的写入值为空，请检查, 参数名{point.Label}");
//                continue; // 跳过读点
//            }

//            if (!Enum.TryParse(point.DataType, out DataType dataType))
//            {
//                logger.Error($"ModbusRtu的数据类型异常，请假查,参数名{point.Label},写入值{point.WriteValue}");
//                continue;
//            }

//            try
//            {
//                HandleWriteOperation(point, writePointResult, dataType, logger);
//            }
//            catch (Exception ex)
//            {
//                logger.Error($"ModbusRtu执行写操作发生异常,参数名{point.Label},写入值{point.WriteValue},异常信息{ex}");
//            }
//        }

//        return deviceResult;
//    }

//    private void HandleWriteOperation(Point point, WritePointResult writePointResult, DataType dataType, Serilog.ILogger logger)
//    {
//        try
//        {
//            switch (dataType)
//            {
//                case DataType.Bool:
//                    if (bool.TryParse(point.WriteValue, out bool writeBool))
//                    {
//                        var res = _connection.Write(point.Address, writeBool);
//                        writePointResult.WriteIsSuccess = res.IsSuccess;
//                        if (res.IsSuccess)
//                        {
//                            var readRes = _connection.ReadBool(point.Address);
//                            writePointResult.WriteResult = readRes.IsSuccess ? readRes.Content.ToString() : null;
//                            logger.Information($"ModbusRtu执行写入成功,参数名{point.Label},写入值{point.WriteValue},实际值{readRes.Content}");
//                        }
//                    }
//                    break;
//                case DataType.Short:
//                    {
//                        double writeVal;
//                        if (!string.IsNullOrWhiteSpace(point.Change))
//                        {
//                            double y = Convert.ToDouble(point.WriteValue);
//                            writeVal = ExpressionHelper.InverseEval(point.Change, y);
//                        }
//                        else
//                            writeVal = Convert.ToDouble(point.WriteValue);
//                        short writeShort = (short)Math.Round(writeVal);
//                        var res = _connection.Write(point.Address, writeShort);
//                        writePointResult.WriteIsSuccess = res.IsSuccess;
//                        if (res.IsSuccess)
//                        {
//                            var readRes = _connection.ReadInt16(point.Address);
//                            writePointResult.WriteResult = readRes.IsSuccess ? readRes.Content.ToString() : null;
//                            logger.Information($"ModbusRtu执行写入成功,参数名{point.Label},写入值{point.WriteValue},实际值{readRes.Content}");
//                        }
//                    }
//                    break;
//                case DataType.UShort:
//                    {
//                        double writeVal;
//                        if (!string.IsNullOrWhiteSpace(point.Change))
//                        {
//                            double y = Convert.ToDouble(point.WriteValue);
//                            writeVal = ExpressionHelper.InverseEval(point.Change, y);
//                        }
//                        else
//                            writeVal = Convert.ToDouble(point.WriteValue);
//                        ushort writeUShort = (ushort)Math.Round(writeVal);
//                        var res = _connection.Write(point.Address, writeUShort);
//                        writePointResult.WriteIsSuccess = res.IsSuccess;
//                        if (res.IsSuccess)
//                        {
//                            var readRes = _connection.ReadUInt16(point.Address);
//                            writePointResult.WriteResult = readRes.IsSuccess ? readRes.Content.ToString() : null;
//                            logger.Information($"ModbusRtu执行写入成功,参数名{point.Label},写入值{point.WriteValue},实际值{readRes.Content}");
//                        }
//                    }
//                    break;
//                case DataType.Int:
//                    {
//                        double writeVal;
//                        if (!string.IsNullOrWhiteSpace(point.Change))
//                        {
//                            double y = Convert.ToDouble(point.WriteValue);
//                            writeVal = ExpressionHelper.InverseEval(point.Change, y);
//                        }
//                        else
//                            writeVal = Convert.ToDouble(point.WriteValue);
//                        int writeInt = (int)Math.Round(writeVal);
//                        var res = _connection.Write(point.Address, writeInt);
//                        writePointResult.WriteIsSuccess = res.IsSuccess;
//                        if (res.IsSuccess)
//                        {
//                            var readRes = _connection.ReadInt32(point.Address);
//                            writePointResult.WriteResult = readRes.IsSuccess ? readRes.Content.ToString() : null;
//                            logger.Information($"ModbusRtu执行写入成功,参数名{point.Label},写入值{point.WriteValue},实际值{readRes.Content}");
//                        }
//                    }
//                    break;
//                case DataType.UInt:
//                    {
//                        double writeVal;
//                        if (!string.IsNullOrWhiteSpace(point.Change))
//                        {
//                            double y = Convert.ToDouble(point.WriteValue);
//                            writeVal = ExpressionHelper.InverseEval(point.Change, y);
//                        }
//                        else
//                            writeVal = Convert.ToDouble(point.WriteValue);
//                        uint writeUint = (uint)Math.Round(writeVal);
//                        var res = _connection.Write(point.Address, writeUint);
//                        writePointResult.WriteIsSuccess = res.IsSuccess;
//                        if (res.IsSuccess)
//                        {
//                            var readRes = _connection.ReadUInt32(point.Address);
//                            writePointResult.WriteResult = readRes.IsSuccess ? readRes.Content.ToString() : null;
//                            logger.Information($"ModbusRtu执行写入成功,参数名{point.Label},写入值{point.WriteValue},实际值{readRes.Content}");
//                        }
//                    }
//                    break;
//                case DataType.Float:
//                    {
//                        double writeVal;
//                        if (!string.IsNullOrWhiteSpace(point.Change))
//                        {
//                            double y = Convert.ToDouble(point.WriteValue);
//                            writeVal = ExpressionHelper.InverseEval(point.Change, y);
//                        }
//                        else
//                            writeVal = Convert.ToDouble(point.WriteValue);
//                        float writeFloat = (float)Math.Round(writeVal, 2);
//                        var res = _connection.Write(point.Address, writeFloat);
//                        writePointResult.WriteIsSuccess = res.IsSuccess;
//                        if (res.IsSuccess)
//                        {
//                            var readRes = _connection.ReadFloat(point.Address);
//                            writePointResult.WriteResult = readRes.IsSuccess ? readRes.Content.ToString() : null;
//                            logger.Information($"ModbusRtu执行写入成功,参数名{point.Label},写入值{point.WriteValue},实际值{readRes.Content}");
//                        }
//                    }
//                    break;
//                case DataType.Double:
//                    {
//                        double writeVal;
//                        if (!string.IsNullOrWhiteSpace(point.Change))
//                        {
//                            double y = Convert.ToDouble(point.WriteValue);
//                            writeVal = ExpressionHelper.InverseEval(point.Change, y);
//                        }
//                        else
//                            writeVal = Convert.ToDouble(point.WriteValue);
//                        double writeDouble = Math.Round(writeVal, 2);
//                        var res = _connection.Write(point.Address, writeDouble);
//                        writePointResult.WriteIsSuccess = res.IsSuccess;
//                        if (res.IsSuccess)
//                        {
//                            var readRes = _connection.ReadDouble(point.Address);
//                            writePointResult.WriteResult = readRes.IsSuccess ? readRes.Content.ToString() : null;
//                            logger.Information($"ModbusRtu执行写入成功,参数名{point.Label},写入值{point.WriteValue},实际值{readRes.Content}");
//                        }
//                    }
//                    break;
//                case DataType.String:
//                    var resStr = _connection.Write(point.Address, point.WriteValue, ushort.Parse(point.Length));
//                    writePointResult.WriteIsSuccess = resStr.IsSuccess;
//                    if (resStr.IsSuccess)
//                    {
//                        var readRes = _connection.ReadString(point.Address, ushort.Parse(point.Length));
//                        writePointResult.WriteResult = readRes.IsSuccess ? readRes.Content : null;
//                        logger.Information($"ModbusRtu执行写入成功,参数名{point.Label},写入值{point.WriteValue},实际值{readRes.Content}");
//                    }
//                    break;
//                default:
//                    logger.Error($"ModbusRtu执行写入失败，数据类型不存在,参数名{point.Label},写入值{point.WriteValue}");
//                    break;
//            }
//        }
//        catch (Exception ex)
//        {
//            logger.Error($"ModbusRtu执行写入异常,参数名{point.Label},写入值{point.WriteValue},异常信息{ex}");
//            throw;
//        }
//    }
//}


