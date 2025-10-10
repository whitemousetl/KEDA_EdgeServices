using HslCommunication;
using KEDA_EdgeServices.Configs;
using KEDA_EdgeServices.Helpers;
using KEDA_EdgeServices.Models;
using KEDA_EdgeServices.Protocols.Factory;
using KEDA_EdgeServices.Protocols.Interfaces;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace KEDA_EdgeServices.Services;

public class ProtocolEngineService
{
    private readonly ProtocolAdapterFactory _protocolFactory;
    private readonly ValidateHelper _validateHelper;
    private ConcurrentDictionary<string, IProtocolAdapter> _protocolInstances = [];
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _protocolLocks = [];
    private readonly ConcurrentDictionary<string, List<ProtocolRequest>> _protocolRequestQueues = [];
    private readonly ConcurrentDictionary<string, ProtocolRequest> _currentRequests = [];
    private readonly ILogger<ProtocolEngineService> _logger;


    public ProtocolEngineService(ProtocolAdapterFactory protocolFactory, ValidateHelper validateHelper, ILogger<ProtocolEngineService> logger)
    {
        _protocolFactory = protocolFactory;
        _validateHelper = validateHelper;
        _logger = logger;   
    }

    public async Task<IResult> ReadOrWriteAsync(Protocol? protocol)
    {
        try
        {
            var validateMsg = _validateHelper.ProtocolValidate(protocol);
            if (!string.IsNullOrEmpty(validateMsg))
            {
                if(protocol.IsLogPoints)
                    _logger.LogError(validateMsg);
                return Results.Ok(ApiResponse<string>.Fial(validateMsg));
            }

            var protocolInstance = GetOrAddProtocolInstance(protocol);
            var semaphore = _protocolLocks.GetOrAdd(protocol.ProtocolID, _ => new SemaphoreSlim(1, 1));
            var requestQueue = _protocolRequestQueues.GetOrAdd(protocol.ProtocolID, _ => []);
            var cts = new CancellationTokenSource();
            var isWrite = !ProtocolAllPointsWriteValueIsEmpty(protocol);
            var request = new ProtocolRequest { IsWrite = isWrite, CancellationTokenSource = cts };

            lock (requestQueue)
            {
                requestQueue.Add(request);
            }

            // 写请求到来时，取消所有排队和正在执行的读请求
            if (isWrite)
            {
                lock (requestQueue)
                {
                    foreach (var req in requestQueue.ToList())
                    {
                        if (!req.IsWrite)
                        {
                            req.CancellationTokenSource.Cancel();
                            requestQueue.Remove(req);
                        }
                    }
                }
                if (_currentRequests.TryGetValue(protocol.ProtocolID, out var runningReq) && !runningReq.IsWrite)
                {
                    runningReq.CancellationTokenSource.Cancel();
                }
            }

            try
            {
                await semaphore.WaitAsync(cts.Token);

                _currentRequests[protocol.ProtocolID] = request;

                var res = await protocolInstance.ReadOrWriteAsync(protocol, cts.Token);

                return Results.Ok(ApiResponse<List<DeviceDataResult>>.Success("读取完成", res));
            }
            catch (OperationCanceledException ex)
            {               
                _logger.LogError("任务被取消" + ex.Message);
                return Results.Ok(ApiResponse<string>.FromException(ex));
            }
            catch (Exception ex)
            {
                _logger.LogError("读取异常" + ex.Message);
                return Results.Ok(ApiResponse<string>.FromException(ex));
            }
            finally
            {
                _currentRequests.TryRemove(protocol.ProtocolID, out _);
                lock (requestQueue)
                {
                    requestQueue.Remove(request);
                }
                semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            var msg = "ReadOrWriteAsync异常" + ex.Message;
            _logger.LogError(msg);
            return Results.Ok(ApiResponse<string>.FromException(ex));
        }
    }

    private IProtocolAdapter GetOrAddProtocolInstance(Protocol protocol)
    {
        var key = protocol.ProtocolID;

        var protocoInstance = _protocolFactory.CreateAdapter(protocol);

        return _protocolInstances.GetOrAdd(key, _ => protocoInstance);
    }

    private bool ProtocolAllPointsWriteValueIsEmpty(Protocol protocol)
    {
        if (protocol?.Devices == null || protocol.Devices.Count == 0)
            return true;

        foreach (var device in protocol.Devices)
        {
            if (device?.Points == null || device.Points.Count == 0)
                continue;

            foreach (var point in device.Points)
            {
                if (!string.IsNullOrEmpty(point?.WriteValue))
                    return false;
            }
        }
        return true;
    }

    private class ProtocolRequest
    {
        public bool IsWrite { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; } = new();
    }
}
