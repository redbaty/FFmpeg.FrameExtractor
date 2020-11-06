# FFmpeg.FrameExtractor
Extracts frames from FFmpeg output pipe.

# Usage
```csharp
var ffmpeg = FrameExtractionService.Default; /* Gets the default FrameExtractionService instance. (This has no logging capabilities) */
await foreach (var frame in ffmpeg.GetFrames(filePath))
{
    await File.WriteAllBytesAsync($"frame-{frame.Position:0000}.jpg", frame.Data);
}
```
