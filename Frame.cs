namespace FrameExtractor
{
    public class Frame
    {
        public Frame(byte[] data)
        {
            Data = data;
        }

        public byte[] Data { get; }

        public int Position { get; internal set; }
        
        public FrameExtractionOptions Options { get; internal set; }
    }
}