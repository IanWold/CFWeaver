using System.Text;

namespace CFWeaver;

public record Table(IEnumerable<Table.Row> Rows, IEnumerable<string> Columns)
{
    public record Row(IEnumerable<string> Cells, IEnumerable<string> Conditions)
    {
        internal void AppendCsv(StringBuilder sb) => sb
            .AppendLine()
            .AppendJoin(",", Cells);

        internal void AppendHtml(StringBuilder sb) => sb
            .AppendLine("<tr>")
            .AppendJoin("", Cells.Select(c => $"<td>{c}</td>"))
            .Append("<td>")
            .AppendJoin(" and ", Conditions)
            .AppendLine("</td>");

        internal void AppendMarkdown(StringBuilder sb) => sb
            .Append('|')
            .AppendJoin("|", Cells)
            .Append('|')
            .AppendJoin(" and ", Conditions)
            .AppendLine("|");
    }

    internal static Table From(IEnumerable<Result.Row> rows, IEnumerable<string> columns) =>
        new(
            rows.Select(row =>
                new Row(
                    columns.Select(col => row.Cells.FirstOrDefault(c => c.Column == col)?.Value ?? string.Empty),
                    row.Conditions
                )
            ),
            columns
        );

    internal void AppendCsv(StringBuilder sb) => sb
        .AppendJoin(",", Columns)
        .AppendDelegate(Rows.Select(r => (Action<StringBuilder>)r.AppendCsv));

    internal void AppendHtml(StringBuilder sb) => sb
        .AppendLine("<table><tr>")
        .AppendJoin("", Columns.Select(c => $"<th>{c}</th>"))
        .AppendLine("<th>Conditions</th></tr>")
        .AppendDelegate(Rows.Select(r => (Action<StringBuilder>)r.AppendHtml))
        .AppendLine("</table>");

    internal void AppendMarkdown(StringBuilder sb) => sb
        .Append('|')
        .AppendJoin("|", Columns)
        .AppendLine("|Conditions|")
        .Append('|')
        .AppendJoin("|", Columns.Select(_ => "---"))
        .AppendLine("|---|")
        .AppendDelegate(Rows.Select(r => (Action<StringBuilder>)r.AppendMarkdown));
}
