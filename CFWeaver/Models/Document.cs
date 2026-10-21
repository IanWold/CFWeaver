using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CFWeaver;

public record Document(IEnumerable<Operation> Operations, string Description = "")
{
    public string Html(bool selfContained = false) => new StringBuilder()
        .AppendLine("<!DOCTYPE html><html>")
        .Append($"<head><meta charset=\"utf-8\"><meta name=\"generator\" content=\"{Tool.Name}\">")
        .Append($"<style>{Assets.StyleSheet}{Environment.NewLine}th, td {{ text-align: left !important; }}</style>")
        .Append(selfContained ? $"<script>{Assets.Mermaid}</script>" : $"<script src=\"{Assets.MermaidUrl}\"></script>")
        .AppendLine("</head>")
        .AppendLine("<body><nav style=\"justify-content:center;\"><ul>")
        .AppendJoin(Environment.NewLine, Operations.Select(o => o.NavigationHtml))
        .AppendLine("</ul></nav>")
        .Append(Description.Length > 0 ? $"<header>{Description.MarkdownToHtml()}</header>" : "")
        .AppendDelegate(Operations.Select(o => (Action<StringBuilder>)(sb => o.AppendHtml(sb, Operations))))
        .AppendLine("<script>mermaid.initialize({ startOnLoad: true }); mermaid.init();</script></body></html>")
        .ToString();

    public string Markdown() => new StringBuilder()
        .Append(Description.Length > 0 ? $"{Description.ReplaceLineEndings()}{Environment.NewLine}{Environment.NewLine}" : "")
        .AppendDelegate(Operations.Select(o => (Action<StringBuilder>)(sb => o.AppendMarkdown(sb, Operations))))
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
                operation.WriteJson(writer, Operations);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    internal IEnumerable<Warning> Warnings =>
        Operations.SelectMany(o => o.Warnings.Concat(o.SubOperationWarnings(Operations)).OrderBy(w => w.Line.Number));
}
