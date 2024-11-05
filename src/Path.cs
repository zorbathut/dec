namespace Dec
{
    public abstract class Path
    {
        public abstract string Serialize();
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
    }
}
