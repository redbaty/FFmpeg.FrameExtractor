using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FrameExtractor.Decoders
{
    internal interface IFrameBufferDecoder
    {
        ChannelWriter<FrameData> ChannelWriter { get; }
        
        Task WriteAsync(byte[] buffer, CancellationToken cancellationToken);
    }
}