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

        /// <summary>
        /// The player turned the mod off, so it won't load. That isn't a problem: it doesn't count against other mods, and problems naming only turned-off mods are listed but not reported. Every installed copy of a turned-off id is Off, except that a duplicate's other copies are Hidden as usual.
        /// </summary>
        Off,
    }
}
