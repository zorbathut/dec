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

        public string PathString()
        {
            return path?.Serialize() ?? "[unknown]";
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