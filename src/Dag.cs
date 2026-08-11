
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dec
{
    /// <summary>
    /// A simple stable Dag evaluation class, made public for utility usage.
    /// </summary>
    /// <remarks>
    /// This isn't *really* part of Dec, it's just here for convenience because Dec needs it.
    /// </remarks>
    public static class Dag<T>
    {
        /// <summary>
        /// Information on a single dag dependency.
        /// </summary>
        public struct Dependency
        {
            public T before;
            public T after;
        }

        private enum Status
        {
            Unvisited,
            Visiting,
            Visited
        }

        /// <summary>
        /// Given an input list and a list of dependencies, calculate a stable order.
        /// </summary>
        /// <remarks>
        /// This is guaranteed to return the same order every run.
        ///
        /// Some effort is made to minimize output changes if more items are added or more dependencies are added.
        ///
        /// Output may change after Dec is updated; this is not guaranteed stable between versions!
        /// </remarks>
        public static List<T> CalculateOrder<U>(IEnumerable<T> input, List<Dependency> dependencies, Func<T, U> tiebreaker)  where U : IComparable<U>
        {
            // OrderBy is a stable sort, which is important for us
            var inputOrder = input.OrderBy(tiebreaker).ToArray();
            var seen = new Status[inputOrder.Length];

            var indexLookup = new Dictionary<T, int>();
            for (int i = 0; i < inputOrder.Length; i++)
            {
                indexLookup[inputOrder[i]] = i;
            }

            List<List<int>> dependenciesCompiled = new List<List<int>>();
            foreach (var t in inputOrder)
            {
                // unnecessary allocations but whatever
                dependenciesCompiled.Add(new List<int>());
            }

            foreach (var dep in dependencies)
            {
                if (!indexLookup.TryGetValue(dep.before, out int beforeIndex) || !indexLookup.TryGetValue(dep.after, out int afterIndex))
                {
                    Dbg.Err($"Dependency references an item not in the input list: {dep.before} -> {dep.after}. If you want this to work, go pester Zorba on Discord.");
                    continue;
                }

                dependenciesCompiled[afterIndex].Add(beforeIndex);
            }

            List<T> result = new List<T>();
            for (int i = 0; i < inputOrder.Length; i++)
            {
                Visit(inputOrder, i, seen, dependenciesCompiled, result);
            }

            return result;
        }

        private static void Visit(T[] inputOrder, int i, Status[] status, List<List<int>> dependenciesCompiled, List<T> result)
        {
            if (status[i] == Status.Visited)
            {
                return;
            }

            if (status[i] == Status.Visiting)
            {
                Dbg.Err($"Cycle detected in dependency graph involving {inputOrder[i]}");
                return;
            }

            status[i] = Status.Visiting;

            foreach (var depIndex in dependenciesCompiled[i])
            {
                Visit(inputOrder, depIndex, status, dependenciesCompiled, result);
            }

            status[i] = Status.Visited;

            result.Add(inputOrder[i]);
        }
    }
}