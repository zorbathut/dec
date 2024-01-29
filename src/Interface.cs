namespace Dec
{
    /// <summary>
    /// Used to provide a per-object post-clone callback.
    /// </summary>
    public interface IPostClone
    {
        /// <summary>
        /// Called after an object is cloned.
        /// </summary>
        /// <remarks>
        /// While members will be initialized, it is possible that classes referenced by this class will not yet be fully cloned.
        /// </remarks>
        void PostClone();
    }
}
