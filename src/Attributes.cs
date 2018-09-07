namespace Def
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    [AttributeUsage(AttributeTargets.Class)]  
    public class StaticReferences : Attribute
    {
        internal static List<Type> StaticReferencesFilled = new List<Type>();

        // We use stack black magic to name the class, so we need to make sure it isn't inlined
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Initialized()
        {
            Parser.StaticReferencesInitialized();
        }
    }
}
