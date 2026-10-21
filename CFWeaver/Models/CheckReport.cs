using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CFWeaver;

public record CheckReport(IEnumerable<Error> Errors, IEnumerable<Warning> Warnings, IEnumerable<Operation> Operations)
{
    public bool IsWellFormed => !Errors.Any();

    public string Summary()
    {
        var operations = Operations.Count();
        var scenarios = Operations.Sum(o => o.Scenarios.Count);

        return new StringBuilder()
            .AppendLine($"Model is well-formed: {Count(operations, "operation")}, {Count(scenarios, "scenario")}.")
            .AppendJoin(Environment.NewLine, Operations.Select(Line))
            .ToString();

        string Line(Operation operation) =>
            operation.CalledBy(Operations).Select(o => o.Name).ToArray() is [_, ..] callers
            ? $"  {operation.Name}: {Count(operation.Scenarios.Count, "scenario")}, called by {callers.JoinAnd()}"
            : $"  {operation.Name}: {Count(operation.Scenarios.Count, "scenario")}";

        static string Count(int count, string noun) =>
            $"{count} {noun}{(count == 1 ? "" : "s")}";
    }

    public string Json()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new() { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            writer.WriteStartObject();
            writer.WriteString("generator", Tool.Name);
            writer.WriteBoolean("wellFormed", IsWellFormed);

            writer.WriteStartArray("operations");
            foreach (var operation in Operations)
            {
                writer.WriteStartObject();
                writer.WriteString("name", operation.Name);
                writer.WriteNumber("scenarios", operation.Scenarios.Count);
                writer.WriteStartArray("calledBy");
                foreach (var caller in operation.CalledBy(Operations))
                {
                    writer.WriteStringValue(caller.Name);
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WriteStartArray("diagnostics");
            foreach (var error in Errors)
            {
                WriteDiagnostic("error", error.Code, error.Message, error.Line);
            }
            foreach (var warning in Warnings)
            {
                WriteDiagnostic("warning", warning.Code, warning.Message, warning.Line);
            }
            writer.WriteEndArray();

            writer.WriteEndObject();

            void WriteDiagnostic(string severity, int code, string message, Line? line)
            {
                writer.WriteStartObject();
                writer.WriteString("severity", severity);
                writer.WriteNumber("code", code);

                if (line is Line l)
                {
                    writer.WriteNumber("line", l.Number);
                    writer.WriteString("text", l.Text);
                }
                else
                {
                    writer.WriteNull("line");
                    writer.WriteNull("text");
                }
                
                writer.WriteString("message", message);
                writer.WriteEndObject();
            }
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
