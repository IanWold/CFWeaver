using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace CFWeaver;

file record ResultAccumulator<T>(IEnumerable<T> Successes, IEnumerable<Error> Errors);

public static partial class Extensions
{

    [GeneratedRegex("`([^`]+)`")]
    private static partial Regex InlineCode();

    internal static ParseResult<U> Coalesce<T, U>(this IEnumerable<ParseResult<T>> results, Func<IEnumerable<T>, U> coalesce)
    {
        var aggregation = results.Aggregate<ParseResult<T>, ResultAccumulator<T>>(new([],[]), (accumulator, result) => result.Map(
            success: t => accumulator with { Successes = [..accumulator.Successes, t] },
            failure: errors => accumulator with { Errors = [..accumulator.Errors, ..errors] }
        ));

        return aggregation.Errors.Any()
            ? new FailureResult(aggregation.Errors)
            : coalesce(aggregation.Successes);
    }
    
    internal static StringBuilder AppendDelegate(this StringBuilder sb, params IEnumerable<Action<StringBuilder>> delegates)
    {
        foreach (var appender in delegates)
        {
            appender(sb);
        }

        return sb;
    }

    public static string PrettyPrint(this IEnumerable<Error> errors) =>
        string.Join("\n", errors);

    internal static string HtmlEncode(this string text) =>
        WebUtility.HtmlEncode(text);

    internal static string CsvField(this string text) =>
        text.IndexOfAny([',', '"', '\r', '\n']) >= 0
        ? $"\"{text.Replace("\"", "\"\"")}\""
        : text;

    internal static string MarkdownCell(this string text) =>
        text.Replace("|", "\\|");

    internal static string MarkdownToHtml(this string markdown)
    {
        var sb = new StringBuilder();
        var paragraph = new List<string>();
        var inList = false;

        foreach (var line in markdown.ReplaceLineEndings("\n").Split('\n').Select(l => l.Trim()))
        {
            if (line.StartsWith("- ") || line == "-")
            {
                CloseParagraph();
                if (!inList)
                {
                    sb.AppendLine("<ul>");
                    inList = true;
                }

                sb.AppendLine($"<li>{Inline(line[1..].Trim())}</li>");
            }
            else if (line.Length == 0)
            {
                CloseParagraph();
                CloseList();
            }
            else
            {
                CloseList();
                paragraph.Add(line);
            }
        }

        CloseParagraph();
        CloseList();

        return sb.ToString();

        void CloseParagraph()
        {
            if (paragraph.Count > 0)
            {
                sb.AppendLine($"<p>{Inline(string.Join(" ", paragraph))}</p>");
                paragraph.Clear();
            }
        }

        void CloseList()
        {
            if (inList)
            {
                sb.AppendLine("</ul>");
                inList = false;
            }
        }

        static string Inline(string text) =>
            InlineCode().Replace(text.HtmlEncode(), "<code>$1</code>");
    }
}
