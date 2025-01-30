using System.Text;

namespace CFWeaver;

public record Document(IEnumerable<Endpoint> Endpoints)
{
    public string Html() => new StringBuilder()
        .AppendLine("<!DOCTYPE html><html>")
        .AppendLine("<head><link rel=\"stylesheet\" href=\"https://unpkg.com/mvp.css\"><style>th, td { text-align: left !important; }</style><script src=\"https://cdn.jsdelivr.net/npm/mermaid/dist/mermaid.min.js\"></script></head>")
        .AppendLine("<body><nav style=\"justify-content:center;\"><ul>")
        .AppendJoin("\r\n", Endpoints.Select(e => $"<li><a href=\"#{e.Name.Replace(" ", "-")}\">{e.Name}</a></li>"))
        .AppendLine("</ul></nav>")
        .AppendDelegate(Endpoints.Select(e => (Action<StringBuilder>)e.AppendHtml))
        .AppendLine("<script>mermaid.initialize({ startOnLoad: true }); mermaid.init();</script></body></html>")
        .ToString();

    public string Markdown() => new StringBuilder()
        .AppendDelegate(Endpoints.Select(e => (Action<StringBuilder>)e.AppendMarkdown))
        .ToString();
}
