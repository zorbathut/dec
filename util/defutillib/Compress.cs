using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace DefUtilLib
{
    public static class Compress
    {
        public static void WriteToFile(string filename, string data)
        {
            var compressor = new ZstdNet.Compressor(new ZstdNet.CompressionOptions(20));
            File.WriteAllBytes(filename + ".zst", compressor.Wrap(System.Text.Encoding.UTF8.GetBytes(data)));
        }

        public static string ReadFromFile(string filename)
        {
            if (File.Exists(filename))
            {
                return File.ReadAllText(filename);
            }

            var decompressor = new ZstdNet.Decompressor();
            return System.Text.Encoding.UTF8.GetString(decompressor.Unwrap(File.ReadAllBytes(filename + ".zst")));
        }
    }
}
