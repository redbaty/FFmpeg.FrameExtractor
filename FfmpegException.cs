using System;

namespace FrameExtractor
{
    public class FfmpegException : Exception
    {
        public FfmpegException(string message) : base(message)
        {
        }
    }
}