using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CFWeaver;

public record Document(IEnumerable<Operation> Operations, string Description = "")
{
    public string Html() => new StringBuilder()
        .AppendLine("<!DOCTYPE html><html>")
        .AppendLine($"<head><meta charset=\"utf-8\"><meta name=\"generator\" content=\"{Tool.Name}\"><link rel=\"stylesheet\" href=\"https://unpkg.com/mvp.css@1.18.0\"><style>th, td {{ text-align: left !important; }}</style><script src=\"https://cdn.jsdelivr.net/npm/mermaid@12.2.0/dist/mermaid.min.js\"></script></head>")
        .AppendLine("<body><nav style=\"justify-content:center;\"><ul>")
        .AppendJoin(Environment.NewLine, Operations.Select(o => o.NavigationHtml))
        .AppendLine("</ul></nav>")
        .Append(Description.Length > 0 ? $"<header>{Description.MarkdownToHtml()}</header>" : "")
        .AppendDelegate(Operations.Select(o => (Action<StringBuilder>)o.AppendHtml))
        .AppendLine("<script>mermaid.initialize({ startOnLoad: true }); mermaid.init();</script></body></html>")
        .ToString();

    public string Markdown() => new StringBuilder()
        .Append(Description.Length > 0 ? $"{Description.ReplaceLineEndings()}{Environment.NewLine}{Environment.NewLine}" : "")
        .AppendDelegate(Operations.Select(o => (Action<StringBuilder>)o.AppendMarkdown))
        .ToString();

    public string Json()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new() { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            writer.WriteStartObject();
            writer.WriteString("generator", Tool.Name);
            writer.WriteString("description", Description);
            writer.WriteStartArray("operations");
            foreach (var operation in Operations)
            {
                operation.WriteJson(writer);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    internal IEnumerable<Warning> Warnings =>
        Operations.SelectMany(o => o.Warnings.Concat(o.SubOperationWarnings(Operations)).OrderBy(w => w.Line.Number));
}
