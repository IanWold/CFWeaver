using System.Text;

namespace CFWeaver;

public record TableRow(IEnumerable<string> Cells)
{
    internal void AppendCsv(StringBuilder sb) => sb
        .AppendLine()
        .AppendJoin(",", Cells);

    internal void AppendHtml(StringBuilder sb) => sb
        .AppendLine("<tr>")
        .AppendJoin("", Cells.Select(c => $"<td>{c}</td>"))
        .AppendLine("</tr>");

    internal void AppendMarkdown(StringBuilder sb) => sb
        .Append('|')
        .AppendJoin("|", Cells)
        .AppendLine("|");
}
