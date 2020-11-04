namespace FrameExtractor
{
    public class FrameExtractionOptions
    {
        internal static FrameExtractionOptions Default { get; } = new FrameExtractionOptions();

        public bool EnableHardwareAcceleration { get; set; }

        public bool DumpFiles { get; set; }

        public FrameSize FrameSize { get; set; } = new FrameSize
        {
            Height = 360,
            Width = 640
        };
    }
}