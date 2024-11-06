namespace Dec
{
    [System.Diagnostics.DebuggerDisplay("{Serialize(),nq}")]
    public abstract class Path
    {
        public abstract string Serialize();

        public abstract bool IsValidForWriting();
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

        public override string Serialize()
        {
            return $"{decTypeName ?? decType.ComposeDecFormatted()}.{decName}";
        }

        public override bool IsValidForWriting()
        {
            return true;
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
    }

    public class PathDictionaryValue : Path
    {
        private Path parent;

        public PathDictionaryValue(Path parent)
        {
            this.parent = parent;
        }

        public override string Serialize()
        {
            return $"{parent.Serialize()}[nyi]";
        }

        public override bool IsValidForWriting()
        {
            // if we have a usable key this can be done! but we're not right now
            return false;
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
            return $"{parent.Serialize()}[KEY]";
        }

        public override bool IsValidForWriting()
        {
            // not yet identifiable; I'm not sure how this even can work, frankly
            return false;
        }
    }
}
