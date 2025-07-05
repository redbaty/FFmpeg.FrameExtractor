using System;

namespace FrameExtractor.Exceptions
{
    public class FFprobeException : Exception
    {
        public FFprobeException(string message) : base(message)
        {
        }
    }
}