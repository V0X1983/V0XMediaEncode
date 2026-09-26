using V0XMediaEncode.Services.Ffmpeg;

namespace V0XMediaEncode.Services.Tests;

public class FFmpegTimestampFormatterTests
{
    [Theory]
    [InlineData(0, 0, 0, 0, "00:00:00.00")]
    [InlineData(0, 0, 41, 230, "00:00:41.23")]
    [InlineData(1, 2, 3, 450, "01:02:03.45")]
    [InlineData(25, 0, 0, 0, "25:00:00.00")]
    public void Format_ProducesFfmpegCompatibleTimestamp(int hours, int minutes, int seconds, int milliseconds, string expected)
    {
        var value = new TimeSpan(0, hours, minutes, seconds, milliseconds);

        Assert.Equal(expected, FFmpegTimestampFormatter.Format(value));
    }

    [Fact]
    public void Format_ClampsNegativeValuesToZero()
    {
        Assert.Equal("00:00:00.00", FFmpegTimestampFormatter.Format(TimeSpan.FromSeconds(-5)));
    }
}
