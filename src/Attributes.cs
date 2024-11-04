using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Dec
{
    /// <summary>
    /// Signals that static references in this class should be filled out after parsing is complete.
    /// </summary>
    /// <remarks>
    /// In addition, the class's static constructor should call StaticReferencesAttribute.Initialized().
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public class StaticReferencesAttribute : Attribute
    {
        // This keeps track of which static references we actually have filled. It exists largely for Database.Clear().
        internal static HashSet<Type> StaticReferencesFilled = new HashSet<Type>();

        /// <summary>
        /// Informs the construction environment that a static-reference class has been constructed.
        /// </summary>
        /// <remarks>
        /// This must be placed in the static constructor of any StaticReferences class, but not otherwise called.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]  // We use stack black magic to identify the class, so we need to make sure it isn't inlined
        public static void Initialized()
        {
            ParserModular.StaticReferencesInitialized();
        }
    }

    /// <summary>
    /// Signals that this Dec-deriving class is not a valid database root. No classes of this type will be instantiated and children of this class will have their own namespaces.
    /// </summary>
    /// <remarks>
    /// Classes with this attribute must be abstract. The parent class of this class must also be marked with this.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public class AbstractAttribute : Attribute
    {

    }

    /// <summary>
    /// Signals that this class or struct should be cloned with standard assignment.
    /// </summary>
    /// <remarks>
    /// This is most useful for collections of objects, where it can rely on the collection's own cloning behavior to clone a vast swath of objects at once.
    ///
    /// For structs, this becomes a shallow copy. For classes, this becomes a reference copy.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class CloneWithAssignmentAttribute : Attribute
    {

    }

    /// <summary>
    /// Signals that this class should have its ConfigErrors/PostLoad run after a different class.
    /// </summary>
    /// <remarks>
    /// Currently valid and meaningful only when applied to, and referencing, things inheriting from Dec.Dec.
    ///
    /// Configuration order will be (mostly) stable with a fixed set of constraints into account.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SetupDependsOnAttribute : Attribute
    {
        private Type type;

        internal Type Type => type;

        public SetupDependsOnAttribute(Type type)
        {
            this.type = type;
        }
    }
}
