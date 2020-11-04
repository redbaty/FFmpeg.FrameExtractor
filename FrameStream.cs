using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FFmpeg.FrameExtractor.Decoders;

namespace FFmpeg.FrameExtractor
{
    internal class FrameStream : Stream
    {
        public FrameStream(IFrameBufferDecoder decoder)
        {
            Decoder = decoder;
        }


        public override bool CanRead { get; } = false;
        public override bool CanSeek { get; } = false;
        public override bool CanWrite { get; } = true;
        public override long Length { get; } = 0;
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
            return Decoder.WriteAsync(buffer[..count]);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotSupportedException();
        }


        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}