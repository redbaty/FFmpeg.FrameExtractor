using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FrameExtractor.Decoders
{
    internal class DecoderStreamWrapper : Stream
    {
        public DecoderStreamWrapper(IFrameBufferDecoder decoder)
        {
            Decoder = decoder;
        }


        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => 0;
        public override long Position { get; set; }
        private IFrameBufferDecoder Decoder { get; }


        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return Decoder.WriteAsync(buffer[..count], cancellationToken);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = new())
        {
            await Decoder.WriteAsync(buffer.ToArray(), cancellationToken);
        }


        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}