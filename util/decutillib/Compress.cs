
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace DecUtilLib
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static class Compress
    {
        public static void WriteToFile(string filename, string data)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var compressor = new ZstdNet.Compressor(new ZstdNet.CompressionOptions(20));
                File.WriteAllBytes(filename + ".zst", compressor.Wrap(System.Text.Encoding.UTF8.GetBytes(data)));
            }
            else
            {
                // Annoyingly this doesn't currently work on Linux.
                // TODO: come up with a better solution.
                File.WriteAllBytes(filename, System.Text.Encoding.UTF8.GetBytes(data));

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "zstd";
                startInfo.Arguments = filename;
                var process = Process.Start(startInfo);
                process.WaitForExit();
            }
        }

        public static string ReadFromFile(string filename)
        {
            if (File.Exists(filename))
            {
                return File.ReadAllText(filename);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var decompressor = new ZstdNet.Decompressor();
                return System.Text.Encoding.UTF8.GetString(decompressor.Unwrap(File.ReadAllBytes(filename + ".zst")));
            }
            else
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "unzstd";
                startInfo.Arguments = filename + ".zst";
                var process = Process.Start(startInfo);
                process.WaitForExit();

                return File.ReadAllText(filename);
            }
        }
    }
}
