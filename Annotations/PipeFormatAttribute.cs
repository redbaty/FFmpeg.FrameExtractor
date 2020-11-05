using System;

namespace FrameExtractor.Annotations
{
    internal class PipeFormatAttribute : Attribute
    {
        public PipeFormatAttribute(string format)
        {
            Format = format;
        }

        internal string Format { get; }
    }
}