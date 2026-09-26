using V0XMediaEncode.Core.Models;
using V0XMediaEncode.Services.History;

namespace V0XMediaEncode.Services.Tests;

public class HistoryLogFormatterTests
{
    private static HistoryEntry CreateEntry(EncodeStatus status = EncodeStatus.Completed, string? errorMessage = null, string? logTail = null) => new()
    {
        SourcePath = @"C:\in\video.mov",
        OutputPath = @"C:\out\video.mp4",
        PresetName = "H.264 - YouTube 1080p",
        Status = status,
        StartedAt = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero),
        CompletedAt = new DateTimeOffset(2026, 1, 1, 10, 5, 30, TimeSpan.Zero),
        ErrorMessage = errorMessage,
        LogTail = logTail,
    };

    [Fact]
    public void FormatEntry_IncludesCoreFields()
    {
        var text = HistoryLogFormatter.FormatEntry(CreateEntry());

        Assert.Contains(@"C:\in\video.mov", text);
        Assert.Contains(@"C:\out\video.mp4", text);
        Assert.Contains("H.264 - YouTube 1080p", text);
        Assert.Contains("Completed", text);
        Assert.Contains("00:05:30", text);
    }

    [Fact]
    public void FormatEntry_OmitsErrorSection_WhenNoError()
    {
        var text = HistoryLogFormatter.FormatEntry(CreateEntry());

        Assert.DoesNotContain("Erreur :", text);
        Assert.DoesNotContain("journal ffmpeg", text);
    }

    [Fact]
    public void FormatEntry_IncludesErrorAndLogTail_WhenFailed()
    {
        var text = HistoryLogFormatter.FormatEntry(CreateEntry(
            status: EncodeStatus.Failed,
            errorMessage: "Codec non supporté",
            logTail: "frame=  120 fps=30 ..."));

        Assert.Contains("Erreur : Codec non supporté", text);
        Assert.Contains("journal ffmpeg", text);
        Assert.Contains("frame=  120 fps=30 ...", text);
    }

    [Fact]
    public void FormatAll_OrdersByCompletedAtDescending()
    {
        var older = CreateEntry();
        older.CompletedAt = new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);
        var newer = CreateEntry();
        newer.CompletedAt = new DateTimeOffset(2026, 1, 2, 8, 0, 0, TimeSpan.Zero);

        var text = HistoryLogFormatter.FormatAll([older, newer]);

        var newerIndex = text.IndexOf("Terminé : 2026-01-02", StringComparison.Ordinal);
        var olderIndex = text.IndexOf("Terminé : 2026-01-01", StringComparison.Ordinal);
        Assert.True(newerIndex >= 0 && olderIndex >= 0 && newerIndex < olderIndex);
    }
}
