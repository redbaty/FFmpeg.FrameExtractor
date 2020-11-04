using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using FrameExtractor.Decoders;

namespace FrameExtractor
{
    public static class FFmpeg
    {
        public static async Task<double> GetFps(string filePath)
        {
            var ffprobeResult = await Cli.Wrap("ffprobe")
                .WithArguments($"\"{filePath}\"")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

            if (ffprobeResult.ExitCode != 0)
            {
                throw new FFprobeException(ffprobeResult.StandardError);
            }

            return GetFpsFromOutput(string.IsNullOrEmpty(ffprobeResult.StandardOutput)
                ? ffprobeResult.StandardError
                : ffprobeResult.StandardOutput);
        }

        public static async IAsyncEnumerable<Frame> GetFrames(string filePath,
            FrameExtractionOptions frameExtractionOptions,
            TimeSpan? timeLimit,
            Action<double> onFpsGathered = null
        )
        {
            var argumentsList = new List<string>();

            if (timeLimit.HasValue) argumentsList.Add($"-t {timeLimit.Value:hh\\:mm\\:ss\\.fff}");

            if (frameExtractionOptions.EnableHardwareAcceleration) argumentsList.Add("-hwaccel auto");

            argumentsList.Add("-i - -an");

            if (frameExtractionOptions.FrameSize.Valid)
            {
                argumentsList.Add(
                    $"-s {frameExtractionOptions.FrameSize.Width}x{frameExtractionOptions.FrameSize.Height}");
            }

            argumentsList.Add("-f image2pipe");
            argumentsList.Add("pipe:.jpg");

            var arguments = argumentsList.Aggregate((x, y) => $"{x} {y}");
            var channel = Channel.CreateUnbounded<Frame>();
            await using var standardInput = File.OpenRead(filePath);
            await using var standardOutput = new FrameStream(new JpegBufferDecoder(channel));
            var standardErrorOutput = new StringBuilder();

            var taskResult = Cli.Wrap("ffmpeg")
                .WithStandardInputPipe(PipeSource.FromStream(standardInput))
                .WithStandardOutputPipe(PipeTarget.ToStream(standardOutput))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(standardErrorOutput))
                .WithArguments(arguments)
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync()
                .Task.ContinueWith(t =>
                {
                    channel.Writer.Complete();
                    return t.Result;
                });

            await foreach (var frame in channel.Reader.ReadAllAsync())
            {
                yield return frame;
            }

            var result = await taskResult;
            if (result.ExitCode != 0) throw new FFmpegException(standardErrorOutput.ToString());

            if (onFpsGathered != null)
            {
                var fps = GetFpsFromOutput(standardErrorOutput.ToString());
                onFpsGathered.Invoke(fps);
            }
        }

        private static double GetFpsFromOutput(string stdOut)
        {
            var fpsMatch = Regex.Match(stdOut, "\\d*\\.?\\d* fps");
            var fpsDigits = new string(fpsMatch.Value.Where(i => char.IsDigit(i) || i == '.').ToArray());
            var fps = double.Parse(fpsDigits, CultureInfo.InvariantCulture);
            return fps;
        }
    }
}