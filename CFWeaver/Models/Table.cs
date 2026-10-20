using System.Text;

namespace CFWeaver;

public record Table(IEnumerable<TableRow> Rows, IEnumerable<string> Columns)
{
    internal void AppendCsv(StringBuilder sb) => sb
        .AppendJoin(",", Columns)
        .AppendDelegate(Rows.Select(r => (Action<StringBuilder>)r.AppendCsv));

    internal void AppendHtml(StringBuilder sb) => sb
        .AppendLine("<table><tr>")
        .AppendJoin("", Columns.Select(c => $"<th>{c}</th>"))
        .AppendLine("</tr>")
        .AppendDelegate(Rows.Select(r => (Action<StringBuilder>)r.AppendHtml))
        .AppendLine("</table>");

    internal void AppendMarkdown(StringBuilder sb) => sb
        .Append('|')
        .AppendJoin("|", Columns)
        .AppendLine("|")
        .Append('|')
        .AppendJoin("|", Columns.Select(_ => "---"))
        .AppendLine("|")
        .AppendDelegate(Rows.Select(r => (Action<StringBuilder>)r.AppendMarkdown));
}
