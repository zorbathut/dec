using System.Linq;
using BenchmarkDotNet.Running;

var switcher = BenchmarkSwitcher.FromAssembly(typeof(DecBenchmark.BenchmarkBase).Assembly);
if (!args.Any(a => a == "--filter" || a == "-f"))
    args = new[] { "--filter", "*" }.Concat(args).ToArray();
switcher.Run(args);
