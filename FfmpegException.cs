using System;

namespace FFmpeg.FrameExtractor
{
    public class FfmpegException : Exception
    {
        public FfmpegException(string message) : base(message)
        {
        }
    }
}