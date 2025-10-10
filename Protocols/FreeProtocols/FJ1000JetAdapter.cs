
using HslCommunication.Profinet.Freedom;
using KEDA_EdgeServices.Configs;
using KEDA_EdgeServices.Models;
using KEDA_EdgeServices.Protocols.Attributes;
using KEDA_EdgeServices.Protocols.Interfaces;
using KEDA_EdgeServices.Protocols.Tcp;
using Microsoft.VisualBasic;
using System.Drawing;
using System.Net.Sockets;
using static System.Net.Mime.MediaTypeNames;

namespace KEDA_EdgeServices.Protocols.FreeProtocols;

[ProtocolType("FJ1000Jet")]//砖侧码
public class FJ1000JetAdapter : IProtocolAdapter
{
    private FreedomTcpNet _connection = new ();
    private string _protocolType => "FJ1000Jet";
    private readonly ILogger _logger;
    public FJ1000JetAdapter(ILogger<FJ1000JetAdapter> logger) 
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

        var hexList = new List<byte>();
        hexList.Add(0x1B);
        hexList.Add(0x02);
        var station = StringToHex(protocol.Devices[0].StationNo);
        //地址
        hexList.Add(station);
        hexList.Add(0x1D);
        var points = protocol.Devices[0].Points;
        var strCount = (byte)points.Count;
        //字段总数
        hexList.Add(strCount);
        //文本信息
        var msgByteList = ConvertInformationIntoHexadecimal(points);
        hexList.AddRange(msgByteList);
        hexList.Add(0x1B);
        hexList.Add(0x03);
        //校验码
        var checksum =  Checksum([.. hexList]);
        hexList.Add(checksum);

        var res = await _connection.ReadFromCoreServerAsync([.. hexList]);
        var devDataRes = new DeviceDataResult();
        if (res.IsSuccess)
            devDataRes.DeviceMsg = res.Content[3].ToString("X2");
        else
        {
            devDataRes.DeviceMsg = res.Message;
            _logger.LogError(res.Message);
        }

        result.Add(devDataRes);

        return result;
    }

    #region 翻译指令
    private byte[] ConvertInformationIntoHexadecimal(List<Models.Point> points)
    {
        var msgByteList = new List<byte>();
        foreach (var point in points)
        {
            //字段标识
            msgByteList.Add(StringToHex(point.Label));
            ////文本字节数
            //msgByteList.Add((byte)point.Address.Length);
            ////文本
            //byte[] bytes = System.Text.Encoding.UTF8.GetBytes(point.Address);
            //msgByteList.AddRange(bytes);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(point.Address);
            msgByteList.Add((byte)bytes.Length); // 用字节长度
            msgByteList.AddRange(bytes);

        }

        return msgByteList.ToArray();
    }

    public byte Checksum(byte[] bytes)
    {
        // 计算所有字节的总和
        int sum = 0;
        foreach (byte b in bytes)
        {
            sum += b;
        }

        // 对256求模
        int mod = sum % 256;

        // 计算2补码
        int checksum = (~mod + 1) & 0xFF;

        return (byte)checksum;
    }

    private byte StringToHex(string station)
    {
        if (!int.TryParse(station, out var result))
        {
            var msg = "输入不是有效的十进制数字字符串";
            _logger.LogError($"协议{_protocolType} " + msg);
            throw new ArgumentException(msg);
        }

        if (result < 0 || result > 255)
        {
            var msg = "数字必须在0到255之间";
            _logger.LogError($"协议{_protocolType} " + msg);
            throw new ArgumentOutOfRangeException(msg);
        }

        return (byte)result;
    } 
    #endregion

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
