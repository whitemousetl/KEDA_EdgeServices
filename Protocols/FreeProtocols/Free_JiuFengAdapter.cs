using HslCommunication;
using HslCommunication.Profinet.Freedom;
using KEDA_EdgeServices.Configs;
using KEDA_EdgeServices.Enums;
using KEDA_EdgeServices.Helpers;
using KEDA_EdgeServices.Models;
using KEDA_EdgeServices.Protocols.Attributes;
using KEDA_EdgeServices.Protocols.Base;
using KEDA_EdgeServices.Protocols.Interfaces;
using System.Net.Sockets;
using System.Text;

namespace KEDA_EdgeServices.Protocols.FreeProtocols;

[ProtocolType("Free_JiuFeng")]
public class Free_JiuFengAdapter : IProtocolAdapter
{
    private FreedomTcpNet _connection = default!;
    private Global _global;
    private ILogger _logger;
    private string _protocolType = "Free_JiuFeng";
    public Free_JiuFengAdapter(ILogger<Free_JiuFengAdapter> logger, Global global)
    {
        _logger = logger;
        _global = global;
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

    private bool EvaluatePointData(DeviceDataResult deviceResult, Point point, Serilog.ILogger logger, bool isLogPoints)
    {
        var readPointResult = new ReadPointResult { Label = point.Label };
        try
        {

            var dataType = Enum.Parse<DataType>(point.DataType);

            var res = TryReadData(point);

            if (res == null)
            {
                if (isLogPoints)
                    logger.Error($"EvaluatePointData读出来的res为null,参数名{point.Label}");
                return false;
            }

            //if (res is OperateResult<bool> boolRes)
            //{
            //    readPointResult.ReadIsSuccess = boolRes.IsSuccess;
            //    if (boolRes.IsSuccess)
            //    {
            //        readPointResult.ReadValue = boolRes.Content;
            //        if (isLogPoints)
            //            logger.Information(messageTemplate: $"{_protocolType}执行读取成功,参数名{point.Label},读取值{boolRes.Content},信息{boolRes.Message}");
            //    }
            //    else
            //    {
            //        if (isLogPoints)
            //            logger.Information(messageTemplate: $"{_protocolType}执行读取失败,参数名{point.Label},读取值{boolRes.Content},信息{boolRes.Message}");

            //        return false;
            //    }

            //}
            //else if (res is OperateResult<short> shortRes)
            //{
            //    var re = HandleNumericResult(shortRes, point, readPointResult, logger, isLogPoints);
            //    if (!re)
            //        return false;
            //}

            //else if (res is OperateResult<ushort> ushortRes)
            //{
            //    var re = HandleNumericResult(ushortRes, point, readPointResult, logger, isLogPoints);
            //    if (!re)
            //        return false;
            //}
            //else if (res is OperateResult<int> intRes)
            //{
            //    var re = HandleNumericResult(intRes, point, readPointResult, logger, isLogPoints);
            //    if (!re)
            //        return false;
            //}

            //else if (res is OperateResult<uint> uintRes)
            //{
            //    var re = HandleNumericResult(uintRes, point, readPointResult, logger, isLogPoints);
            //    if (!re)
            //        return false;
            //}

            //else if (res is OperateResult<float> floatRes)
            //{
            //    var re = HandleNumericResult(floatRes, point, readPointResult, logger, isLogPoints);
            //    if (!re)
            //        return false;
            //}

            //else if (res is OperateResult<double> doubleRes)
            //{
            //    var re = HandleNumericResult(doubleRes, point, readPointResult, logger, isLogPoints);
            //    if (!re)
            //        return false;
            //}

            //else if (res is OperateResult<string> strRes)
            //{
            //    readPointResult.ReadIsSuccess = strRes.IsSuccess;
            //    if (strRes.IsSuccess)
            //    {
            //        readPointResult.ReadValue = strRes.Content;
            //        if (isLogPoints)
            //            logger.Information(messageTemplate: $"{_protocolType}执行读取成功,参数名{point.Label},读取值{strRes.Content},信息{strRes.Message}");
            //    }
            //    else
            //    {
            //        if (isLogPoints)
            //            logger.Error(messageTemplate: $"{_protocolType}执行读取失败,参数名{point.Label},读取值{strRes.Content},信息{strRes.Message}");
            //        return false;
            //    }

            //}
            //else
            //{
            //    readPointResult.ReadIsSuccess = false;
            //    if (isLogPoints)
            //        logger.Error(messageTemplate: $"{_protocolType}执行读取失败,参数名{point.Label},无法解析的数据类型");
            //    return false;
            //}
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
        throw new NotImplementedException();
    }

    protected OperateResult<object> TryReadData(Point point)
    {
        var res = _connection.Read(point.Address, 1024);

        if (!ValidateResponse(res.Content))
            return OperateResult.CreateFailedResult<object>(new OperateResult() { IsSuccess = false, Message = "校验失败" });

        var result = ParseResponse(res.Content);

        return OperateResult.CreateSuccessResult(result);
    }

    public bool ValidateResponse(byte[] response)
    {
        // 校验和起始位确认
        if (response.Length != 41)
            return false;

        if (response[0] != 0xA6 || response[1] != 0x30 || response[2] != 0x31)
            return false;

        // 校验 CRC 和回车符
        string carriageReturn = response[40].ToString("X2");
        if (carriageReturn != "0D")
            return false;

        return true;
    }

    public object ParseResponse(byte[] response)
    {
        // 提取有用数据
        string totalVolume = Encoding.ASCII.GetString(response, 6, 8);

        return totalVolume;
    }
}
