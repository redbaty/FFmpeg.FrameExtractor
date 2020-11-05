using System.Threading.Channels;

namespace FrameExtractor.Decoders
{
    internal class JpegBufferDecoder : SignatureBasedDecoder
    {
        private static byte[] StartSignature { get; } = {255, 216, 255};
        private static byte[] EndSignature { get; } = {255, 217};

        public JpegBufferDecoder(ChannelWriter<Frame> channelWriter) : base(StartSignature, EndSignature, channelWriter)
        {
        }
    }
}