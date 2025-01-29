namespace CFWeaver;

public abstract record Result(string Name, string? Condition)
{
    internal record Cell(string Column, string Value);
    internal record Row(IEnumerable<Cell> Cells, IEnumerable<string> Conditions);

    internal abstract IEnumerable<Row> ResultsTable(string step);

    protected IEnumerable<string> EnumeratedCondition =>
        Condition is not null
        ? [Condition]
        : [];
}

