using System;
using System.Collections.Generic;

namespace Loaf
{
    public static class Util
    {
        public static T MaxOfOrDefault<T>(this IEnumerable<T> data, Func<T, float> func)
        {
            var result = default(T);
            float resultValue = float.MinValue;
            foreach (var elem in data)
            {
                float thisValue = func(elem);
                if (thisValue >= resultValue)
                {
                    result = elem;
                    resultValue = thisValue;
                }
            }

            return result;
        }
    }
}