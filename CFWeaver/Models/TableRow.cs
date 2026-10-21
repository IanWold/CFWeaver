using System.Text;

namespace CFWeaver;

public record TableRow(IEnumerable<string> Cells)
{
    internal void AppendCsv(StringBuilder sb) => sb
        .AppendJoin(",", Cells.Select(c => c.CsvField()))
        .AppendLine();

    internal void AppendHtml(StringBuilder sb) => sb
        .AppendLine("<tr>")
        .AppendJoin("", Cells.Select(c => $"<td>{c.InlineMarkdownToHtml()}</td>"))
        .AppendLine("</tr>");

    internal void AppendMarkdown(StringBuilder sb) => sb
        .Append('|')
        .AppendJoin("|", Cells.Select(c => c.MarkdownCell()))
        .AppendLine("|");
}
