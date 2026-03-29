Step("benchmark")
    .Image("mcr.microsoft.com/dotnet/sdk:9.0")
    .Run(async ctx => {
        await ctx.Shell(@"
            cd /workspace
            dotnet run --project extra/benchmark/benchmark.csproj -c Release -- --artifacts /workspace/benchmark-results
        ");

        await ctx.Shell("apt-get update -qq && apt-get install -y -qq zip > /dev/null");
        await ctx.Shell("cd /workspace/benchmark-results && zip -r /workspace/benchmark-results.zip results/");
        var artifact = await ctx.SaveArtifact("/workspace/benchmark-results.zip");
        Console.WriteLine($"Saved artifact: {artifact.Name} ({artifact.Size} bytes)");
    });
