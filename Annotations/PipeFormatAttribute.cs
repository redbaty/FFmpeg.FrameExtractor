using System;

namespace FrameExtractor.Annotations
{
    [AttributeUsage(AttributeTargets.Field)]
    internal class PipeFormatAttribute : Attribute
    {
        public PipeFormatAttribute(string format)
        {
            Format = format;
        }

        internal string Format { get; }
    }
}