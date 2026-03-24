using BenchmarkDotNet.Running;

var switcher = BenchmarkSwitcher.FromAssembly(typeof(DecBenchmark.BenchmarkBase).Assembly);
if (args.Length == 0)
    switcher.RunAll();
else
    switcher.Run(args);
