namespace KEDA_EdgeServices.Protocols.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ProtocolTypeAttribute : Attribute
{
    public string ProtocolType { get; }
    public ProtocolTypeAttribute(string protocolType) => ProtocolType = protocolType;
}
