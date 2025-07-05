using System.IO;
using System.Runtime.InteropServices;

namespace FrameExtractor
{
    public class FFmpegOptions
    {
        internal static FFmpegOptions Default { get; } = new();

        public string FFmpegBinaryPath { get; set; } = GetPathFromEnvironmentOrThrow("ffmpeg");

        public string FFprobeBinaryPath { get; set; } = GetPathFromEnvironmentOrThrow("ffprobe");

        private static string GetPathFromEnvironmentOrThrow(string fileName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !fileName.EndsWith(".exe"))
            {
                fileName += ".exe";
            }
            
            var fullPath = EnvironmentHelper.GetFullPath(fileName);
            return !string.IsNullOrEmpty(fullPath)
                ? fullPath
                : throw new FileNotFoundException(
                    $"Could not find {fileName} in PATH. Make sure to set it manually or to include it on your system PATH.");
        }
    }
}