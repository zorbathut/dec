using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Dec
{
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
        private ParserModular parserModdable;

        private ParserModular.Module coreModule;

        /// <summary>
        /// Indicates which file type the input should be parsed as.
        /// </summary>
        public enum FileType
        {
            Xml,
        }

        public Parser(Recorder.IUserSettings userSettings = null)
        {
            parserModdable = new ParserModular(userSettings);
            coreModule = parserModdable.CreateModule("core");
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
            coreModule.AddDirectory(directory);
        }

        /// <summary>
        /// Pass a file in for processing.
        /// </summary>
        /// <param name="stringName">A human-readable identifier useful for debugging. Generally, the name of the file that the string was read from. Not required; will be derived from filename automatically.</param>
        public void AddFile(Parser.FileType fileType, string filename, string identifier = null)
        {
            coreModule.AddFile(fileType, filename, identifier);
        }

        /// <summary>
        /// Pass a string in for processing.
        /// </summary>
        /// <param name="identifier">A human-readable identifier useful for debugging. Generally, the name of the file that the string was built from. Not required, but helpful.</param>
        public void AddString(Parser.FileType fileType, string contents, string identifier = "(unnamed)")
        {
            coreModule.AddString(fileType, contents, identifier);
        }

        /// <summary>
        /// Pass a stream in for processing.
        /// </summary>
        /// <param name="identifier">A human-readable identifier useful for debugging. Generally, the name of the file that the stream was built from. Not required; will be derived from filename automatically</param>
        public void AddStream(Parser.FileType fileType, Stream stream, string identifier = "(unnamed)")
        {
            coreModule.AddStream(fileType, stream, identifier);
        }

        /// <summary>
        /// Finish all parsing.
        /// </summary>
        /// <remarks>
        /// The `dependencies` parameter can be used to feed in dependencies for the PostLoad function.
        /// This is a placeholder and is probably going to be replaced at some point, though only with something more capable.
        /// </remarks>
        public void Finish()
        {
            parserModdable.Finish();
        }
    }
}