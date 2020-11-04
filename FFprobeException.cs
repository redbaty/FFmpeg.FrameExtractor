using System;

namespace FrameExtractor
{
    public class FFprobeException : Exception
    {
        public FFprobeException(string message) : base(message)
        {
        }
    }
}