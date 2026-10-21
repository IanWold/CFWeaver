using System.Reflection;

namespace CFWeaver;

static class Tool
{
    public const string ExampleModel =
        """
        # Get an Item

        * Authenticate: Success | Failure => 403 ? the caller is not authenticated
        * Validate: Success | Failure => 400 ? the request is invalid
        * GetFromDatabase: Found ? the item exists | Not found => 404 ? the item does not exist
        * Authorize: Success => 200 | Failure => 401 ? the caller can't access the item & Audited=yes
        """;

    public const string ExampleTable =
        """
        |Authenticate|Validate|GetFromDatabase|Authorize|Respond|Condition|Audited|
        |---|---|---|---|---|---|---|
        |Success|Success|Found|Success|200|the item exists||
        |Success|Success|Found|Failure|401|the item exists and the caller can't access the item|yes|
        |Success|Success|Not found||404|the item does not exist||
        |Success|Failure|||400|the request is invalid||
        |Failure||||403|the caller is not authenticated||
        """;

    public static string Guide =>
        $"""
        CFWeaver reads a control flow state model (CFSM) written in Markdown and generates one test
        scenario for every path through it.

        Model syntax:
          The input is a Markdown file.

          # Operation name        Each # heading starts an operation, which gets its own scenario table.
          * Step: A | B           Each * line under it is a step, a decision point, in the order the steps
                                  run. Step names can't contain spaces and must be unique in the operation.
          * Step                  Results can also be listed below a step, one per - line, instead of
              - Result            after a colon.
              - Result => 404

        Results:
          What a result does next depends on what follows its name:
            Success               Nothing: continue to the next step.
            Failure => 404        "=> <value>": the operation returns this value, which can be any text,
                                  such as 404 or throws NotFoundException.
            Found = UpdateItem    "= <step name>": go to that step. It must come later in the operation;
                                  loops aren't supported.
          Every result on the last step must return a value with "=>".

        Conditions and variables:
          After a result's name and value, "?" starts its details, which are separated by "&":
            Failure => 403 ? the caller is not authenticated & Audited=yes
            Success ? Audited=no
          A detail written name=value, where the name is one word (letters, digits, _, - or .), sets a
          variable. Any other detail is the result's condition, and a result has at most one. Conditions
          along a path are joined with "and"; a variable shows the last value assigned along the path.

        Reserved characters:
          "|" separates results and "&" separates details, so neither can appear in a result name,
          condition or value. A result name or value can't contain "=" or "?"; after the "?", they're
          ordinary text. Other punctuation, such as commas, quotes and "<", is fine anywhere: the
          outputs escape it.

        Descriptions:
          Text and - lists between an operation's heading and its first step describe the operation,
          and text before the first operation describes the whole file. Descriptions are copied into
          the output, so they're a good place for sources and assumptions. After a step, - lines are
          its results, and any other text is ignored.

        Sub-operations:
          A step with the same name as another operation is read as calling it, so its results should
          be that operation's return values. CFWeaver warns if the step leaves one of them out, or has
          a result the operation never returns. Each operation keeps its own scenario table:
            # Get Item
            * Authenticate: Valid | Invalid => 401
            * Lookup: Found => 200 | NotFound => 404

            # Authenticate
            * CheckToken: Valid | Missing => Invalid
            * LoadUser: Found => Valid | NotFound => Invalid

        Output:
          One row per path: the result taken at each step (blank where the path skipped the step), the
          returned value (Respond), the joined conditions, and one column per variable. Use -f md for a
          Markdown table, or -f json for each scenario's path, response, conditions and variables; the
          response is a number if the returned value is an integer. Use -o - to write to stdout
          instead of a file.

          Use --check instead of -o to only check that the model is well-formed: it prints the number
          of scenarios in each operation, or the errors and warnings, and with -f json it prints them
          as JSON. A well-formed model isn't necessarily an accurate one; --check doesn't compare the
          model with the system it describes.

          If the model has errors, they're written to stderr with a code and line number, no output
          is written, and cfweaver exits with code 1. Warnings, such as a step that no path reaches,
          are also written to stderr but don't stop the output.

        Example:
          model.md:

        {Indent(ExampleModel, 4)}

          cfweaver model.md -o scenarios.md -f md writes this table to scenarios.md:

        {Indent(ExampleTable, 4)}

        Full documentation: https://github.com/IanWold/CFWeaver/wiki
        """
        .ReplaceLineEndings();
        
    internal static string Version { get; } =
        (typeof(Tool).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.0").Split('+')[0];

    internal static string Name =>
        $"CFWeaver {Version}";

    static string Indent(string text, int spaces) =>
        string.Join(Environment.NewLine, text.ReplaceLineEndings("\n").Split('\n').Select(l => l.Length > 0 ? new string(' ', spaces) + l : l));
}
