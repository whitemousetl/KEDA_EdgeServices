namespace KEDA_EdgeServices.Enums;

public enum DeviceWriteStatus
{
    AllPointsWriteSuccess = 1000,
    AllPointsWriteFailed = 2000,
    AnyPointWriteFailed = 5000,
}
