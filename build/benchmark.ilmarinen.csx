Step("benchmark")
    .Image("mcr.microsoft.com/dotnet/sdk:10.0")
    .Run(async ctx => {
        await ctx.Shell(@"
            cd /workspace
            dotnet run --project extra/benchmark/benchmark.csproj -c Release -- --artifacts /workspace/benchmark-results
        ");

        await ctx.Shell("tar -czf /workspace/benchmark-results.tar.gz -C /workspace/benchmark-results results/");
        var artifact = await ctx.SaveArtifact("/workspace/benchmark-results.tar.gz");
        Console.WriteLine($"Saved artifact: {artifact.Name} ({artifact.Size} bytes)");
    });
