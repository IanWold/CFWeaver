using System.Diagnostics.CodeAnalysis;
using System.Text;
using Xunit.Sdk;
using Xunit.v3;

[assembly: Parallelization(Mode = ParallelMode.None)]

namespace CFWeaver.Tests;

file class FakeFileSystem(Dictionary<string, string> inputFiles) : FileSystem
{
    public Dictionary<string, string> OutputFiles { get; init; } = [];

    public override bool TryReadAllText(string path, [NotNullWhen(true)] out string? content) =>
        inputFiles.TryGetValue(path, out content);

    public override bool DirectoryExists(string path) =>
        inputFiles.Keys.Any(k => k.Contains(path));

    public override void CreateDirectory(string path) { }

    public override Task WriteAllTextAsync(string path, string content) =>
        Task.FromResult(OutputFiles.TryAdd(path, content));
}

public static class Runner
{
    public static async Task<RunResult> RunTestAsync(string args, params IEnumerable<(string path, string content)> inputFiles)
    {
        var output = new StringBuilder();
        var error = new StringBuilder();
        var files = new FakeFileSystem(inputFiles.ToDictionary(f => f.path, f => f.content));

        Environment.ExitCode = 0;
        await new App(args.Split(' '), new StringWriter(output), new StringWriter(error), files).RunAsync();

        return new(output.ToString(), error.ToString(), files.OutputFiles, Environment.ExitCode);
    }
}
