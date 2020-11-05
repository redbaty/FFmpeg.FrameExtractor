using System;
using System.IO;
using System.Linq;

namespace FrameExtractor
{
    internal class EnvironmentHelper
    {
        public static bool ExistsOnPath(string fileName)
        {
            return GetFullPath(fileName) != null;
        }

        public static string GetFullPath(string fileName)
        {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            var values = Environment.GetEnvironmentVariable("PATH");
            return values?.Split(Path.PathSeparator)
                .Select(path => Path.Combine(path, fileName))
                .FirstOrDefault(File.Exists);
        }
    }
}