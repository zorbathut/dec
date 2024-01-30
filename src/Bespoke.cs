using System;
using System.Collections.Generic;
using System.Text;

namespace Dec.Bespoke
{
    // This is used only to disable the .Record() function during parsing.
    // You should probably not use this unless you know exactly what it's for.
    [AttributeUsage(AttributeTargets.Method)]
    [Obsolete("This will be removed shortly. Reimplement this functionality in terms of IUserSettings and IConditionalRecordable.")]
    public class IgnoreRecordDuringParserAttribute : Attribute { }
}
