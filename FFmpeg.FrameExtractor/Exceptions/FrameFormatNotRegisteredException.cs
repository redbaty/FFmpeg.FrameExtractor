using System;
using FrameExtractor.Annotations;

namespace FrameExtractor.Exceptions
{
    public class FrameFormatNotRegisteredException : Exception
    {
        public FrameFormatNotRegisteredException(FrameFormat frameFormat) : base(
            $"The format '{frameFormat}' has no '{nameof(VcodecAttribute)}' attached to it.")
        {
        }
    }
}