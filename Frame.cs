namespace FrameExtractor
{
    public class Frame
    {
        public Frame(byte[] data)
        {
            Data = data;
            Position = position;
        }

        public byte[] Data { get; }

        public int Position { get; internal set; }
    }
}