namespace Dec
{
    /// <summary>
    /// Used to provide a per-object post-clone callback, applying to the original object.
    /// </summary>
    public interface IPostCloneOriginal
    {
        /// <summary>
        /// Called after an object's clone has been created.
        /// </summary>
        void PostCloneOriginal();
    }

    /// <summary>
    /// Used to provide a per-object post-clone callback, applying to the new object.
    /// </summary>
    public interface IPostCloneNew
    {
        /// <summary>
        /// Called on the clone of an object.
        /// </summary>
        /// <remarks>
        /// While members will be initialized, it is possible that classes referenced by this class will not yet be fully cloned.
        /// </remarks>
        void PostCloneNew();
    }
}
