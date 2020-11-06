# FFmpeg - Frame Extractor
Extracts frames from FFmpeg output pipe.

#### Features
* No direct FFmpeg library dependency. - Frames are extracted directly from the binary stdOut, this usually means less compatibility problems.
* Compressed image formats. - JPG and PNG are currently supported, this means less memory usage when compared to raw image formats (such as Bgr24).

# Usage
```csharp
var ffmpeg = FrameExtractionService.Default; /* Gets the default FrameExtractionService instance. (This has no logging capabilities) */
await foreach (var frame in ffmpeg.GetFrames(filePath))
{
    await File.WriteAllBytesAsync($"frame-{frame.Position:0000}.jpg", frame.Data);
}
```

# Notes
* By default FFmpeg is served from the environment's PATH. A custom file can be specified in the `FrameExtractionOptions` class.