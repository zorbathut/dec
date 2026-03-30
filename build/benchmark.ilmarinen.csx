Step("benchmark")
    .Image("mcr.microsoft.com/dotnet/sdk:10.0")
    .Run(async ctx => {
        var describeResult = await ctx.TryShell("cd /workspace && git describe --always --dirty");
        var describe = describeResult.Stdout.Trim();

        await ctx.Shell($@"
            cd /workspace
            dotnet run --project extra/benchmark/benchmark.csproj -c Release -- --artifacts /workspace/benchmark-{describe}
        ");

        await ctx.Shell($"echo '{describe}' > /workspace/benchmark-{describe}/results/git-describe.txt");
        await ctx.Shell($"tar -czf /workspace/benchmark-{describe}.tar.gz -C /workspace/benchmark-{describe} results/");
        var artifact = await ctx.SaveArtifact($"/workspace/benchmark-{describe}.tar.gz");
        Console.WriteLine($"Saved artifact: {artifact.Name} ({artifact.Size} bytes)");
    });
