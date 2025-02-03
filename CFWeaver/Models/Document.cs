using System.Text;

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
}
