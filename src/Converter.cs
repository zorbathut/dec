using System;

namespace Dec
{


    /// <summary>
    /// Internal class for Converter systems.
    /// </summary>
    /// <remarks>
    /// You probably shouldn't use this for anything - it may vanish without warning or regret, I just haven't figured out how to move it into Internal.
    /// </remarks>
    public abstract class Converter
    {
        // This is `internal virtual` because there's no way I can find to specify `internal abstract`.
        internal virtual Type GetConvertedTypeHint() { return null; }

        /// <summary>
        /// Signals whether to treat this like a valuelike object.
        /// </summary>
        /// <remarks>
        /// Valuelikes cannot be shared and do not produce warnings if "the same object" is not shared.
        ///
        /// This is appropriate for stuff like `string`, `Type`, `MethodInfo`, etc, which are technically classes but act more like immutable value types.
        /// </remarks>
        public virtual bool TreatAsValuelike() { return false; }
    }

    /// <summary>
    /// Internal class for Converter systems.
    /// </summary>
    /// <remarks>
    /// You probably shouldn't use this for anything - it may vanish without warning or regret, I just haven't figured out how to move it into Internal.
    /// </remarks>
    public abstract class ConverterString : Converter
    {
        internal abstract string WriteObj(object input);
        internal abstract object ReadObj(string input, InputContext context);
    }

    /// <summary>
    /// Base class for converting to arbitrary types via strings.
    /// </summary>
    /// <remarks>
    /// This is a standalone class to allow implementation of converters of third-party types. It's useful when implementing converters for types that were not created by you (ex: UnityEngine.Mesh).
    ///
    /// ConverterString is suitable only for converting to and from independent simple string objects. It does not let you reference other objects within the Recorder hierarchy (see ConverterRecord and ConverterFactory), nor does it allow you to respect default settings pre-provided by an object's constructor, nor does it allow you to build complex hierarchies appropriate for complex objects.
    ///
    /// It does allow you to create your own objects as you see fit, or reference objects provided by your framework that have no useful default constructor.
    ///
    /// This should likely be your first choice of Converter if reasonably suitable, although if string serialization is complicated, use ConverterRecord instead even if you don't technically have to.
    /// </remarks>
    public abstract class ConverterString<T> : ConverterString
    {
        /// <summary>
        /// Converts an object to a string.
        /// </summary>
        public abstract string Write(T input);

        /// <summary>
        /// Converts a string to a suitable object type.
        /// </summary>
        /// <remarks>
        /// In case of error, call Dec.Dbg.Err with some appropriately useful message and return default. Message should be formatted as $"{context}: Something went wrong".
        /// </remarks>
        public abstract T Read(string input, InputContext context);

        override internal Type GetConvertedTypeHint()
        {
            return typeof(T);
        }

        override internal string WriteObj(object input)
        {
            return Write((T)input);
        }
        override internal object ReadObj(string input, InputContext context)
        {
            return Read(input, context);
        }
    }

    /// <summary>
    /// Alternate dynamic-typed class for converting to arbitrary types via strings.
    /// </summary>
    /// <remarks>
    /// This is a variant of ConverterString that allows you to use arbitrary types, which is helpful if you're trying to convert non-public types.
    ///
    /// In most cases it should be avoided.
    /// </remarks>
    public abstract class ConverterStringDynamic : ConverterString
    {
        /// <summary>
        /// Converts an object to a string.
        /// </summary>
        public abstract string Write(object input);

        /// <summary>
        /// Converts a string to a suitable object type.
        /// </summary>
        /// <remarks>
        /// In case of error, call Dec.Dbg.Err with some appropriately useful message and return default. Message should be formatted as $"{context}: Something went wrong".
        /// </remarks>
        public abstract object Read(string input, InputContext context);

        override internal string WriteObj(object input)
        {
            return Write(input);
        }
        override internal object ReadObj(string input, InputContext context)
        {
            return Read(input, context);
        }
    }

    /// <summary>
    /// Internal class for Converter systems.
    /// </summary>
    /// <remarks>
    /// You probably shouldn't use this for anything - it may vanish without warning or regret, I just haven't figured out how to move it into Internal.
    /// </remarks>
    public abstract class ConverterRecord : Converter
    {
        internal abstract object RecordObj(object input, Recorder recorder);
    }

    /// <summary>
    /// Base class for converting to arbitrary types via the Recorder API.
    /// </summary>
    /// <remarks>
    /// This is a standalone class to allow implementation of converters of third-party types. It's useful when implementing converters for types that were not created by you (ex: UnityEngine.Vector).
    ///
    /// ConverterRecord is suitable only for converting to and from objects with usable default constructors. It does not allow you to create your own objects or return objects that are provided by your framework (see ConverterString and ConverterFactory).
    ///
    /// It does allow you to reference other objects within the Recorder hierarchy. It also allows you to respect pre-set defaults (if not shared) and build complex hierarchies for complicated data types.
    ///
    /// This should likely be your second choice of Converter, used only if ConverterString is inappropriate.
    /// </remarks>
    public abstract class ConverterRecord<T> : ConverterRecord
    {
        /// <summary>
        /// Records an object.
        /// </summary>
        /// <remarks>
        /// See [`Dec.IRecordable.Record`](xref:Dec.IRecordable.Record*) for details, although you'll need to use `this` instead of `input`.
        /// </remarks>
        public abstract void Record(ref T input, Recorder recorder);

        override internal Type GetConvertedTypeHint()
        {
            return typeof(T);
        }

        override internal object RecordObj(object input, Recorder recorder)
        {
            T var = (T)input;
            Record(ref var, recorder);
            return var;
        }
    }

    /// <summary>
    /// Alternate dynamic-typed class for converting to arbitrary types via the Recorder API.
    /// </summary>
    /// <remarks>
    /// This is a variant of ConverterRecord that allows you to use arbitrary types, which is helpful if you're trying to convert non-public types.
    ///
    /// In most cases it should be avoided.
    /// </remarks>
    public abstract class ConverterRecordDynamic : ConverterRecord
    {
        /// <summary>
        /// Records an object.
        /// </summary>
        /// <remarks>
        /// See [`Dec.IRecordable.Record`](xref:Dec.IRecordable.Record*) for details, although you'll need to use `this` instead of `input`.
        /// </remarks>
        public abstract object Record(ref object input, Recorder recorder);

        override internal object RecordObj(object input, Recorder recorder)
        {
            Record(ref input, recorder);
            return input;
        }
    }

    /// <summary>
    /// Internal class for Converter systems.
    /// </summary>
    /// <remarks>
    /// You probably shouldn't use this for anything - it may vanish without warning or regret, I just haven't figured out how to move it into Internal.
    /// </remarks>
    public abstract class ConverterFactory : Converter
    {
        internal abstract void WriteObj(object input, Recorder recorder);

        internal abstract object CreateObj(Recorder recorder);
        internal abstract object ReadObj(object input, Recorder recorder);
    }

    /// <summary>
    /// Base class for converting to arbitrary types via the Recorder API, with full control over the object creation process.
    /// </summary>
    /// <remarks>
    /// This is a standalone class to allow implementation of converters of third-party types. It's useful when implementing converters for types that were not created by you (ex: UnityEngine.Vector).
    ///
    /// ConverterFactory is suitable for converting to and from objects constructed in whatever way you wish, possibly with custom constructors or provided via your framework. It allows you to reference other objects and build arbitrarily complicated hierarchies.
    ///
    /// This is the most complicated Converter to work with, but also the most powerful. This should be your last choice of Converter, used only if neither ConverterString nor ConverterRecord are appropriate. Empirically, this appears to be rarely used . . . but it's available when needed.
    /// </remarks>
    public abstract class ConverterFactory<T> : ConverterFactory
    {
        /// <summary>
        /// Writes an object.
        /// </summary>
        /// <remarks>
        /// See [`Dec.IRecordable.Record`](xref:Dec.IRecordable.Record*) for details, although you'll need to use `this` instead of `input`.
        /// </remarks>
        public abstract void Write(T input, Recorder recorder);

        /// <summary>
        /// Creates an object.
        /// </summary>
        /// <remarks>
        /// This is similar to [`Dec.IRecordable.Record`](xref:Dec.IRecordable.Record*), although you'll need to use `this` instead of `input`.
        ///
        /// This function will not be called if an instance already exists. In addition, you *cannot* reference other shared objects within Create, even transitively. Those must be referenced within Read. It is recommended that you do the bare minimum here to create the necessary object.
        /// </remarks>
        public abstract T Create(Recorder recorder);

        /// <summary>
        /// Reads an object.
        /// </summary>
        /// <remarks>
        /// This is similar to [`Dec.IRecordable.Record`](xref:Dec.IRecordable.Record*), although you'll need to use `this` instead of `input`.
        /// </remarks>
        public abstract void Read(ref T input, Recorder recorder);

        override internal Type GetConvertedTypeHint()
        {
            return typeof(T);
        }

        override internal void WriteObj(object input, Recorder recorder)
        {
            Write((T)input, recorder);
        }

        override internal object CreateObj(Recorder recorder)
        {
            return Create(recorder);
        }
        override internal object ReadObj(object input, Recorder recorder)
        {
            T var = (T)input;
            Read(ref var, recorder);
            return var;
        }
    }

    /// <summary>
    /// Alternate dynamic-typed class for converting to arbitrary types via the Recorder API, with full control over the object creation process.
    /// </summary>
    /// <remarks>
    /// This is a variant of ConverterFactory that allows you to use arbitrary types, which is helpful if you're trying to convert non-public types.
    ///
    /// In most cases it should be avoided.
    /// </remarks>
    public abstract class ConverterFactoryDynamic : ConverterFactory
    {
        /// <summary>
        /// Writes an object.
        /// </summary>
        /// <remarks>
        /// See [`Dec.IRecordable.Record`](xref:Dec.IRecordable.Record*) for details, although you'll need to use `this` instead of `input`.
        /// </remarks>
        public abstract void Write(object input, Recorder recorder);

        /// <summary>
        /// Creates an object.
        /// </summary>
        /// <remarks>
        /// This is similar to [`Dec.IRecordable.Record`](xref:Dec.IRecordable.Record*), although you'll need to use `this` instead of `input`.
        ///
        /// This function will not be called if an instance already exists. In addition, you *cannot* reference other shared objects within Create, even transitively. Those must be referenced within Read. It is recommended that you do the bare minimum here to create the necessary object.
        /// </remarks>
        public abstract object Create(Recorder recorder);

        /// <summary>
        /// Reads an object.
        /// </summary>
        /// <remarks>
        /// This is similar to [`Dec.IRecordable.Record`](xref:Dec.IRecordable.Record*), although you'll need to use `this` instead of `input`.
        /// </remarks>
        public abstract void Read(ref object input, Recorder recorder);

        override internal void WriteObj(object input, Recorder recorder)
        {
            Write(input, recorder);
        }

        override internal object CreateObj(Recorder recorder)
        {
            return Create(recorder);
        }
        override internal object ReadObj(object input, Recorder recorder)
        {
            Read(ref input, recorder);
            return input;
        }
    }
}
