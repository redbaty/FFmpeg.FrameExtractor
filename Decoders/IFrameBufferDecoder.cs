using System.Threading.Channels;
using System.Threading.Tasks;

namespace FFmpeg.FrameExtractor.Decoders
{
    internal interface IFrameBufferDecoder
    {
        ChannelWriter<Frame> ChannelWriter { get; }
        
        Task WriteAsync(byte[] buffer);
    }
}