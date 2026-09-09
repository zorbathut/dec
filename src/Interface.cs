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
        /// While members will be initialized, it is possible that classes referenced by this class will not yet be fully cloned. Limit your operations to the current object and its members.
        /// </remarks>
        void PostCloneNew();
    }

    /// <summary>
    /// Used to name an object's entry in a Recorder's reference block.
    /// </summary>
    /// <remarks>
    /// This applies only to objects that are actually written as references.
    ///
    /// Names should be unique within a single write, otherwise Dec will generate its own names.
    /// </remarks>
    public interface IRefName
    {
        /// <summary>
        /// Returns the name for this object's reference entry, or null to accept a generated name.
        /// </summary>
        string RefName(Recorder.IUserSettings userSettings);
    }

    /// <summary>
    /// Used to force an object into a Recorder's reference block even when only one reference to it exists.
    /// </summary>
    /// <remarks>
    /// This does not set .Shared(); it has no effect if an object isn't .Shared().
    /// </remarks>
    public interface IRefForce
    {
    }
}
