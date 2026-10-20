using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CFWeaver;

public record Document(IEnumerable<Operation> Operations)
{
    public string Html() => new StringBuilder()
        .AppendLine("<!DOCTYPE html><html>")
        .AppendLine("<head><link rel=\"stylesheet\" href=\"https://unpkg.com/mvp.css\"><style>th, td { text-align: left !important; }</style><script src=\"https://cdn.jsdelivr.net/npm/mermaid/dist/mermaid.min.js\"></script></head>")
        .AppendLine("<body><nav style=\"justify-content:center;\"><ul>")
        .AppendJoin("\r\n", Operations.Select(o => $"<li><a href=\"#{o.Name.Replace(" ", "-")}\">{o.Name}</a></li>"))
        .AppendLine("</ul></nav>")
        .AppendDelegate(Operations.Select(o => (Action<StringBuilder>)o.AppendHtml))
        .AppendLine("<script>mermaid.initialize({ startOnLoad: true }); mermaid.init();</script></body></html>")
        .ToString();

    public string Markdown() => new StringBuilder()
        .AppendDelegate(Operations.Select(o => (Action<StringBuilder>)o.AppendMarkdown))
        .ToString();

    public string Json()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new() { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            writer.WriteStartObject();
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
        Operations.SelectMany(o => o.Warnings);
}
