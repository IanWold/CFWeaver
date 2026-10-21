using System.IO.Compression;

namespace CFWeaver;

static class Assets
{
    const string MermaidVersion = "12.2.0";
    const string StyleSheetVersion = "1.18.0";

    internal static string MermaidUrl =>
        $"https://cdn.jsdelivr.net/npm/mermaid@{MermaidVersion}/dist/mermaid.min.js";

    internal static string Mermaid =>
        WithLicense($"mermaid-{MermaidVersion}.LICENSE", Read($"mermaid-{MermaidVersion}.min.js.gz"));

    internal static string StyleSheet =>
        WithLicense($"mvp-{StyleSheetVersion}.LICENSE", Read($"mvp-{StyleSheetVersion}.css"));

    internal static string ModelingRecipes =>
        Read("Modeling-Recipes.md");

    internal static string ParserErrors =>
        Read("Parser-Errors.md");

    static string WithLicense(string license, string content) =>
        $"/*!{Environment.NewLine}{Read(license)}*/{Environment.NewLine}{content}";

    static string Read(string name)
    {
        using var resource = typeof(Assets).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException($"Missing embedded asset {name}");
        using var stream = name.EndsWith(".gz") ? new GZipStream(resource, CompressionMode.Decompress) : resource;
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
