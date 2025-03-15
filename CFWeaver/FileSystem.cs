using System.Diagnostics.CodeAnalysis;

namespace CFWeaver;

public class FileSystem
{
    public virtual bool TryReadAllText(string path, [NotNullWhen(true)] out string? content) =>
        (content = File.Exists(path) ? File.ReadAllText(path) : null)
        is not null;

    public virtual string? GetDirectoryName(string path) =>
        Path.GetDirectoryName(path);

    public virtual bool DirectoryExists(string path) =>
        Directory.Exists(path);

    public virtual void CreateDirectory(string path) =>
        Directory.CreateDirectory(path);

    public virtual async Task WriteAllTextAsync(string path, string content) =>
        await File.WriteAllTextAsync(path, content);
}
