namespace Dec
{
    [System.Diagnostics.DebuggerDisplay("{Serialize(),nq}")]
    public abstract class Path
    {
        public abstract string Serialize();

        // Whether this path re-finds the object it describes. Besides deciding what the database can write a reference to, this is what setup diagnostics use to choose between several paths to one shared object, on the grounds that a path which can't re-find the object also can't tell a human which object it is.
        public abstract bool IsValidForWriting();

        internal abstract Path GetParent();

        // Whether a write could address this position: every segment must be a recorder member or an array/list index. A different axis than IsValidForWriting, which is about reference re-finding.
        internal abstract bool IsSettable();

        internal static bool ParentsEqual(Path lhs, Path rhs)
        {
            if (ReferenceEquals(lhs, rhs))
            {
                return true;
            }

            if (lhs is null || rhs is null)
            {
                return false;
            }

            return lhs.Equals(rhs);
        }

        // Paths compare structurally, so separately constructed chains describing the same position are equal and usable as keys.
        public abstract override bool Equals(object obj);
        public abstract override int GetHashCode();
    }

    public class PathRoot : Path
    {
        private string rootType;

        public PathRoot(string rootType)
        {
            this.rootType = rootType;
        }

        public override string Serialize()
        {
            return rootType;
        }

        public override bool IsValidForWriting()
        {
            return true;
        }

        internal override Path GetParent()
        {
            return null;
        }

        internal override bool IsSettable()
        {
            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is PathRoot rhs && rootType == rhs.rootType;
        }

        public override int GetHashCode()
        {
            return unchecked(0x50a7_0001 * 31 + (rootType?.GetHashCode() ?? 0));
        }
    }

    public class PathDec : Path
    {
        // we keep these separate so we don't have to do string manipulation at runtime
        private System.Type decType;
        private string decTypeName; // this is kind of redundant and should be cleaned up, but right now it's hard to tell what the actual path should be; calculating a "valid dec name" is tricky
        private string decName;

        public PathDec(System.Type decType, string decName)
        {
            this.decType = decType;
            this.decName = decName;
        }

        public PathDec(string decTypeName, string decName)
        {
            this.decTypeName = decTypeName;
            this.decName = decName;
        }

        private string EffectiveTypeName()
        {
            return decTypeName ?? decType.ComposeDecFormatted();
        }

        public override string Serialize()
        {
            return $"{EffectiveTypeName()}.{decName}";
        }

        public override bool IsValidForWriting()
        {
            return true;
        }

        internal override Path GetParent()
        {
            return null;
        }

        internal override bool IsSettable()
        {
            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is PathDec rhs && decName == rhs.decName && EffectiveTypeName() == rhs.EffectiveTypeName();
        }

        public override int GetHashCode()
        {
            return unchecked((0x50a7_0002 * 31 + (decName?.GetHashCode() ?? 0)) * 31 + (EffectiveTypeName()?.GetHashCode() ?? 0));
        }
    }

    public class PathRef : Path
    {
        // we keep these separate so we don't have to do string manipulation at runtime
        private string refName;

        public PathRef(string refName)
        {
            this.refName = refName;
        }

        public override string Serialize()
        {
            return $"REF.{refName}";
        }

        public override bool IsValidForWriting()
        {
            // how did this even happen?
            return false;
        }

        internal override Path GetParent()
        {
            return null;
        }

        internal override bool IsSettable()
        {
            return false;
        }

        public override bool Equals(object obj)
        {
            return obj is PathRef rhs && refName == rhs.refName;
        }

        public override int GetHashCode()
        {
            return unchecked(0x50a7_0003 * 31 + (refName?.GetHashCode() ?? 0));
        }
    }

    public class PathMember : Path
    {
        private Path parent;
        private string memberName;

        public PathMember(Path parent, string memberName)
        {
            this.parent = parent;
            this.memberName = memberName;
        }

        public override string Serialize()
        {
            return $"{parent.Serialize()}.{memberName}";
        }

        public override bool IsValidForWriting()
        {
            return parent.IsValidForWriting();
        }

        internal override Path GetParent()
        {
            return parent;
        }

        internal override bool IsSettable()
        {
            return parent.IsSettable();
        }

        public override bool Equals(object obj)
        {
            return obj is PathMember rhs && memberName == rhs.memberName && ParentsEqual(parent, rhs.parent);
        }

        public override int GetHashCode()
        {
            return unchecked((0x50a7_0004 * 31 + (parent?.GetHashCode() ?? 0)) * 31 + (memberName?.GetHashCode() ?? 0));
        }
    }

    public class PathIndex : Path
    {
        private Path parent;
        private int index;

        public PathIndex(Path parent, int index)
        {
            this.parent = parent;
            this.index = index;
        }

        public override string Serialize()
        {
            return $"{parent.Serialize()}[{index}]";
        }

        public override bool IsValidForWriting()
        {
            return parent.IsValidForWriting();
        }

        internal override Path GetParent()
        {
            return parent;
        }

        internal override bool IsSettable()
        {
            return parent.IsSettable();
        }

        public override bool Equals(object obj)
        {
            return obj is PathIndex rhs && index == rhs.index && ParentsEqual(parent, rhs.parent);
        }

        public override int GetHashCode()
        {
            return unchecked((0x50a7_0005 * 31 + (parent?.GetHashCode() ?? 0)) * 31 + index);
        }
    }

    public class PathIndexMultidim : Path
    {
        private Path parent;
        private int[] indices;

        public PathIndexMultidim(Path parent, int[] indices)
        {
            this.parent = parent;
            this.indices = indices;
        }

        public override string Serialize()
        {
            return $"{parent.Serialize()}[{string.Join(",", indices)}]";
        }

        public override bool IsValidForWriting()
        {
            return parent.IsValidForWriting();
        }

        internal override Path GetParent()
        {
            return parent;
        }

        internal override bool IsSettable()
        {
            return false;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PathIndexMultidim rhs) || !ParentsEqual(parent, rhs.parent))
            {
                return false;
            }

            if (indices == null || rhs.indices == null)
            {
                return indices == rhs.indices;
            }

            if (indices.Length != rhs.indices.Length)
            {
                return false;
            }

            for (int i = 0; i < indices.Length; ++i)
            {
                if (indices[i] != rhs.indices[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            int hash = unchecked(0x50a7_0006 * 31 + (parent?.GetHashCode() ?? 0));
            if (indices != null)
            {
                foreach (var index in indices)
                {
                    hash = unchecked(hash * 31 + index);
                }
            }
            return hash;
        }
    }

    // Ordered positions that serialize like array elements but are not writable positions; Serialize output is deliberately identical to PathIndex.
    public class PathQueueElement : Path
    {
        private Path parent;
        private int index;

        public PathQueueElement(Path parent, int index)
        {
            this.parent = parent;
            this.index = index;
        }

        public override string Serialize()
        {
            return $"{parent.Serialize()}[{index}]";
        }

        public override bool IsValidForWriting()
        {
            return parent.IsValidForWriting();
        }

        internal override Path GetParent()
        {
            return parent;
        }

        internal override bool IsSettable()
        {
            return false;
        }

        public override bool Equals(object obj)
        {
            return obj is PathQueueElement rhs && index == rhs.index && ParentsEqual(parent, rhs.parent);
        }

        public override int GetHashCode()
        {
            return unchecked((0x50a7_000B * 31 + (parent?.GetHashCode() ?? 0)) * 31 + index);
        }
    }

    public class PathStackElement : Path
    {
        private Path parent;
        private int index;

        public PathStackElement(Path parent, int index)
        {
            this.parent = parent;
            this.index = index;
        }

        public override string Serialize()
        {
            return $"{parent.Serialize()}[{index}]";
        }

        public override bool IsValidForWriting()
        {
            return parent.IsValidForWriting();
        }

        internal override Path GetParent()
        {
            return parent;
        }

        internal override bool IsSettable()
        {
            return false;
        }

        public override bool Equals(object obj)
        {
            return obj is PathStackElement rhs && index == rhs.index && ParentsEqual(parent, rhs.parent);
        }

        public override int GetHashCode()
        {
            return unchecked((0x50a7_000C * 31 + (parent?.GetHashCode() ?? 0)) * 31 + index);
        }
    }

    public class PathTupleItem : Path
    {
        private Path parent;
        private int index;

        public PathTupleItem(Path parent, int index)
        {
            this.parent = parent;
            this.index = index;
        }

        public override string Serialize()
        {
            return $"{parent.Serialize()}[{index}]";
        }

        public override bool IsValidForWriting()
        {
            return parent.IsValidForWriting();
        }

        internal override Path GetParent()
        {
            return parent;
        }

        internal override bool IsSettable()
        {
            return false;
        }

        public override bool Equals(object obj)
        {
            return obj is PathTupleItem rhs && index == rhs.index && ParentsEqual(parent, rhs.parent);
        }

        public override int GetHashCode()
        {
            return unchecked((0x50a7_000D * 31 + (parent?.GetHashCode() ?? 0)) * 31 + index);
        }
    }

    public class PathDictionaryPair : Path
    {
        private Path parent;
        private string key;

        public PathDictionaryPair(Path parent, string key)
        {
            this.parent = parent;
            this.key = key;
        }

        public override string Serialize()
        {
            return $"{parent.Serialize()}[{key}]";
        }

        public override bool IsValidForWriting()
        {
            return parent.IsValidForWriting();
        }

        internal override Path GetParent()
        {
            return parent;
        }

        internal override bool IsSettable()
        {
            return false;
        }

        public override bool Equals(object obj)
        {
            return obj is PathDictionaryPair rhs && key == rhs.key && ParentsEqual(parent, rhs.parent);
        }

        public override int GetHashCode()
        {
            return unchecked((0x50a7_000E * 31 + (parent?.GetHashCode() ?? 0)) * 31 + (key?.GetHashCode() ?? 0));
        }
    }

    public class PathDictionaryValue : Path
    {
        private Path parent;
        private string key;

        public PathDictionaryValue(Path parent, string key)
        {
            this.parent = parent;
            this.key = key;
        }

        public override string Serialize()
        {
            return $"{parent.Serialize()}[{key}]";
        }

        public override bool IsValidForWriting()
        {
            return parent.IsValidForWriting();
        }

        internal override Path GetParent()
        {
            return parent;
        }

        internal override bool IsSettable()
        {
            return false;
        }

        public override bool Equals(object obj)
        {
            return obj is PathDictionaryValue rhs && key == rhs.key && ParentsEqual(parent, rhs.parent);
        }

        public override int GetHashCode()
        {
            return unchecked((0x50a7_0007 * 31 + (parent?.GetHashCode() ?? 0)) * 31 + (key?.GetHashCode() ?? 0));
        }
    }

    // the ones after this point are grossly incomplete

    public class PathDictionaryKey : Path
    {
        private Path parent;

        public PathDictionaryKey(Path parent)
        {
            this.parent = parent;
        }

        public override string Serialize()
        {
            return $"{parent.Serialize()}[KEY]";
        }

        public override bool IsValidForWriting()
        {
            // not yet identifiable; I'm not sure how this even can work, frankly
            return false;
        }

        internal override Path GetParent()
        {
            return parent;
        }

        internal override bool IsSettable()
        {
            return false;
        }

        public override bool Equals(object obj)
        {
            return obj is PathDictionaryKey rhs && ParentsEqual(parent, rhs.parent);
        }

        public override int GetHashCode()
        {
            return unchecked(0x50a7_0008 * 31 + (parent?.GetHashCode() ?? 0));
        }
    }

    public class PathDictionaryValueUnpathable : Path
    {
        private Path parent;

        public PathDictionaryValueUnpathable(Path parent)
        {
            this.parent = parent;
        }

        public override string Serialize()
        {
            return $"{parent.Serialize()}[UNSERIALIZABLE]";
        }

        public override bool IsValidForWriting()
        {
            // if we have a usable key this can be done! but we're not right now
            return false;
        }

        internal override Path GetParent()
        {
            return parent;
        }

        internal override bool IsSettable()
        {
            return false;
        }

        public override bool Equals(object obj)
        {
            return obj is PathDictionaryValueUnpathable rhs && ParentsEqual(parent, rhs.parent);
        }

        public override int GetHashCode()
        {
            return unchecked(0x50a7_0009 * 31 + (parent?.GetHashCode() ?? 0));
        }
    }

    public class PathHashSetElement : Path
    {
        private Path parent;

        public PathHashSetElement(Path parent)
        {
            this.parent = parent;
        }

        public override string Serialize()
        {
            return $"{parent.Serialize()}[SETELEM]";
        }

        public override bool IsValidForWriting()
        {
            // not yet identifiable; I'm not sure how this even can work, frankly
            return false;
        }

        internal override Path GetParent()
        {
            return parent;
        }

        internal override bool IsSettable()
        {
            return false;
        }

        public override bool Equals(object obj)
        {
            return obj is PathHashSetElement rhs && ParentsEqual(parent, rhs.parent);
        }

        public override int GetHashCode()
        {
            return unchecked(0x50a7_000A * 31 + (parent?.GetHashCode() ?? 0));
        }
    }
}
