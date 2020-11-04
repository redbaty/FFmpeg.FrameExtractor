using System;

namespace FrameExtractor
{
    public class FFmpegException : Exception
    {
        public FFmpegException(string message) : base(message)
        {
        }
    }
}