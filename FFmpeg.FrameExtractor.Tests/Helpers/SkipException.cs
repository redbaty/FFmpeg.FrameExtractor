namespace FFmpeg.FrameExtractor.Tests.Helpers;

/// <summary>
/// Exception that causes a test to be skipped when thrown
/// </summary>
public class SkipException : Exception
{
    public SkipException(string message) : base(message)
    {
    }

    public SkipException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
