using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using FrameExtractor.Decoders;
using FrameExtractor.Exceptions;
using FrameExtractor.Extensions;
using Microsoft.Extensions.Logging;

namespace FrameExtractor
{
    public class FrameExtractionService
    {
        public static FrameExtractionService Default { get; } = new(null);

        public FrameExtractionService(ILogger<FrameExtractionService>? logger)
        {
            Logger = logger;
        }

        private ILogger<FrameExtractionService>? Logger { get; }

        /// <summary>
        ///     Returns the 'FPS' returned from FFprobe.
        /// </summary>
        /// <param name="filePath">The full file path.</param>
        /// <param name="options"></param>
        /// <returns></returns>
        /// <exception cref="FFprobeException">This happens when FFprobe returns a non 0 exit code.</exception>
        public async Task<double> GetFps(string filePath, FFmpegOptions? options = null)
        {
            options ??= FFmpegOptions.Default;

            Logger?.LogDebug("Getting fps from FFprobe. {@Arguments}", new
            {
                FFprobePath = options.FFmpegBinaryPath,
                File = filePath
            });

            var ffprobeResult = await Cli.Wrap(options.FFprobeBinaryPath)
                .WithArguments($"\"{filePath}\"")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

            if (ffprobeResult.ExitCode != 0)
            {
                var ffprobeException = new FFprobeException(ffprobeResult.StandardError);
                Logger?.LogError(ffprobeException, "Error while running FFprobe on '{@File}'", filePath);
                throw ffprobeException;
            }

            Logger?.LogInformation("FFprobe ran successfully on '{@File}'", filePath);

            return GetFpsFromOutput(string.IsNullOrEmpty(ffprobeResult.StandardOutput)
                ? ffprobeResult.StandardError
                : ffprobeResult.StandardOutput);
        }

        /// <summary>
        ///     Gets frames asynchronously from a video file.
        /// </summary>
        /// <param name="filePath">The full file path.</param>
        /// <param name="cancellationToken"></param>
        /// <param name="options"></param>
        /// <param name="onFpsGathered">
        ///     After the video parsing is done, this will be called with the fps value extracted from
        ///     ffmpeg 'stderr' output.
        /// </param>
        /// <returns></returns>
        /// <exception cref="FFmpegException">This happens when FFmpeg returns a non 0 exit code.</exception>
        public async IAsyncEnumerable<Frame> GetFrames(string filePath, [EnumeratorCancellation] CancellationToken cancellationToken = default(CancellationToken), FrameExtractionOptions? options = null, Action<double>? onFpsGathered = null)
        {
            await using var standardInput = File.OpenRead(filePath);
            await foreach (var frame in GetFrames(standardInput, cancellationToken, options, onFpsGathered)) yield return frame;
        }

        /// <summary>
        ///     Gets frames asynchronously from a video stream.
        /// </summary>
        /// <param name="standardInput">The video stream.</param>
        /// <param name="cancellationToken"></param>
        /// <param name="options"></param>
        /// <param name="onFpsGathered">
        ///     After the video parsing is done, this will be called with the fps value extracted from
        ///     ffmpeg 'stderr' output.
        /// </param>
        /// <returns></returns>
        /// <exception cref="FFmpegException">This happens when FFmpeg returns a non 0 exit code.</exception>
        public async IAsyncEnumerable<Frame> GetFrames(Stream standardInput, [EnumeratorCancellation] CancellationToken cancellationToken = default(CancellationToken), FrameExtractionOptions? options = null, Action<double>? onFpsGathered = null)
        {
            options ??= FrameExtractionOptions.Default;

            var argumentsList = new List<string> { "-i - -an" };


            if (options.FrameSize is { Valid: true })
                argumentsList.Add(
                    $"-s {options.FrameSize.Width}x{options.FrameSize.Height}");

            if (options.TimeLimit.HasValue) argumentsList.Add($"-t {options.TimeLimit.Value:hh\\:mm\\:ss\\.fff}");

            if (options.Fps.HasValue)
                argumentsList.Add($"-r 320:-1");

            argumentsList.Add("-f image2pipe");
            argumentsList.Add($"pipe:{options.FrameFormat.GetPipeFormat()}");

            var arguments = argumentsList.Aggregate((x, y) => $"{x} {y}");
            var channel = Channel.CreateUnbounded<FrameData>();
            await using var standardOutput =
                new DecoderStreamWrapper(options.FrameFormat.GetDecoder(channel.Writer));
            var standardErrorOutput = new StringBuilder();

            Logger?.LogInformation("Starting FFmpeg. {@Arguments}", new
            {
                Arguments = arguments,
                FFmpegPath = options.FFmpegBinaryPath
            });

            var taskResult = Cli.Wrap(options.FFmpegBinaryPath)
                .WithStandardInputPipe(PipeSource.FromStream(standardInput))
                .WithStandardOutputPipe(PipeTarget.ToStream(standardOutput))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(standardErrorOutput))
                .WithArguments(arguments)
                .ExecuteAsync(cancellationToken)
                .Task.ContinueWith(t =>
                {
                    channel.Writer.Complete();
                    return t.Result;
                }, cancellationToken);

            var currentFrame = 1;
            await foreach (var frame in channel.Reader.ReadAllAsync())
            {
                Logger?.LogDebug("Frame {@Frame} delivered", currentFrame);
                yield return new Frame(frame.Data, currentFrame, options);
                currentFrame++;
            }

            var result = await taskResult;
            if (result.ExitCode != 0)
            {
                var ffmpegException = new FFmpegException(standardErrorOutput.ToString());
                Logger?.LogError(ffmpegException, "Error while running FFmpeg");
                throw ffmpegException;
            }

            Logger?.LogInformation("FFmpeg ran successfully. {@FrameCount} frames delivered, took {@ElapsedTime}", currentFrame, result.RunTime);

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