namespace Def
{
    using System.Collections.Generic;

    public class Def
    {
        public string defName;

        public int index;

        public override string ToString()
        {
            return defName;
        }

        public virtual void PostLoad()
        {

        }

        public virtual IEnumerable<string> ConfigErrors()
        {
            yield break;
        }
    }
}
