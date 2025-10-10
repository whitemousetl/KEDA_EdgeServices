using KEDA_EdgeServices.Models;
using KEDA_EdgeServices.Protocols.FreeProtocols;
using KEDA_EdgeServices.Services;

namespace KEDA_EdgeServices.Extensions;

public static class MinimalApiExtensions
{
    public static void MapProtocolEngineApis(this WebApplication app)
    {
        app.MapGet("/test", () => "Hello World");

        app.MapPost("/read-or-write-device", (ProtocolEngineService service, Protocol? protocol) => service.ReadOrWriteAsync(protocol));
    }
}
