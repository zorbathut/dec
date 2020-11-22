namespace DefTest
{
    using NUnit.Framework.Interfaces;
    using System;
    using System.Collections;
    using System.Collections.Generic;

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class ValuesExceptAttribute : System.Attribute, IParameterDataSource
    {
        Enum arg1;

        public ValuesExceptAttribute(object arg1)
        {
            this.arg1 = (Enum)arg1;
        }

        public IEnumerable GetData(IParameterInfo parameter)
        {
            foreach (var value in Enum.GetValues(parameter.ParameterType))
            {
                if (value.Equals(arg1))
                {
                    continue;
                }

                yield return value;
            }
        }
    }
}
