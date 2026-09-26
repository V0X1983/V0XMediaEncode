namespace V0XMediaEncode.Core.Models;

public enum EncodeStatus
{
    Queued,
    Probing,
    Ready,
    Encoding,
    Paused,
    Completed,
    Failed,
    Cancelled,
}
