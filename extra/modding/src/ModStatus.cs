namespace Dec.Modding
{
    public enum ModStatus
    {
        /// <summary>
        /// The mod will load.
        /// </summary>
        Enabled,

        /// <summary>
        /// The mod won't load; at least one Error problem names it.
        /// </summary>
        Disabled,

        /// <summary>
        /// Another installed copy of the same mod was chosen instead.
        /// </summary>
        Hidden,
    }
}
