using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public partial class FrameExtractionService
    {
        public interface IFFmegInput
        {
            string GetInputArgument();
            
            Stream? GetStream();
        }
        
        private class FileFFmpegInput(string filePath) : IFFmegInput
        {
            public string GetInputArgument()
            {
                return filePath;
            }

            public Stream? GetStream()
            {
                return null;
            }
        }

        public class StreamFFmpegInput(Stream stream) : IFFmegInput
        {
            public Stream Stream { get; } = stream;
            
            public string GetInputArgument()
            {
                return "-";
            }

            public Stream GetStream()
            {
                return Stream;
            }
        }
        
        
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
        /// <param name="onDurationUpdate">Called with the duration value extracted from ffmpeg 'stderr' output.</param>
        /// <returns></returns>
        /// <exception cref="FFmpegException">This happens when FFmpeg returns a non 0 exit code.</exception>
        public async IAsyncEnumerable<Frame> GetFrames(string filePath, [EnumeratorCancellation] CancellationToken cancellationToken = default(CancellationToken), FrameExtractionOptions? options = null, Action<double>? onFpsGathered = null, Action<TimeSpan, TimeSpan>? onDurationUpdate = null)
        {
            var ffmpegFileInput = new FileFFmpegInput(filePath);
            await foreach (var frame in GetFrames(ffmpegFileInput, cancellationToken, options, onFpsGathered, onDurationUpdate)) yield return frame;
        }

        private async Task<TimeSpan> GetFileDuration(IFFmegInput input)
        {
            var agumentsList = new List<string>
            {
                "-i",
                input.GetInputArgument(),
                "-show_entries",
                "format=duration",
                "-v",
                "quiet",
                "-sexagesimal",
                "-of",
                "csv=\"p=0\""
            };

            var baseCommand = Cli.Wrap("ffprobe");
            
            if (input.GetStream() is { } standardInput)
            {
                baseCommand = baseCommand.WithStandardInputPipe(PipeSource.FromStream(standardInput));
            }
            
            var command = baseCommand
                .WithArguments(agumentsList);
            
            var commandResult = await command.ExecuteBufferedAsync();
            var duration = TimeSpan.Parse(commandResult.StandardOutput, CultureInfo.InvariantCulture);

            return duration;
        }

        /// <summary>
        ///     Gets frames asynchronously from a video stream.
        /// </summary>
        /// <param name="ffmegInput">The video file input</param>
        /// <param name="cancellationToken"></param>
        /// <param name="options"></param>
        /// <param name="onFpsGathered">
        ///     After the video parsing is done, this will be called with the fps value extracted from
        ///     ffmpeg 'stderr' output.
        /// </param>
        /// <param name="onDurationUpdate">
        ///     While the video is being parsed, this will be called with the duration value extracted from
        ///     ffmpeg 'stderr' output.
        /// </param>
        /// <returns></returns>
        /// <exception cref="FFmpegException">This happens when FFmpeg returns a non 0 exit code.</exception>
        public async IAsyncEnumerable<Frame> GetFrames(IFFmegInput ffmegInput, [EnumeratorCancellation] CancellationToken cancellationToken = default(CancellationToken), FrameExtractionOptions? options = null, 
            Action<double>? onFpsGathered = null, Action<TimeSpan, TimeSpan>? onDurationUpdate = null
        )
        {
            options ??= FrameExtractionOptions.Default;
            TimeSpan? duration = null;

            if (onDurationUpdate != null)
            {
                duration = await GetFileDuration(ffmegInput);
                onDurationUpdate(duration.Value, TimeSpan.Zero);

                if (ffmegInput is StreamFFmpegInput streamFFmpegInput)
                {
                    if(!streamFFmpegInput.Stream.CanSeek)
                        throw new InvalidOperationException("Stream must be seekable to get the duration");
                    
                    streamFFmpegInput.Stream.Seek(0, SeekOrigin.Begin);
                }
            }

            var argumentsList = new List<string>
            {
                "-i",
                ffmegInput.GetInputArgument(),
                "-an"
            };


            if (options.FrameSize is { Valid: true })
                argumentsList.Add(
                    $"-s {options.FrameSize.Width}x{options.FrameSize.Height}");

            if (options.TimeLimit.HasValue) argumentsList.Add($"-t {options.TimeLimit.Value:hh\\:mm\\:ss\\.fff}");

            if (options.Fps.HasValue)
                argumentsList.Add($"-r {options.Fps}");

            if (options.AdditionalInputArguments != null)
            {
                argumentsList.AddRange(options.AdditionalInputArguments);
            }
            
            argumentsList.Add($"-vcodec {options.FrameFormat.GetVcodec()}");
            argumentsList.Add("-f image2pipe");
            argumentsList.Add("-");

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

            var baseCommand = Cli.Wrap(options.FFmpegBinaryPath);

            if (ffmegInput.GetStream() is { } standardInput)
            {
                baseCommand = baseCommand.WithStandardInputPipe(PipeSource.FromStream(standardInput));
            }
            
            var command = baseCommand
                .WithStandardOutputPipe(PipeTarget.ToStream(standardOutput))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(l =>
                {
                    Debug.WriteLine(l);
                    
                    if (onDurationUpdate != null && duration != null && GetCurrentDurationFromOutput(l) is {} currentDuration)
                    {
                        onDurationUpdate(duration.Value, currentDuration);
                    }

                    standardErrorOutput.AppendLine(l);
                }))
                .WithArguments(arguments);
            
            var taskResult = command
                .ExecuteAsync(cancellationToken)
                .Task.ContinueWith(t =>
                {
                    channel.Writer.Complete();
                    return t.Result;
                }, cancellationToken);

            var currentFrame = 1;
            await foreach (var frame in channel.Reader.ReadAllAsync(cancellationToken))
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

        private static TimeSpan? GetCurrentDurationFromOutput(string line)
        {
            var durationMatch = CurrentTimeRegex().Match(line);
            return TimeSpan.TryParse(durationMatch.Groups["currentDuration"].Value, out var currentDuration)
                ? currentDuration
                : null;
        }

        private static double GetFpsFromOutput(string stdOut)
        {
            var fpsMatch = CurrentFpsRegex().Match(stdOut);
            var fpsDigits = new string(fpsMatch.Value.Where(i => char.IsDigit(i) || i == '.').ToArray());
            var fps = double.Parse(fpsDigits, CultureInfo.InvariantCulture);
            return fps;
        }

        [GeneratedRegex(@"time=(?<currentDuration>\d*:\d*:\d*\.\d*)")]
        private static partial Regex CurrentTimeRegex();
        
        [GeneratedRegex(@"\d*\.?\d* fps")]
        private static partial Regex CurrentFpsRegex();
    }
}