using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FrameExtractor.Decoders
{
    internal class SignatureBasedDecoder : IFrameBufferDecoder
    {
        protected SignatureBasedDecoder(byte[] startSignature, byte[] endSignature, ChannelWriter<FrameData> channelWriter)
        {
            StartSignature = startSignature;
            EndSignature = endSignature;
            ChannelWriter = channelWriter;
        }
        
        private List<byte> LastBuffer { get; } = new();
        private byte[] StartSignature { get; }
        private byte[] EndSignature { get; }
        private bool FrameStarted { get; set; }
        private int? PositionStart { get; set; }
        private int? PositionEnd { get; set; }
        public ChannelWriter<FrameData> ChannelWriter { get; }

        public virtual async Task WriteAsync(byte[] buffer, CancellationToken cancellationToken)
        {
#if DEBUG
            var packageReadingTime = Stopwatch.StartNew();
#endif
            var currentStartSignaturePosition = 0;
            var currentEndSignaturePosition = 0;

            for (var index = 0; index < buffer.Length; index++)
            {
                var b = buffer[index];

                if (!FrameStarted && b == StartSignature[currentStartSignaturePosition])
                {
                    var length = StartSignature.Length - 1;
                    if (currentStartSignaturePosition == length)
                    {
                        FrameStarted = true;
                        PositionStart = index - length;
                        currentStartSignaturePosition = 0;
                    }
                    else
                    {
                        currentStartSignaturePosition++;
                    }
                }
                else
                {
                    currentStartSignaturePosition = 0;
                }

                if (FrameStarted && b == EndSignature[currentEndSignaturePosition])
                {
                    var length = EndSignature.Length - 1;
                    if (currentEndSignaturePosition == length)
                    {
                        PositionEnd = index + 1;

                        if (PositionStart.HasValue)
                        {
                            var bytes = buffer[PositionStart.Value..PositionEnd.Value];
                            await WriteImageToChannel(bytes, cancellationToken);
                            PositionStart = null;
                        }
                        else
                        {
                            if (LastBuffer.Count > 0)
                            {
                                var array = LastBuffer.Concat(buffer[..PositionEnd.Value]).ToArray();
                                await WriteImageToChannel(array, cancellationToken);
                                LastBuffer.Clear();
                            }
                        }

                        FrameStarted = false;
                        currentEndSignaturePosition = 0;
                    }
                    else
                    {
                        currentEndSignaturePosition++;
                    }
                }
                else
                {
                    currentEndSignaturePosition = 0;
                }
            }

            if (PositionEnd.HasValue)
            {
                var delta = buffer.Length - PositionEnd.Value;
                if (delta > 1) LastBuffer.AddRange(buffer[PositionEnd.Value..]);
            }
            else
            {
                LastBuffer.AddRange(buffer);
            }

            PositionStart = null;
            PositionEnd = null;

#if DEBUG
            packageReadingTime.Stop();
            if (packageReadingTime.ElapsedMilliseconds > 0)
                Debug.WriteLine($"Package reading took {packageReadingTime.ElapsedMilliseconds}ms.");
#endif
        }

        private ValueTask WriteImageToChannel(byte[] data, CancellationToken cancellationToken) => ChannelWriter.WriteAsync(new FrameData(data), cancellationToken);
    }
}