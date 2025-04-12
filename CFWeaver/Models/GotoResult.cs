namespace CFWeaver;

public record GotoResult(string Name, Dictionary<string, string> Variables, Step Goto) : Result(Name, Variables)
{
    internal override IEnumerable<Row> ResultsTable(string step) =>
        Goto.ResultTable.Select(t => new Row([new(step, Name), ..t.Cells], Reconcile(Variables, t.Variables)));

    static Dictionary<string, string> Reconcile(Dictionary<string, string> into, Dictionary<string, string> from)
    {
        var onlyFrom = from.Where(f => !into.ContainsKey(f.Key));
        var fromForInto = from.Where(f => into.ContainsKey(f.Key));
        var onlyInto = into.Where(i => !from.ContainsKey(i.Key));
        var intoForFrom = into.Where(i => from.ContainsKey(i.Key));

        var all = onlyInto.Concat(onlyFrom).Concat(intoForFrom.Select(i =>
            i.Key == "Condition"
            ? new KeyValuePair<string, string>("Condition", $"{i.Value} and {from["Condition"]}")
            : fromForInto.Single(f => f.Key == i.Key)
        ));

        return all.ToDictionary();
    }
}
