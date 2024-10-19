using FrameExtractor.Annotations;

namespace FrameExtractor
{
    public enum FrameFormat
    {
        [Vcodec("jpg")] Jpg,

        [Vcodec("png")] Png
    }
}