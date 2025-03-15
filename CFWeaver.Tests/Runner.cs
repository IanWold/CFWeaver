using System.Diagnostics.CodeAnalysis;
using System.Text;

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
    public static async Task<(string console, Dictionary<string, string> files)> RunTestAsync(string args, params IEnumerable<(string path, string content)> inputFiles)
    {
        var sb = new StringBuilder();
        var files = new FakeFileSystem(inputFiles.ToDictionary(f => f.path, f => f.content));

        await new App(args.Split(' '), new StringWriter(sb), files).RunAsync();

        return (sb.ToString(), files.OutputFiles);
    }
}
