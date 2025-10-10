using HslCommunication.Core;
using KEDA_EdgeServices.Enums;
using KEDA_EdgeServices.Models;
using System.IO.Ports;
using System.Net;
using System.Text.RegularExpressions;

namespace KEDA_EdgeServices.Helpers;

public class ValidateHelper
{
    private readonly int[] _validBaudRates =
    [
        110, 300, 600, 1200, 2400, 4800, 9600,
        14400, 19200, 38400, 57600, 115200,
        128000, 256000
    ];
    public string ProtocolValidate(Protocol? protocol)
    {
        if (protocol == null) return "协议为空，请检查";

        if (string.IsNullOrWhiteSpace(protocol.ProtocolID)) return $"[协议]id为空，请检查[{nameof(protocol.ProtocolID)}]";

        if (string.IsNullOrWhiteSpace(protocol.Interface)) return $"[协议]{protocol.ProtocolID}的接口类型Interface为空";

        if (string.IsNullOrWhiteSpace(protocol.ProtocolType)) return $"[协议{protocol.ProtocolID}]的协议类型ProtocolType为空";

        // 校验协议类型是否属于对应接口类型
        if (protocol.Interface == "LAN")
        {
            if (!Enum.TryParse<LanProtocolType>(protocol.ProtocolType, ignoreCase: true, out _) || int.TryParse(protocol.ProtocolType, out _))
                return $"[协议{protocol.ProtocolID}]协议类型[{protocol.ProtocolType}]暂未实现或不支持LAN接口";
        }
        else if (protocol.Interface == "COM")
        {
            if (!Enum.TryParse<ComProtocolType>(protocol.ProtocolType, ignoreCase: true, out _) || int.TryParse(protocol.ProtocolType, out _))
                return $"[协议{protocol.ProtocolID}]协议类型[{protocol.ProtocolType}]暂未实现或不支持COM接口";
        }
        else
            return $"[协议{protocol.ProtocolID}]协议接口类型[{protocol.Interface}]暂未实现或不支持";

        if (string.IsNullOrWhiteSpace(protocol.CollectCycle)) return $"[协议]{protocol.ProtocolID}的通讯延时CollectCycle为空";

        if (string.IsNullOrWhiteSpace(protocol.ReceiveTimeOut)) return $"[协议]{protocol.ProtocolID}的接收超时ReceiveTimeOut为空";

        if (string.IsNullOrWhiteSpace(protocol.ConnectTimeOut)) return $"[协议]{protocol.ProtocolID}的连接超时ConnectTimeOut为空";

        if (protocol.Interface == "LAN")
        {
            if (string.IsNullOrWhiteSpace(protocol.IPAddress))
                return $"[网口协议]{protocol.ProtocolType}的ip地址IPAddress为空，协议id是{protocol.ProtocolID}";

            if (!IPAddress.TryParse(protocol.IPAddress, out _))
                return $"[网口协议]{protocol.ProtocolType}的ip地址IPAddress格式不正确，协议id是{protocol.ProtocolID}";

            if (string.IsNullOrWhiteSpace(protocol.ProtocolPort))
                return $"[网口协议]{protocol.ProtocolType}的端口号ProtocolPort为空，协议id是{protocol.ProtocolID}";

            //if (!uint.TryParse(protocol.ProtocolPort, out var port) || port > 65535)
            //    return $"[网口协议]{protocol.ProtocolType}的端口号ProtocolPort格式不正确，协议id是{protocol.ProtocolID}";

        }
        else if (protocol.Interface == "COM")
        {
            if (string.IsNullOrWhiteSpace(protocol.PortName))
                return $"[串口协议]{protocol.ProtocolType}的串口号PortName为空，协议id是{protocol.ProtocolID}";

            if (!SerialPort.GetPortNames().Contains(protocol.PortName))
                return $"[串口协议]{protocol.ProtocolType}的串口号PortName用SerialPort.GetPortNames()方法找不到，协议id是{protocol.ProtocolID}";

            if (string.IsNullOrWhiteSpace(protocol.BaudRate))
                return $"[串口协议]{protocol.ProtocolType}的波特率BaudRate为空，协议id是{protocol.ProtocolID}";

            if (!int.TryParse(protocol.BaudRate, out int baudRate) || !_validBaudRates.Contains(baudRate))
                return $"[串口协议]{protocol.ProtocolType}的波特率BaudRate格式不正确，协议id是{protocol.ProtocolID}";

            if (string.IsNullOrWhiteSpace(protocol.DataBits))
                return $"[串口协议]{protocol.ProtocolType}的数据位DataBits为空，协议id是{protocol.ProtocolID}";

            if (!int.TryParse(protocol.DataBits, out int dataBits) || dataBits < 5 || dataBits > 8)
                return $"[串口协议]{protocol.ProtocolType}的数据位DataBits格式不正确，协议id是{protocol.ProtocolID}";

            if (string.IsNullOrWhiteSpace(protocol.StopBits))
                return $"[串口协议]{protocol.ProtocolType}的停止位StopBits为空，协议id是{protocol.ProtocolID}";

            if (!Enum.TryParse<StopBits>(protocol.StopBits, out _) || int.TryParse(protocol.StopBits, out _))
                return $"[串口协议]{protocol.ProtocolType}的停止位StopBits格式不正确，协议id是{protocol.ProtocolID}";

            if (string.IsNullOrWhiteSpace(protocol.Parity))
                return $"[串口协议]{protocol.ProtocolType}的校验位Parity为空，协议id是{protocol.ProtocolID}";

            if (!Enum.TryParse<Parity>(protocol.Parity, out _) || int.TryParse(protocol.Parity, out _))
                return $"[串口协议]{protocol.ProtocolType}的校验位Parity格式不正确，协议id是{protocol.ProtocolID}";
        }

        if (protocol.ProtocolType.Contains("Modbus", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(protocol.Format))
                return $"[Modbus系列]{protocol.ProtocolType}的Format为空，协议id是{protocol.ProtocolID}";

            if (!Enum.TryParse<DataFormat>(protocol.Format, out _) || int.TryParse(protocol.Format, out _))
                return $"[Modbus系列]{protocol.ProtocolType}的Format格式不正确，协议id是{protocol.ProtocolID}";

            if (string.IsNullOrWhiteSpace(protocol.AddressStartWithZero))
                return $"[Modbus系列]{protocol.ProtocolType}的AddressStartWithZero为空，协议id是{protocol.ProtocolID}";

            if (!bool.TryParse(protocol.AddressStartWithZero, out _))
                return $"[Modbus系列]{protocol.ProtocolType}的AddressStartWithZero格式不正确，协议id是{protocol.ProtocolID}";
        }

        if (protocol.ProtocolType.Equals("CJT188OverTcp_2004", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(protocol.InstrumentType))
                return $"[CJT188OverTcp_2004]{protocol.ProtocolType}的仪表类型为空，协议id是{protocol.ProtocolID}";
        }

        if (protocol.Devices == null || protocol.Devices.Count == 0)
            return "[协议]{protocol.ProtocolID}的设备列表Devices为空或数量为0";

        foreach (var device in protocol.Devices)
        {
            var deviceValidateResults = DeviceValidate(protocol.ProtocolID, protocol.ProtocolType, protocol.Devices);
            if (string.IsNullOrWhiteSpace(deviceValidateResults))
            {
                var pointValidateResults = PointValidate(device.EquipmentID, device.EquipmentName, device.Points, protocol.ProtocolType);
                if (!string.IsNullOrWhiteSpace(pointValidateResults)) return pointValidateResults;
            }
            return deviceValidateResults;
        }

        return string.Empty;
    }

    private string DeviceValidate(string protocolId, string protocolType, List<Device> devices)
    {
        for (int i = 0; i < devices.Count; i++)
        {
            var device = devices[i];
            if (device == null)
                return $"[协议]{protocolId}的设备列表Devices中第{i + 1}个设备对象为null";

            if (string.IsNullOrWhiteSpace(device.EquipmentID))
                return $"[设备]第{i + 1}个设备的EquipmentID为空，协议id是{protocolId}";

            if (string.IsNullOrWhiteSpace(device.EquipmentName))
                return $"[设备]{device.EquipmentID}的设备名称EquipmentName为空，协议id是{protocolId}";

            if (string.IsNullOrWhiteSpace(device.Type))
                return $"[设备]{device.EquipmentID}的Type为空，协议id是{protocolId}";

            if (device.Points == null || device.Points.Count == 0)
                return $"[设备]{device.EquipmentID}的点位列表Points为空或数量为0，协议id是{protocolId}";

            if (protocolType.Contains("Modbus", StringComparison.OrdinalIgnoreCase) ||
                protocolType.Contains("DLT6452007", StringComparison.OrdinalIgnoreCase) ||
                protocolType.Equals("CJT188OverTcp_2004", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(device.StationNo))
                    return $"[设备]{device.EquipmentID}的站号StationNo为空，设备名称是{device.EquipmentName}，协议id是{protocolId}";

                //if (!long.TryParse(device.StationNo, out _))
                //    return $"[设备]{device.EquipmentID}的站号StationNo格式不正确，设备名称是{device.EquipmentName}，协议id是{protocolId}";
            }

            if (device.Points == null || device.Points.Count == 0)
                return $"[设备]{device.EquipmentID}的点位列表Points为空或数量为0，设备名称是{device.EquipmentName}";
        }

        return string.Empty;
    }

    private string PointValidate(string deviceId, string deviceName, List<Point> points, string protocolType)
    {
        if( !protocolType.Contains("GP1125T" ) && !protocolType.Contains("FJ1000Jet") && !protocolType.Contains("FJ60W"))
        {
            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                if (point == null)
                    return $"[设备]{deviceId}的点位列表Points中第{i + 1}个点对象为null，设备名称是{deviceName}";

                if (string.IsNullOrWhiteSpace(point.Label))
                    return $"[点位]第{i + 1}个点的Label为空，设备ID={deviceId}，设备名称={deviceName}";

                if (string.IsNullOrWhiteSpace(point.DataType))
                    return $"[点位]{point.Label}的DataType为空，设备ID={deviceId}，设备名称={deviceName}";

                if (!Enum.TryParse<DataType>(point.DataType, ignoreCase: true, out _) || int.TryParse(point.DataType, out _))
                    return $"[点位]数据类型[{point.DataType}]暂未实现";

                if (string.IsNullOrWhiteSpace(point.Address))
                    return $"[点位]{point.Label}的Address为空，设备ID={deviceId}，设备名称={deviceName}";

                // 可选：校验Length为正整数
                if (point.DataType == DataType.String.ToString() && protocolType != LanProtocolType.FJ1000Jet.ToString() && protocolType != LanProtocolType.FJ60W.ToString())
                {
                    if (!string.IsNullOrWhiteSpace(point.Length) && !int.TryParse(point.Length, out _))
                        return $"[点位]{point.Label}的Length格式不正确，设备ID={deviceId}，设备名称={deviceName}";
                }

                // 可选：校验MinValue/MaxValue为数字
                if (!string.IsNullOrWhiteSpace(point.MinValue) && !double.TryParse(point.MinValue, out _))
                    return $"[点位]{point.Label}的MinValue格式不正确，设备ID={deviceId}，设备名称={deviceName}";
                if (!string.IsNullOrWhiteSpace(point.MaxValue) && !double.TryParse(point.MaxValue, out _))
                    return $"[点位]{point.Label}的MaxValue格式不正确，设备ID={deviceId}，设备名称={deviceName}";

                //if (!string.IsNullOrWhiteSpace(point.Change))
                //{
                //    // 一元一次方程正则
                //    var linearExprPattern = @"^([+-]?\d*\.?\d*)\s*\*?\s*x(\s*[+-]\s*\d+(\.\d+)?)?$|^x\s*\*\s*([+-]?\d*\.?\d*)(\s*[+-]\s*\d+(\.\d+)?)?$|^x(\s*[+-]\s*\d+(\.\d+)?)?$|^([+-]?\d+(\.\d+)?)$";
                //    if (!Regex.IsMatch(point.Change.Replace(" ", ""), linearExprPattern, RegexOptions.IgnoreCase))
                //        return $"[点位]{point.Label}的Change不是有效的一元一次方程，设备ID={deviceId}，设备名称={deviceName}";
                //}
            }

            return string.Empty;
        }
        else
            return string.Empty;
      
    }

    public bool ProtocolAllPointsWriteValueIsEmpty(Protocol protocol)
    {
        // 如果所有点的WriteValue都为空，返回true，否则false
        return protocol.Devices != null
            && protocol.Devices.SelectMany(d => d.Points ?? Enumerable.Empty<Point>())
                .All(p => string.IsNullOrWhiteSpace(p.WriteValue));
    }
}
