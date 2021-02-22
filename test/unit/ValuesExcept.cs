namespace DecTest
{
    using NUnit.Framework.Interfaces;
    using System;
    using System.Collections;
    using System.Collections.Generic;

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class ValuesExceptAttribute : System.Attribute, IParameterDataSource
    {
        List<Enum> excepts = new List<Enum>();

        public ValuesExceptAttribute(object arg1)
        {
            excepts.Add((Enum)arg1);
        }

        public ValuesExceptAttribute(object arg1, object arg2)
        {
            excepts.Add((Enum)arg1);
            excepts.Add((Enum)arg2);
        }

        public IEnumerable GetData(IParameterInfo parameter)
        {
            foreach (var value in Enum.GetValues(parameter.ParameterType))
            {
                if (excepts.Contains((Enum)value))
                {
                    continue;
                }

                yield return value;
            }
        }
    }
}
