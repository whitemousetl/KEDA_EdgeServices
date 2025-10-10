using KEDA_EdgeServices.Models;
using KEDA_EdgeServices.Protocols.Interfaces;

namespace KEDA_EdgeServices.Protocols.Factory;

public interface IProtocolAdapterFactory
{
    IProtocolAdapter CreateAdapter(Protocol protocol);
}
