using HslCommunication.Profinet.Freedom;
using KEDA_EdgeServices.Configs;
using KEDA_EdgeServices.Models;
using KEDA_EdgeServices.Protocols.Attributes;
using KEDA_EdgeServices.Protocols.Interfaces;
using lib60870.CS104;
using System.Net.Sockets;
using System.Text;

namespace KEDA_EdgeServices.Protocols.FreeProtocols;

[ProtocolType("BottomImageF1970")]//底码图片
public class BottomImageF1970 : IProtocolAdapter
{
    private Socket _connection = null!;
    private string _protocolType => "BottomImageF1970";
    private readonly ILogger _logger;
    public BottomImageF1970(ILogger<BottomImageF1970> logger)
    {
        _logger = logger;
    }

    //public async Task<List<DeviceDataResult>> ReadOrWriteAsync(Protocol protocol, CancellationToken ct)
    //{
    //    InitOrReset(protocol);

    //    if (_connection == null)
    //    {
    //        if (protocol.IsLogPoints)
    //            _logger.LogError($"初始化或重置{_protocolType}连接失败，请检查。");
    //        return [];
    //    }

    //    var result = new List<DeviceDataResult>();

    //    var address = protocol.Devices[0].Points[0].Address;

    //    byte[] hexData = HexStringToBytes(address);

    //    int chunkSize = 8192;
    //    int offset = 0;

    //    while (offset < hexData.Length)
    //    {
    //        int size = Math.Min(chunkSize, hexData.Length - offset);

    //        // 关键：直接用 Socket.Send，保证每次一次性发出去
    //        _connection.Send(hexData, offset, size, SocketFlags.None);

    //        offset += size;

    //        // 必须完全模拟调试助手：300ms 间隔
    //        await Task.Delay(300, ct);
    //    }

    //    byte[] buffer = new byte[4096];
    //    int bytesRead = await _connection.ReceiveAsync(buffer);

    //    string res = string.Empty;

    //    if (bytesRead > 0)
    //    {
    //        res = Encoding.ASCII.GetString(buffer, 0, bytesRead);
    //        int idx = res.IndexOf("\r\n", StringComparison.Ordinal);
    //        if (idx >= 0)
    //            res = res.Substring(0, idx); // 只保留第一个换行符前的内容
    //    }

    //    var devDataRes = new DeviceDataResult();
    //    devDataRes.DeviceMsg = res;

    //    result.Add(devDataRes);

    //    return result;
    //}

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

        var address = protocol.Devices[0].Points[0].Address;

        // 直接读取文件内容
        byte[] fileData = HexStringToBytes(address);

        if (fileData.Length == 0)
        {
            _logger.LogWarning("待发送数据为空。");
            result.Add(new DeviceDataResult { DeviceMsg = "EMPTY" });
            return result;
        }

        // 发送前 8 字节（如果不足 8，则直接发送全部然后结束）
        int headLen = Math.Min(8, fileData.Length);
        await SendAllAsync(fileData.AsMemory(0, headLen), ct);

        int chunkSize = 4096; // 你可以保持原值或调整
        int offset = headLen;

        while (offset < fileData.Length)
        {
            ct.ThrowIfCancellationRequested();

            int size = Math.Min(chunkSize, fileData.Length - offset);
            await SendAllAsync(fileData.AsMemory(offset, size), ct);
            offset += size;

            // 每包等待 30ms
            await Task.Delay(300, ct);
        }

        byte[] buffer = new byte[4096];
        int bytesRead = await _connection.ReceiveAsync(buffer);

        string res = string.Empty;

        if (bytesRead > 0)
        {
            res = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            int idx = res.IndexOf("\r\n", StringComparison.Ordinal);
            if (idx >= 0)
                res = res.Substring(0, idx); // 只保留第一个换行符前的内容
        }

        var devDataRes = new DeviceDataResult();
        devDataRes.DeviceMsg = res;

        result.Add(devDataRes);

        return result;
    }

    // 发送全部数据的安全方法
    private async Task SendAllAsync(ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        int sent = 0;
        while (sent < data.Length)
        {
            ct.ThrowIfCancellationRequested();
            int n = await _connection.SendAsync(data.Slice(sent), SocketFlags.None, ct);
            if (n <= 0)
                throw new IOException("发送返回 0，连接可能已断开。");
            sent += n;
        }
    }
    #region 翻译指令


    static byte[] HexStringToBytes(string hexString)
    {
        // 去除空格、换行、制表符、逗号、分号等常见分隔符
        var charsToRemove = new[] { " ", "\r", "\n", "\t", ",", ";", "\r\n" };
        foreach (var c in charsToRemove)
        {
            hexString = hexString.Replace(c, "");
        }
        int len = hexString.Length;
        if (len % 2 != 0)
            throw new ArgumentException("十六进制字符串长度必须为偶数。");
        byte[] bytes = new byte[len / 2];
        for (int i = 0; i < len; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
        }
        return bytes;
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

        // 判断是否需要重连
        if (_connection == null || !_connection.Connected || _lastConfig == null || !_lastConfig.Equals(config) || protocol.ResetConnection)
        {
            // 关闭旧连接
            try
            {
                _connection?.Close();
            }
            catch { }

            try
            {
                _connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    NoDelay = true,
                    SendBufferSize = 8192,
                    ReceiveBufferSize = 8192
                };

                _connection.Connect(config.Ip, config.Port);
                _lastConfig = config;
                _connection.SendTimeout = int.TryParse(protocol.ConnectTimeOut, out var sendTimeout) ? sendTimeout : 5000;
                _connection.ReceiveTimeout = int.TryParse(protocol.ReceiveTimeOut, out var receiveTimeout) ? receiveTimeout : 5000;
            }
            catch (Exception ex)
            {
                if (protocol.IsLogPoints)
                    _logger.LogError($"{_protocolType}连接失败: {ex.Message}");
                throw new IOException($"{_protocolType}连接失败: {ex.Message}");
            }
            _lastConfig = config;
        }
    }
}
