using FrameExtractor.Annotations;

namespace FrameExtractor
{
    public enum FrameFormat
    {
        [Vcodec("mjpeg")] Jpg,

        [Vcodec("png")] Png
    }
}