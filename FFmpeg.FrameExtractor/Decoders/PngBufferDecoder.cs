using System.Threading.Channels;

namespace FrameExtractor.Decoders
{
    internal class PngBufferDecoder : SignatureBasedDecoder
    {
        private static byte[] StartSignature { get; } = {137, 80, 78, 71, 13, 10, 26, 10};
        private static byte[] EndSignature { get; } = {73, 69, 78, 68, 174, 66, 96, 130};

        public PngBufferDecoder(ChannelWriter<FrameData> channelWriter) : base(StartSignature, EndSignature, channelWriter)
        {
        }
    }
}