using System;

namespace FrameExtractor.Exceptions
{
    public class FFmpegException : Exception
    {
        public FFmpegException(string message) : base(message)
        {
        }
    }
}