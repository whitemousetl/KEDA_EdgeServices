namespace KEDA_EdgeServices.Models;

public class WriteTask
{
    public Protocol Protocol { get; set; } = new();
    public TaskCompletionSource<DeviceDataResult> CompletionSource { get; set; } = new();
}