using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dec
{
    /// <summary>
    /// Introspection of an object's serialization structure: enumerating the positions its Record() would produce, and writing values back to them.
    /// </summary>
    public static class Reflection
    {
        /// <summary>
        /// A single recorded position in an object's serialization structure, as reported by Enumerate.
        /// </summary>
        /// <remarks>
        /// An Entry describes one position that a serialization write would produce: what static type it was declared with, what value it currently holds, whether it would serialize as a reference, and whether SetByPath can address it.
        ///
        /// Entry trees are snapshots of structure and boxed values; they are not live views, and mutating the underlying object does not restructure previously returned entries. Value holds live object references, though, so mutations to a referenced object are visible through it.
        /// </remarks>
        public sealed class Entry
        {
            /// <summary>
            /// A best-effort textual label this position was recorded under.
            /// </summary>
            public string Label { get; internal set; }

            /// <summary>
            /// The statically declared type of this position.
            /// </summary>
            /// <remarks>
            /// This is correct even when the value is null or a subclass instance; it is the type the serialization system would use for family dispatch.
            /// </remarks>
            public Type DeclaredType { get; internal set; }

            /// <summary>
            /// Whether this position could serialize as a reference.
            /// </summary>
            public bool Shared { get; internal set; }

            /// <summary>
            /// The current value at this position.
            /// </summary>
            public object Value { get; internal set; }

            /// <summary>
            /// Whether SetByPath can address this position.
            /// </summary>
            public bool Writable { get; internal set; }

            /// <summary>
            /// The entry this one was enumerated under, or null for the root.
            /// </summary>
            public Entry Parent { get; internal set; }

            /// <summary>
            /// The Path identifying this position.
            /// </summary>
            /// <remarks>
            /// Path.Serialize() provides a display rendering such as "options[2].node". Paths compare structurally via Equals, so writable positions can be keyed across separate Enumerate calls. Read-only dictionary key/value and set interiors are currently unpathable - sibling entries there share one path identity.
            /// </remarks>
            public Path Path { get; internal set; }

            internal readonly List<Entry> ChildrenMutable = new List<Entry>();
            private ReadOnlyCollection<Entry> childrenView;

            /// <summary>
            /// The entries recorded beneath this position.
            /// </summary>
            public IReadOnlyList<Entry> Children
            {
                get
                {
                    if (childrenView == null)
                    {
                        childrenView = new ReadOnlyCollection<Entry>(ChildrenMutable);
                    }

                    return childrenView;
                }
            }

            internal Entry() { }
        }

        /// <summary>
        /// Runs a serialization write of obj under an introspection backend and returns a tree of entries, one per recorded position.
        /// </summary>
        /// <remarks>
        /// obj can be anything Recorder.Write accepts: a recordable, a converter-backed type, a container, or a scalar. The returned root entry represents obj itself, declared as T exactly as Recorder.Write&lt;T&gt; would write it; its children are the positions beneath it.
        ///
        /// The traversal reports what a Recorder.Write of the same object would produce, using the same dispatch: shared object references are leaves, containers descend, non-shared recordable values descend into their own Record(), ConverterRecord and ConverterFactory bodies descend the same way, ConverterString types are leaves, and RecordAsThis flattens. A repeat encounter of a shared container is a leaf — the file contains a bare reference there — so only its first occurrence carries children. Positions beneath a ConverterFactory body, or beneath a value-type root, report Writable false: neither has a channel to carry a write back. Shared-pipeline diagnostics fire as they would on a real write; messages the backends own (such as unregistered-Dec reporting) are not emitted, and the recursion depth limit is slightly stricter than the XML writers', which defer deep chains rather than stopping.
        ///
        /// A Dec root reports what Composer.ComposeXml would write for that Dec instead: its serializable fields by reflection in declaration order, derived class first, unless the Dec implements IRecordable and records itself like anything else; the root Path is a PathDec. The composer has no reference system, so Shared is false throughout, a Dec-typed field is a leaf holding the referenced Dec, and an object reachable from several positions enumerates in full at each of them.
        ///
        /// shouldDescend, when provided, is called for non-root entries that can carry children (a non-shared recordable, a converter body, a container, or under a Dec root any position walked by reflection, whether or not it turns out to hold any), with the entry fully populated except for Children. Returning false leaves it a childless entry. The call is not guaranteed; in some cases it may behave like the callback returned true.
        /// </remarks>
        public static Entry Enumerate<T>(T obj, Func<Entry, bool> shouldDescend = null, Recorder.IUserSettings userSettings = null)
        {
            if (obj == null)
            {
                Dbg.Err("Reflection.Enumerate called with a null object");
                return null;
            }

            Serialization.Initialize();

            using (var _ = new CultureInfoScope(Config.CultureInfo))
            {
                bool compose = obj is Dec;
                var writer = new WriterIntrospect(userSettings, shouldDescend, compose);
                var rootEntry = new Entry();

                WriterNodeIntrospect node;
                if (compose)
                {
                    node = WriterNodeIntrospect.StartDec(writer, rootEntry, obj as Dec);
                }
                else
                {
                    // The same root name the serialization writers use, so entry paths and their diagnostics read identically to a real write's.
                    node = WriterNodeIntrospect.StartRecord(writer, rootEntry, new PathRoot("RECORD"), writableSuppressed: obj.GetType().IsValueType);
                }

                Serialization.ComposeElement(node, obj, typeof(T), isRootDec: compose);

                return rootEntry;
            }
        }

        /// <summary>
        /// Sets the value at a path previously reported by Enumerate, replaying the same Record() bodies to locate the position.
        /// </summary>
        /// <remarks>
        /// obj is the object the path was enumerated from, or any object with the same structure; the path's root segment is ignored. It must be a reference type: a value-type root arrives boxed, so nothing written beneath it could reach the caller's variable.
        ///
        /// Failures — an unresolvable path, a read-only position (including anything beneath a ConverterFactory body), an assignment-incompatible value, or a ShouldRecord-suppressed body — report an error, which throws under the default error handler.
        ///
        /// SetByPath writes through the ref the Record() body passed and nothing more; a body that records a temporary computed on the write path will silently discard the assignment. A ConverterRecord body that replaces its instance is honored, since the value it hands back is written into the owning slot; at the root there is no such slot, and that case reports an error.
        ///
        /// Under a Dec root, positions the composer reaches by reflection are written through their fields, readonly fields included; an object reachable from several positions is changed for all of them. A Dec-typed field can be replaced but not entered.
        /// </remarks>
        public static void SetByPath(object obj, Path path, object value, Recorder.IUserSettings userSettings = null)
        {
            if (obj == null)
            {
                Dbg.Err("Reflection.SetByPath called with a null object");
                return;
            }

            if (obj.GetType().IsValueType)
            {
                Dbg.Err($"Reflection.SetByPath called on a value type {obj.GetType()}; it arrives boxed, so nothing written beneath it could reach the caller's variable. Pass the object that contains it instead.");
                return;
            }

            if (path == null)
            {
                Dbg.Err("Reflection.SetByPath called with a null path");
                return;
            }

            Serialization.Initialize();

            using (var _ = new CultureInfoScope(Config.CultureInfo))
            {
                // Materialize the parent-linked chain root-first; candidates are built on the chain's own root instance, so any enumeration root works unmodified.
                var chain = new List<Path>();
                for (var segment = path; segment != null; segment = segment.GetParent())
                {
                    chain.Add(segment);
                }
                chain.Reverse();

                if (!(chain[0] is PathRoot) && !(chain[0] is PathDec) && !(chain[0] is PathRef))
                {
                    Dbg.Err($"SetByPath: path starts with a {chain[0].GetType()} segment instead of a root, which usually means a segment was constructed with a null parent");
                    return;
                }

                if (chain.Count < 2)
                {
                    Dbg.Err($"SetByPath: path [{path.Serialize()}] has no segments beyond the root");
                    return;
                }

                if (!path.IsSettable())
                {
                    Dbg.Err($"SetByPath: path [{path.Serialize()}] addresses a read-only position; dictionary, set, queue, stack, tuple, and multidimensional-array positions are not settable");
                    return;
                }

                var state = new RecorderSetByPath.State() { chain = chain, newValue = value, userSettings = userSettings, targetSerialized = path.Serialize(), compose = obj is Dec };

                // Every failure inside ApplyInto reports its own error. A reference-type root needs no write-back, unless a converter body handed back a different instance; nested positions store that into the owning slot, but the root has none.
                var (landed, updated) = RecorderSetByPath.ApplyInto(obj, 1, state, isRoot: true);
                if (landed && !ReferenceEquals(updated, obj))
                {
                    Dbg.Err($"SetByPath: the write to [{path.Serialize()}] landed on a replacement instance of {obj.GetType()} created by its converter, and a root has no owning slot to store it in; pass an object that contains it instead");
                }
            }
        }
    }
}
