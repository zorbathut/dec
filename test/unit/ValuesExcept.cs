using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace DecTest
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class ValuesExceptAttribute : System.Attribute, IParameterDataSource
    {
        List<Enum> excepts = new List<Enum>();

        public ValuesExceptAttribute(params object[] arg1)
        {
            excepts.AddRange(arg1.Cast<Enum>());
        }

        public IEnumerable GetData(IParameterInfo parameter)
        {
            foreach (var except in excepts)
            {
                Assert.IsTrue(Enum.IsDefined(parameter.ParameterType, except), "Enum {0} does not contain value {1}", parameter.ParameterType, except);
            }

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
