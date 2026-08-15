using System;
using System.Collections.Generic;

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
    /// Signals that this struct should be cloned with standard shallow-copy assignment semantics.
    /// </summary>
    /// <remarks>
    /// This is potentially much faster than using IRecordable to accomplish the same thing, especially if the struct is in a collection of some kind.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Struct)]
    public class CloneStructPiecewiseAttribute : Attribute
    {

    }

    /// <summary>
    /// Signals that this class should be "cloned" by simply duplicating the reference.
    /// </summary>
    /// <remarks>
    /// This is (obviously) much faster than copying the class, though it also (obviously) leaves you with a "clone" that shares state with the original. Great for immutable classes or classes wrapped in copy-on-write structures.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public class CloneClassAsSharedRefAttribute : Attribute
    {

    }

    /// <summary>
    /// Marks a method as a setup function, run automatically at the end of Parser.Finish() and Recorder.Read/ReadSimple.
    /// </summary>
    /// <remarks>
    /// An instance setup function runs once for each instance of its class, during Parser.Finish() or at the end of Recorder.Read/ReadSimple. Static setup functions run exactly once during Parser.Finish(). Recorder.Clone never triggers setup. The method must have the signature `void M(Action&lt;string&gt; reporter)`; call the reporter to report errors attributed to the instance being processed.
    ///
    /// Execution order is controlled by [Dec.SetupAfter] and [Dec.SetupBefore]; all setup functions, including the built-in ConfigErrors/PostLoad passes, are ordered together in one dependency graph.
    ///
    /// Set Parallel to true to allow an instance setup function to run across its instances on multiple threads; reports go through the Config handlers from worker threads (handlers must be threadsafe), and a parallel function must not call Dec's database mutation APIs.
    ///
    /// Set Stage to add this function to a stage class; other setup functions can then order themselves against the entire stage by referencing that class in [Dec.SetupAfter] or [Dec.SetupBefore].
    ///
    /// Setup functions are not valid on structs. Instances created inside constructors or field initializers and never touched by the loaded data may not be seen and may not have setup functions run on them. Don't rely on this though.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class SetupAttribute : Attribute
    {
        /// <summary>
        /// Optional stage class this function is a member of; dependencies on that class include this function.
        /// </summary>
        public Type Stage { get; set; }

        /// <summary>
        /// Allows this instance setup function to be run across its instances in parallel.
        /// </summary>
        public bool Parallel { get; set; }
    }

    /// <summary>
    /// Declares that a setup function, or every setup function of a class, must run after another class's or function's setup.
    /// </summary>
    /// <remarks>
    /// Applied to a method, it constrains that single setup function; applied to a class, it constrains every setup function belonging to that class's stage, including ConfigErrors/PostLoad on Dec classes.
    ///
    /// Referencing a type means "after that type's own setup": every setup function the type has - declared on it or inherited into it, including ConfigErrors/PostLoad on Dec classes - as it runs on instances of the type and its subclasses. Setup functions introduced by derived classes are not included; set IncludeDerived to true to also wait on those.
    ///
    /// Referencing a type plus a member name means "after that specific setup function". IncludeDerived cannot be combined with a member name.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public class SetupAfterAttribute : Attribute
    {
        private Type type;
        private string memberName;

        internal Type Type => type;
        internal string MemberName => memberName;

        /// <summary>
        /// Extends a bare-type reference to also cover setup functions introduced by the referenced type's derived classes.
        /// </summary>
        public bool IncludeDerived { get; set; }

        public SetupAfterAttribute(Type type)
        {
            this.type = type;
        }

        public SetupAfterAttribute(Type type, string memberName)
        {
            this.type = type;
            this.memberName = memberName;
        }
    }

    /// <summary>
    /// Declares that a setup function, or every setup function of a class, must run before another class's or function's setup.
    /// </summary>
    /// <remarks>
    /// This is the mirror image of [Dec.SetupAfter]; see that attribute for the full semantics. It exists chiefly so a class can insert its setup ahead of a class it can't modify, which is common in mod modules.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public class SetupBeforeAttribute : Attribute
    {
        private Type type;
        private string memberName;

        internal Type Type => type;
        internal string MemberName => memberName;

        /// <summary>
        /// Extends a bare-type reference to also cover setup functions introduced by the referenced type's derived classes.
        /// </summary>
        public bool IncludeDerived { get; set; }

        public SetupBeforeAttribute(Type type)
        {
            this.type = type;
        }

        public SetupBeforeAttribute(Type type, string memberName)
        {
            this.type = type;
            this.memberName = memberName;
        }
    }

    /// <summary>
    /// Marks a Converter class as factory-only: it will not be auto-registered by Dec's converter scan, and must be produced on demand by a `Config.ConverterFactory` callback instead.
    /// </summary>
    /// <remarks>
    /// Without this attribute, Dec's scan would treat the class as a normal Converter, try to instantiate it via its parameterless constructor, and register it against the type declared in its Converter base. For factory-only converters the parameterless-constructor requirement is usually unsatisfiable, and even when it isn't, the declared-type registration would shadow the `Config.ConverterFactory` lookup that's supposed to produce the right instance.
    ///
    /// `ConverterStringDynamic`, `ConverterRecordDynamic`, and `ConverterFactoryDynamic` already carry this attribute themselves, so every Dynamic subclass inherits it automatically and doesn't need to be tagged.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class FactoryOnlyAttribute : Attribute
    {

    }
}
