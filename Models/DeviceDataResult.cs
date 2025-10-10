using KEDA_EdgeServices.Enums;

namespace KEDA_EdgeServices.Models;

public class DeviceDataResult
{
    /// <summary>
    /// 设备唯一标识
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// 读取该设备所有采集点所用的总时间（毫秒）
    /// </summary>
    public long ReadElapsedMilliseconds { get; set; }

    /// <summary>
    /// 读取该设备所有采集点所用的总时间（毫秒）
    /// </summary>
    public long WriteElapsedMilliseconds { get; set; }

    /// <summary>
    /// 设备读取状态，200，全部读取成功，300，全部读取失败，500，任意点读取失败
    /// </summary>
    public DeviceReadStatus? ReadDeviceStatus { get; set; }

    /// <summary>
    /// 采集到的点位数据及状态
    /// </summary>
    public List<ReadPointResult> ReadPointResults { get; set; } = [];

    /// <summary>
    /// 设备写入状态，2000，全部写入成功，3000，全部写入失败，5000，任意点写入失败
    /// </summary>
    public DeviceWriteStatus? WriteDeviceStatus { get; set; }

    /// <summary>
    /// 写的点位数据及状态
    /// </summary>
    public List<WritePointResult> WritePointResults { get; set; } = [];

    /// <summary>
    /// 设备返回信息
    /// </summary>
    public string DeviceMsg { get; set; } = string.Empty;
}

/// <summary>
/// 单个采集点的采集结果
/// </summary>
public class ReadPointResult
{
    /// <summary>
    /// 采集点参数名（如Label）
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 读取该采集点所用的时间（毫秒）
    /// </summary>
    public long ReadElapsedMilliseconds { get; set; }

    /// <summary>
    /// 采集值
    /// </summary>
    public object? ReadValue { get; set; }

    /// <summary>
    /// 是否读成功
    /// </summary>
    public bool ReadIsSuccess { get; set; }
}

public class WritePointResult
{
    /// <summary>
    /// 采集点参数名（如Label）
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 写入的原始值
    /// </summary>
    public string OriginalValue { get; set; } = string.Empty;

    /// <summary>
    /// 写结果
    /// </summary>
    public string? WriteResult { get; set; }

    /// <summary>
    /// 是否写成功
    /// </summary>
    public bool WriteIsSuccess { get; set; }


    /// <summary>
    /// 读取该采集点所用的时间（毫秒）
    /// </summary>
    public long WriteElapsedMilliseconds { get; set; }
}