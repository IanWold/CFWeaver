namespace CFWeaver;

public record Warning(string Message, Line Line)
{
    public override string ToString() =>
        $"Warning (line {Line.Number} \"{Line.Text}\"): {Message}.";
}
