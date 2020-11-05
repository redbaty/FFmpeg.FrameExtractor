using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FrameExtractor.Decoders
{
    internal class SignatureBasedDecoder : IFrameBufferDecoder
    {
        protected SignatureBasedDecoder(byte[] startSignature, byte[] endSignature, ChannelWriter<Frame> channelWriter)
        {
            StartSignature = startSignature;
            EndSignature = endSignature;
            ChannelWriter = channelWriter;
        }

        private int CurrentFrame { get; set; }
        private List<byte> LastBuffer { get; } = new List<byte>();
        private byte[] StartSignature { get; }
        private byte[] EndSignature { get; }
        private bool FrameStarted { get; set; }
        private int? PositionStart { get; set; }
        private int? PositionEnd { get; set; }
        public ChannelWriter<Frame> ChannelWriter { get; }

        public virtual async Task WriteAsync(byte[] buffer)
        {
#if DEBUG
            var packageReadingTime = Stopwatch.StartNew();
#endif
            var currentStartSignaturePosition = 0;
            var currentEndSignaturePosition = 0;
            var writingTasks = new List<Task>();

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
                            writingTasks.Add(CreateImage(bytes, CurrentFrame));
                            CurrentFrame++;
                            PositionStart = null;
                        }
                        else
                        {
                            if (LastBuffer.Count > 0)
                            {
                                var array = LastBuffer.Concat(buffer[..PositionEnd.Value]).ToArray();
                                writingTasks.Add(CreateImage(array, CurrentFrame));
                                CurrentFrame++;
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
            await Task.WhenAll(writingTasks);

#if DEBUG
            packageReadingTime.Stop();
            if (packageReadingTime.ElapsedMilliseconds > 0)
                Debug.WriteLine($"Package reading took {packageReadingTime.ElapsedMilliseconds}ms.");
#endif
        }

        private async Task CreateImage(byte[] data, int frame)
        {
            await ChannelWriter.WriteAsync(new Frame(data, frame));
        }
    }
}