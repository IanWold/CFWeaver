namespace StateFusion;

public record Error(int Code, string Message, Line? Line = null)
{
    string LineString =>
        Line is Line line
        ? $" (line {line.Number} \"{line.Text}\")"
        : string.Empty;

    public override string ToString() =>
        $"Error {Code}{LineString}: {Message}.";
}
