namespace CFWeaver;

public abstract record Result(string Name, Dictionary<string, string> Variables)
{
    internal record Cell(string Column, string Value);
    internal record Row(IEnumerable<Cell> Cells, Dictionary<string, string> Variables);

    internal abstract IEnumerable<Row> ResultsTable(string step);
}

