namespace V0XMediaEncode.Services.Ffmpeg;

public sealed record EncodeResult(bool Success, int ExitCode, string? ErrorTail);
