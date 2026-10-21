namespace CFWeaver;

public record Warning(int Code, string Message, Line Line)
{
    public override string ToString() =>
        $"Warning {Code} (line {Line.Number} \"{Line.Text}\"): {Message}.";
}
