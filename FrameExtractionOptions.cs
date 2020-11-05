using System;

namespace FrameExtractor
{
    public class FrameExtractionOptions : FFmpegOptions
    {
        internal new static FrameExtractionOptions Default { get; } = new FrameExtractionOptions();

        public bool EnableHardwareAcceleration { get; set; }

        public FrameFormat FrameFormat { get; set; } = FrameFormat.Jpg;

        public FrameSize FrameSize { get; set; }

        public TimeSpan? TimeLimit { get; set; }
    }
}