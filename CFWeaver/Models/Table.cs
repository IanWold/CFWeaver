using System.Text;

namespace CFWeaver;

public record Table(IEnumerable<Table.Row> Rows, IEnumerable<string> Columns)
{
    public record Row(IEnumerable<string> Cells)
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

    internal static Table From(IEnumerable<Result.Row> rows, IEnumerable<string> columns) =>
        new(
            rows.Select(row =>
                new Row(
                    columns.Select(col =>
                        row.Cells.FirstOrDefault(c => c.Column == col)?.Value
                        ?? (
                            row.Variables.TryGetValue(col, out var variable)
                            ? variable
                            : string.Empty
                        )
                    )
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
        .AppendLine("</tr>")
        .AppendDelegate(Rows.Select(r => (Action<StringBuilder>)r.AppendHtml))
        .AppendLine("</table>");

    internal void AppendMarkdown(StringBuilder sb) => sb
        .Append('|')
        .AppendJoin("|", Columns)
        .Append('|')
        .AppendJoin("|", Columns.Select(_ => "---"))
        .AppendLine("|---|")
        .AppendDelegate(Rows.Select(r => (Action<StringBuilder>)r.AppendMarkdown));
}
