using KEDA_EdgeServices.Models;
using System.Threading.Channels;

namespace KEDA_EdgeServices.Protocols.Interfaces;

public interface IProtocolAdapter
{
    Task<List<DeviceDataResult>> ReadOrWriteAsync(Protocol protocol, CancellationToken ct);
}
