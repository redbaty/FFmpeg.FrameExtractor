using System;
using System.Threading.Channels;
using FrameExtractor.Annotations;
using FrameExtractor.Decoders;
using FrameExtractor.Exceptions;

namespace FrameExtractor.Extensions
{
    public static class FrameFormatExtensions
    {
        internal static IFrameBufferDecoder GetDecoder(this FrameFormat frameFormat, ChannelWriter<FrameData> channelWriter)
        {
            return frameFormat switch
            {
                FrameFormat.Jpg => new JpegBufferDecoder(channelWriter),
                FrameFormat.Png => new PngBufferDecoder(channelWriter),
                _ => throw new ArgumentOutOfRangeException(nameof(frameFormat), frameFormat, null)
            };
        }

        public static string GetPipeFormat(this FrameFormat frameFormat)
        {
            return frameFormat.GetAttributeOfType<PipeFormatAttribute>()?.Format ??
                   throw new FrameFormatNotRegisteredException(frameFormat);
        }

        private static T? GetAttributeOfType<T>(this Enum enumVal) where T : Attribute
        {
            var type = enumVal.GetType();
            var memInfo = type.GetMember(enumVal.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
            return (attributes.Length > 0) ? (T) attributes[0] : null;
        }
    }
}