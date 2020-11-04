namespace FFmpeg.FrameExtractor
{
    public class FrameSize
    {
        public int? Height { get; set; }

        public int? Width { get; set; }

        public bool Valid => Height == null && Width == null || Height.HasValue && Width.HasValue;
    }
}