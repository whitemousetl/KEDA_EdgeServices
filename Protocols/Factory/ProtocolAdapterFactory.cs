using KEDA_EdgeServices.Models;
using KEDA_EdgeServices.Protocols.Attributes;
using KEDA_EdgeServices.Protocols.Interfaces;

namespace KEDA_EdgeServices.Protocols.Factory;

public class ProtocolAdapterFactory : IProtocolAdapterFactory
{
    private readonly IServiceProvider _sp;
    private readonly Dictionary<string, Type> _typeMap;

    public ProtocolAdapterFactory(IServiceProvider sp)
    {
        _sp = sp;
        _typeMap = typeof(IProtocolAdapter).Assembly
            .GetTypes()
            .Where(t => typeof(IProtocolAdapter).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(t => t.GetCustomAttributes(typeof(ProtocolTypeAttribute), false)
            .Cast<ProtocolTypeAttribute>()
            .Select(attr => new { attr.ProtocolType, Type = t }))
            .ToDictionary(x => x.ProtocolType, x => x.Type, StringComparer.OrdinalIgnoreCase);
    }

    public IProtocolAdapter CreateAdapter(Protocol protocol)
    {
        ArgumentNullException.ThrowIfNull(protocol);

        if(_typeMap.TryGetValue(protocol.ProtocolType, out var type))
            return (IProtocolAdapter)_sp.GetRequiredService(type);

        throw new NotSupportedException($"不支持的协议类型: {protocol.ProtocolType}");
    }
}
