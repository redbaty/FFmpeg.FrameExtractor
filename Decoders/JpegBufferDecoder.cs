using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FFmpeg.FrameExtractor.Decoders
{
    internal class JpegBufferDecoder : IFrameBufferDecoder
    {
        public JpegBufferDecoder(ChannelWriter<Frame> channelWriter)
        {
            ChannelWriter = channelWriter;
        }

        private int CurrentFrame { get; set; }
        private List<byte> LastBuffer { get; } = new List<byte>();
        private static byte[] StartSignature { get; } = {255, 216, 255, 224};
        private static byte[] EndSignature { get; } = {255, 217};

        public ChannelWriter<Frame> ChannelWriter { get; }

        private bool FrameStarted { get; set; }

        private int? PositionStart { get; set; }
        private int? PositionEnd { get; set; }

        public async Task WriteAsync(byte[] buffer)
        {
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
                            await CreateImage(bytes);
                            PositionStart = null;
                        }
                        else
                        {
                            if (LastBuffer.Count > 0)
                            {
                                var array = LastBuffer.Concat(buffer[..PositionEnd.Value]).ToArray();
                                await CreateImage(array);
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

                if (delta > 1)
                {
                    LastBuffer.AddRange(buffer[PositionEnd.Value..]);
                }
            }

            PositionStart = null;
            PositionEnd = null;
        }

        private async Task CreateImage(byte[] data)
        {
#if DEBUG
            var malformed = false;

            for (var index = 0; index < StartSignature.Length; index++)
            {
                if (StartSignature[index] != data[index])
                {
                    malformed = true;
                }
            }

            var dataIndex = 0;
            for (var index = EndSignature.Length - 1; index >= 0; index--)
            {
                if (EndSignature[index] != data[data.Length - 1 - dataIndex])
                {
                    malformed = true;
                }

                dataIndex++;
            }

            if (malformed)
            {
                Console.WriteLine("Malformed frame.");
                throw new InvalidOperationException("Malformed frame.");
            }
#endif

            await ChannelWriter.WriteAsync(new Frame(data, CurrentFrame));
            CurrentFrame++;
        }
    }
}