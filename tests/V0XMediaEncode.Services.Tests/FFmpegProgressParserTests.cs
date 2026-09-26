using V0XMediaEncode.Services.Ffmpeg;

namespace V0XMediaEncode.Services.Tests;

public class FFmpegProgressParserTests
{
    private const string SampleLine =
        "frame= 1234 fps=59.94 q=28.0 size=  102400kB time=00:00:41.23 bitrate=2000.0kbits/s speed=1.99x";

    [Fact]
    public void TryParse_ExtractsAllFields()
    {
        var ok = FFmpegProgressParser.TryParse(SampleLine, TimeSpan.FromMinutes(2), out var progress);

        Assert.True(ok);
        Assert.Equal(1234, progress.FrameNumber);
        Assert.Equal(59.94, progress.Fps);
        Assert.Equal(new TimeSpan(0, 0, 0, 41, 230), progress.TimeProcessed);
        Assert.Equal(1.99, progress.SpeedFactor);
        Assert.Equal(102400L * 1024, progress.OutputSizeBytes);
    }

    [Fact]
    public void TryParse_ComputesPercentAndEta_WhenTotalDurationKnown()
    {
        var totalDuration = TimeSpan.FromSeconds(82.46); // exactly 2x the sample's time=41.23
        var ok = FFmpegProgressParser.TryParse(SampleLine, totalDuration, out var progress);

        Assert.True(ok);
        Assert.Equal(50.0, progress.PercentComplete, 3);
        Assert.NotNull(progress.Eta);
        // Remaining 41.23s of source at speed=1.99x => ~20.72s wall-clock left.
        Assert.Equal(41.23 / 1.99, progress.Eta!.Value.TotalSeconds, 1);
    }

    [Fact]
    public void TryParse_ReturnsFalse_WhenNoTotalDuration()
    {
        var ok = FFmpegProgressParser.TryParse(SampleLine, null, out var progress);

        Assert.True(ok);
        Assert.Equal(0, progress.PercentComplete);
        Assert.Null(progress.Eta);
    }

    [Theory]
    [InlineData("ffmpeg version 6.0 Copyright (c) 2000-2023 the FFmpeg developers")]
    [InlineData("  built with gcc 12.2.0")]
    [InlineData("Stream mapping:")]
    public void TryParse_ReturnsFalse_ForNonProgressLines(string line)
    {
        var ok = FFmpegProgressParser.TryParse(line, TimeSpan.FromMinutes(1), out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryParse_HandlesFinalSummaryLine_WithLsizePrefix()
    {
        const string finalLine =
            "frame= 3000 fps= 60 q=-1.0 Lsize=  512000kB time=00:00:50.00 bitrate=8388.6kbits/s speed=   2x";

        var ok = FFmpegProgressParser.TryParse(finalLine, TimeSpan.FromSeconds(50), out var progress);

        Assert.True(ok);
        Assert.Equal(3000, progress.FrameNumber);
        Assert.Equal(100.0, progress.PercentComplete, 3);
        Assert.Equal(512000L * 1024, progress.OutputSizeBytes);
    }

    [Theory]
    [InlineData("00:00:41.23", 0, 0, 41, 230)]
    [InlineData("01:02:03.45", 1, 2, 3, 450)]
    [InlineData("00:00:00.00", 0, 0, 0, 0)]
    public void ParseFfmpegTimestamp_ParsesCentisecondPrecision(string text, int hours, int minutes, int seconds, int milliseconds)
    {
        var result = FFmpegProgressParser.ParseFfmpegTimestamp(text);

        var expected = new TimeSpan(0, hours, minutes, seconds, milliseconds);
        Assert.Equal(expected, result);
    }
}
