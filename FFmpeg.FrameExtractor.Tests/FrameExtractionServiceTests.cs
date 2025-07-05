using FrameExtractor;
using FrameExtractor.Exceptions;
using CliWrap;
using SixLabors.ImageSharp;

namespace FFmpeg.FrameExtractor.Tests;

public class FrameExtractionServiceTests : IAsyncLifetime
{
    private readonly FrameExtractionService _frameExtractionService;
    private readonly string _testVideoPath;
    private readonly string _tempDirectory;

    public FrameExtractionServiceTests()
    {
        _frameExtractionService = FrameExtractionService.Default;
        _tempDirectory = Path.Combine(Path.GetTempPath(), "FFmpegFrameExtractorTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        _testVideoPath = Path.Combine(_tempDirectory, "test_video.mp4");
    }

    public async Task InitializeAsync()
    {
        // Create a simple test video using FFmpeg
        await CreateTestVideo();
    }

    public async Task DisposeAsync()
    {
        await Task.Delay(500);
        
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    private async Task CreateTestVideo()
    {
        var command = Cli.Wrap("ffmpeg")
            .WithArguments([
                "-f", "lavfi",
                "-i", "testsrc=duration=2:size=320x240:rate=30",
                "-pix_fmt", "yuv420p",
                "-y", // Overwrite output file
                _testVideoPath
            ])
            .WithValidation(CommandResultValidation.None);
            
        await command.ExecuteAsync();
    }

    [Fact]
    public async Task GetFps_ValidVideo_ReturnsCorrectFps()
    {
        // Act
        var fps = await _frameExtractionService.GetFps(_testVideoPath);

        // Assert
        Assert.Equal(30.0, fps, 1); // Allow 1 fps tolerance
    }

    [Fact]
    public async Task GetFps_InvalidFile_ThrowsFFprobeException()
    {
        // Arrange
        var invalidPath = Path.Combine(_tempDirectory, "nonexistent.mp4");

        // Act & Assert
        await Assert.ThrowsAsync<FFprobeException>(() => _frameExtractionService.GetFps(invalidPath));
    }

    [Fact]
    public async Task GetFrames_ValidVideo_ReturnsFrames()
    {
        // Arrange
        var options = new FrameExtractionOptions
        {
            FrameFormat = FrameFormat.Jpg,
            TimeLimit = TimeSpan.FromSeconds(1) // Only extract first second
        };

        // Act
        var frames = new List<Frame>();
        await foreach (var frame in _frameExtractionService.GetFrames(_testVideoPath, CancellationToken.None, options))
        {
            frames.Add(frame);
        }

        // Assert
        Assert.NotEmpty(frames);
        Assert.All(frames, frame =>
        {
            Assert.NotNull(frame.Data);
            Assert.True(frame.Data.Length > 0);
            Assert.True(frame.Position >= 0);
        });
    }

    [Fact]
    public async Task GetFrames_WithPngFormat_ReturnsValidPngFrames()
    {
        // Arrange
        var options = new FrameExtractionOptions
        {
            FrameFormat = FrameFormat.Png,
            TimeLimit = TimeSpan.FromSeconds(0.5)
        };

        // Act
        var frames = new List<Frame>();
        await foreach (var frame in _frameExtractionService.GetFrames(_testVideoPath, CancellationToken.None, options))
        {
            frames.Add(frame);
            if (frames.Count >= 5) break; // Just get a few frames for testing
        }

        // Assert
        Assert.NotEmpty(frames);
        
        // Verify PNG signature for first frame
        var firstFrame = frames.First();
        Assert.True(firstFrame.Data.Length > 8);
        
        // PNG signature: 0x89 0x50 0x4E 0x47 0x0D 0x0A 0x1A 0x0A
        var pngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        for (int i = 0; i < pngSignature.Length; i++)
        {
            Assert.Equal(pngSignature[i], firstFrame.Data[i]);
        }
    }

    [Fact]
    public async Task GetFrames_WithFrameSize_ReturnsCorrectlySizedFrames()
    {
        // Arrange
        var options = new FrameExtractionOptions
        {
            FrameFormat = FrameFormat.Jpg,
            FrameSize = new FrameSize { Width = 160, Height = 120 },
            TimeLimit = TimeSpan.FromSeconds(0.5)
        };

        // Act
        var frames = new List<Frame>();
        await foreach (var frame in _frameExtractionService.GetFrames(_testVideoPath, CancellationToken.None, options))
        {
            frames.Add(frame);
            if (frames.Count >= 2) break;
        }

        // Assert
        Assert.NotEmpty(frames);
        
        // Verify frame size by loading the image
        using var memoryStream = new MemoryStream(frames.First().Data);
        using var image = await Image.LoadAsync(memoryStream);
        Assert.Equal(160, image.Width);
        Assert.Equal(120, image.Height);
    }

    [Fact]
    public async Task GetFrames_WithCancellation_StopsExecution()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var options = new FrameExtractionOptions();

        // Act
        var frameCount = 0;
        try
        {
            await foreach (var _ in _frameExtractionService.GetFrames(_testVideoPath, cancellationTokenSource.Token, options))
            {
                frameCount++;
                if (frameCount >= 10)
                {
                    await cancellationTokenSource.CancelAsync();
                }
            }
        }
        catch (OperationCanceledException e)
        {
            // Catching the exception to prevent test failure, we expect cancellation
            Assert.IsType<TaskCanceledException>(e);
        }

        // Assert
        Assert.True(frameCount >= 10, $"Expected at least 10 frames before cancellation, but got {frameCount}");
        Assert.True(frameCount < 60, $"Expected frame count to be less than 60 after cancellation, but got {frameCount}");
    }

    [Fact]
    public async Task GetFrames_WithFpsCallback_CallsCallback()
    {
        // Arrange
        double? capturedFps = null;
        var options = new FrameExtractionOptions
        {
            TimeLimit = TimeSpan.FromSeconds(0.5)
        };

        // Act
        await foreach (var _ in _frameExtractionService.GetFrames(_testVideoPath, CancellationToken.None, options, 
            fps => capturedFps = fps))
        {
            if(capturedFps != null)
                break;
        }

        // Assert
        Assert.NotNull(capturedFps);
        Assert.Equal(30.0, capturedFps.Value, 1);
    }

    [Fact]
    public async Task GetFrames_WithDurationCallback_CallsCallback()
    {
        // Arrange
        var durationUpdates = new List<(TimeSpan duration, TimeSpan current)>();
        var options = new FrameExtractionOptions
        {
            TimeLimit = TimeSpan.FromSeconds(0.5)
        };

        // Act
        var frames = new List<Frame>();
        await foreach (var frame in _frameExtractionService.GetFrames(_testVideoPath, CancellationToken.None, options,
            null, (duration, current) => durationUpdates.Add((duration, current))))
        {
            frames.Add(frame);
            if (frames.Count >= 5) break;
        }
        
        // Assert
        Assert.NotEmpty(durationUpdates);
        Assert.True(durationUpdates.First().duration.TotalSeconds > 1.5); // Should be around 2 seconds
    }

    [Fact]
    public async Task GetFrames_FromStream_ReturnsFrames()
    {
        // Arrange
        var videoBytes = await File.ReadAllBytesAsync(_testVideoPath);
        using var memoryStream = new MemoryStream(videoBytes);
        var streamInput = new FrameExtractionService.StreamFFmpegInput(memoryStream);
        
        var options = new FrameExtractionOptions
        {
            FrameFormat = FrameFormat.Jpg,
            TimeLimit = TimeSpan.FromSeconds(0.5)
        };

        // Act
        var frames = new List<Frame>();
        await foreach (var frame in _frameExtractionService.GetFrames(streamInput, CancellationToken.None, options))
        {
            frames.Add(frame);
            if (frames.Count >= 5) break;
        }

        // Assert
        Assert.NotEmpty(frames);
        Assert.All(frames, frame =>
        {
            Assert.NotNull(frame.Data);
            Assert.True(frame.Data.Length > 0);
        });
    }
}