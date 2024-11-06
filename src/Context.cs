namespace Dec
{
    /// <summary>
    /// Information on the current cursor position when reading files.
    /// </summary>
    /// <remarks>
    /// Standard output format is $"{inputContext}: Your Error Text Here!". This abstracts out the requirements for generating the locational-context text.
    ///
    /// This is a struct to cut down on GC churn.
    /// </remarks>
    public struct Context
    {
        internal string filename;
        internal System.Xml.Linq.XElement element;
        internal Path path;

        internal Context(string filename = null, System.Xml.Linq.XElement element = null, Path path = null)
        {
            this.filename = filename;
            this.element = element;
            this.path = path;
        }

        /// <summary>
        /// Returns the best guess at the path of this Record query.
        /// </summary>
        public string PathString()
        {
            return path?.Serialize() ?? "[unknown]";
        }

        /// <summary>
        /// Post a proper Context-decorated message to the info log.
        /// </summary>
        public void Inf(string message)
        {
            Dbg.Inf($"{this}: {message}");
        }

        /// <summary>
        /// Post a proper Context-decorated message to the warning log.
        /// </summary>
        public void Wrn(string message)
        {
            Dbg.Wrn($"{this}: {message}");
        }

        /// <summary>
        /// Post a proper Context-decorated message to the error log.
        /// </summary>
        public void Err(string message)
        {
            Dbg.Err($"{this}: {message}");
        }

        public override string ToString()
        {
            if (this.element != null)
            {
                return $"{filename}:{element.LineNumber()}";
            }
            else if (filename != null)
            {
                return filename;
            }
            else if (path != null)
            {
                return path.Serialize();
            }
            else
            {
                // shrug
                return "[unknown]";
            }
        }
    }
}