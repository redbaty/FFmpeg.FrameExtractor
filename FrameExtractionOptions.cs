﻿using System;
using System.Collections.Generic;

namespace FrameExtractor
{
    public class FrameExtractionOptions : FFmpegOptions
    {
        internal new static FrameExtractionOptions Default { get; } = new();

        public bool EnableHardwareAcceleration { get; set; }

        public FrameFormat FrameFormat { get; set; } = FrameFormat.Jpg;

        public FrameSize? FrameSize { get; set; }

        public TimeSpan? TimeLimit { get; set; }
        
        public double? Fps { get; set; }
        
        public ICollection<string>? AdditionalInputArguments { get; set; }
    }
}