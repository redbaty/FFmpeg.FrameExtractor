using System.IO;
using System.Runtime.InteropServices;

namespace FrameExtractor
{
    public class FFmpegOptions
    {
        internal static FFmpegOptions Default { get; } = new FFmpegOptions();

        private string _ffmpegBinaryPath;
        private string _ffprobeBinaryPath;

        public string FFmpegBinaryPath
        {
            get => _ffmpegBinaryPath ?? GetPathFromEnvironmentOrThrow("ffmpeg");
            set => _ffmpegBinaryPath = value;
        }

        public string FFprobeBinaryPath
        {
            get => _ffprobeBinaryPath ?? GetPathFromEnvironmentOrThrow("ffprobe");
            set => _ffprobeBinaryPath = value;
        }

        private static string GetPathFromEnvironmentOrThrow(string fileName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !fileName.EndsWith(".exe"))
            {
                fileName += ".exe";
            }
            
            if (EnvironmentHelper.ExistsOnPath(fileName))
            {
                return EnvironmentHelper.GetFullPath(fileName);
            }

            throw new FileNotFoundException(
                $"Could not find {fileName} in PATH. Make sure to set it manually or to include it on your system PATH.");
        }
    }
}