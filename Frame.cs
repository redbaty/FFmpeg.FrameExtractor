namespace FrameExtractor
{
    public record FrameData(byte[] Data);

    public record Frame(byte[] Data, int Position, FrameExtractionOptions Options) : FrameData(Data);
}