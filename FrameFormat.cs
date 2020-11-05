using FrameExtractor.Annotations;

namespace FrameExtractor
{
    public enum FrameFormat
    {
        [PipeFormat(".jpg")] Jpg,

        [PipeFormat(".png")] Png
    }
}