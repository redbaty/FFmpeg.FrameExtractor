using System;

namespace FrameExtractor.Annotations
{
    [AttributeUsage(AttributeTargets.Field)]
    internal class VcodecAttribute : Attribute
    {
        public VcodecAttribute(string format)
        {
            Format = format;
        }

        internal string Format { get; }
    }
}