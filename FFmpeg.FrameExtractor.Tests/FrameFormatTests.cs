using FrameExtractor;
using FrameExtractor.Extensions;

namespace FFmpeg.FrameExtractor.Tests;

public class FrameFormatTests
{
    [Theory]
    [InlineData(FrameFormat.Jpg, "mjpeg")]
    [InlineData(FrameFormat.Png, "png")]
    public void GetVcodec_ReturnsCorrectCodec(FrameFormat format, string expectedCodec)
    {
        // Act
        var codec = format.GetVcodec();

        // Assert
        Assert.Equal(expectedCodec, codec);
    }

    [Fact]
    public void FrameFormat_HasExpectedValues()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(FrameFormat), FrameFormat.Jpg));
        Assert.True(Enum.IsDefined(typeof(FrameFormat), FrameFormat.Png));
    }

    [Fact]
    public void FrameFormat_CanBeCompared()
    {
        // Act & Assert
        Assert.Equal(FrameFormat.Jpg, FrameFormat.Jpg);
        Assert.NotEqual(FrameFormat.Jpg, FrameFormat.Png);
    }
}
