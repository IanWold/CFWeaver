using System.Diagnostics.CodeAnalysis;
using System.Text;
using Files = System.Collections.Generic.Dictionary<string, string>;

namespace CFWeaver.Tests;

file class FakeFileSystem(Files inputFiles) : FileSystem
{
    public Files OutputFiles { get; init; } = [];

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
    public static async Task<(string console, Files files)> RunTestAsync(string[] args, Files inputFiles)
    {
        var sb = new StringBuilder();
        var files = new FakeFileSystem(inputFiles);

        await new App(args, new StringWriter(sb), files).RunAsync();

        return (sb.ToString(), files.OutputFiles);
    }
}
