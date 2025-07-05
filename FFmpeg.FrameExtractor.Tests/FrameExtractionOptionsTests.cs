using FrameExtractor;

namespace FFmpeg.FrameExtractor.Tests;

public class FrameExtractionOptionsTests
{
    [Fact]
    public void Default_ReturnsValidOptions()
    {
        // Act
        var options = new FrameExtractionOptions();

        // Assert
        Assert.Equal(FrameFormat.Jpg, options.FrameFormat);
        Assert.False(options.EnableHardwareAcceleration);
        Assert.Null(options.FrameSize);
        Assert.Null(options.TimeLimit);
        Assert.Null(options.Fps);
    }

    [Fact]
    public void FrameSize_CanBeSet()
    {
        // Arrange
        var options = new FrameExtractionOptions();
        var frameSize = new FrameSize { Width = 1920, Height = 1080 };

        // Act
        options.FrameSize = frameSize;

        // Assert
        Assert.Equal(frameSize, options.FrameSize);
    }

    [Fact]
    public void TimeLimit_CanBeSet()
    {
        // Arrange
        var options = new FrameExtractionOptions();
        var timeLimit = TimeSpan.FromSeconds(10);

        // Act
        options.TimeLimit = timeLimit;

        // Assert
        Assert.Equal(timeLimit, options.TimeLimit);
    }

    [Fact]
    public void Fps_CanBeSet()
    {
        // Arrange
        var options = new FrameExtractionOptions();
        var fps = 30.0;

        // Act
        options.Fps = fps;

        // Assert
        Assert.Equal(fps, options.Fps);
    }

    [Fact]
    public void EnableHardwareAcceleration_CanBeSet()
    {
        // Arrange
        var options = new FrameExtractionOptions();

        // Act
        options.EnableHardwareAcceleration = true;

        // Assert
        Assert.True(options.EnableHardwareAcceleration);
    }
}
