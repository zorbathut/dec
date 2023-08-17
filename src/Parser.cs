namespace Dec
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Xml.Linq;

    // This exists solely to ensure that I don't accidentally mess up the interfaces.
    internal interface IParser
    {
        void AddDirectory(string directory);
        void AddFile(Parser.FileType fileType, string filename, string identifier = null);
        void AddString(Parser.FileType fileType, string contents, string identifier = "(unnamed)");
        void AddStream(Parser.FileType fileType, Stream stream, string identifier = "(unnamed)");
    }

    /// <summary>
    /// Handles all parsing and initialization of dec structures.
    ///
    /// Intended for prototype or non-moddable games; use ParserModdable for mod support.
    /// </summary>
    public class Parser : IParser
    {
        // That's right! I'm actually a ParserModdable on the inside! Evil cackle!
        private ParserModdable parserModdable;

        public enum FileType
        {
            Xml,
        }

        public Parser()
        {
            parserModdable = new ParserModdable();
        }

        /// <summary>
        /// Pass a directory in for recursive processing.
        /// </summary>
        /// <remarks>
        /// This function will ignore dot-prefixed directory names and files, which are common for development tools to create.
        /// </remarks>
        /// <param name="directory">The directory to look for files in.</param>
        public void AddDirectory(string directory)
        {
            parserModdable.AddDirectory(directory);
        }

        /// <summary>
        /// Pass a file in for processing.
        /// </summary>
        /// <param name="stringName">A human-readable identifier useful for debugging. Generally, the name of the file that the string was read from. Not required; will be derived from filename automatically.</param>
        public void AddFile(Parser.FileType fileType, string filename, string identifier = null)
        {
            parserModdable.AddFile(fileType, filename, identifier);
        }

        /// <summary>
        /// Pass a string in for processing.
        /// </summary>
        /// <param name="identifier">A human-readable identifier useful for debugging. Generally, the name of the file that the string was built from. Not required, but helpful.</param>
        public void AddString(Parser.FileType fileType, string contents, string identifier = "(unnamed)")
        {
            parserModdable.AddString(fileType, contents, identifier);
        }

        /// <summary>
        /// Pass a stream in for processing.
        /// </summary>
        /// <param name="identifier">A human-readable identifier useful for debugging. Generally, the name of the file that the stream was built from. Not required; will be derived from filename automatically</param>
        public void AddStream(Parser.FileType fileType, Stream stream, string identifier = "(unnamed)")
        {
            parserModdable.AddStream(fileType, stream, identifier);
        }

        /// <summary>
        /// Finish all parsing.
        /// </summary>
        public void Finish()
        {
            parserModdable.Finish();
        }
    }
}