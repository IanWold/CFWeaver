using System.Globalization;
using System.Text.Json;

namespace CFWeaver;

public record Scenario(IReadOnlyList<Scenario.Choice> Path, string Response, IReadOnlyList<string> Conditions, Dictionary<string, string> Variables)
{
    public record Choice(string Step, string Result);

    internal void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();

        writer.WriteStartArray("path");
        foreach (var choice in Path)
        {
            writer.WriteStartObject();
            writer.WriteString("step", choice.Step);
            writer.WriteString("result", choice.Result);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        if (long.TryParse(Response, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number)
            && number.ToString(CultureInfo.InvariantCulture) == Response)
        {
            writer.WriteNumber("response", number);
        }
        else
        {
            writer.WriteString("response", Response);
        }

        writer.WriteStartArray("conditions");
        foreach (var condition in Conditions)
        {
            writer.WriteStringValue(condition);
        }
        writer.WriteEndArray();

        writer.WriteStartObject("variables");
        foreach (var (name, value) in Variables)
        {
            writer.WriteString(name, value);
        }
        writer.WriteEndObject();

        writer.WriteEndObject();
    }
}
