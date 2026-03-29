using BenchmarkDotNet.Running;

var switcher = BenchmarkSwitcher.FromAssembly(typeof(DecBenchmark.BenchmarkBase).Assembly);
if (!args.Any(a => a == "--filter"))
    args = args.Prepend("--filter").Append("*").ToArray();
switcher.Run(args);
