namespace CFWeaver;

public static class Help
{
    public const string ExampleModel =
        """
        # Get an Item

        * Authenticate: Success | Failure = 403 ? the caller is not authenticated
        * Validate: Success | Failure = 400 ? the request is invalid
        * GetFromDatabase: Found ? the item exists | Not found = 404 ? the item does not exist
        * Authorize: Success = 200 | Failure = 401 ? the caller can't access the item & Audited=yes
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
          The input is a Markdown file. Lines that don't start with #, * or - are ignored.

          # Operation name        Each # heading starts an operation, which gets its own scenario table.
          * Step: A | B           Each * line under it is a step, a decision point, in the order the steps
                                  run. Step names can't contain spaces and must be unique in the operation.
          * Step                  Results can also be listed below a step, one per - line, instead of
              - Result            after a colon.
              - Result = 404

        Results:
          What a result does next depends on what follows its name:
            Success               No "=": continue to the next step.
            Failure = 404         "= <integer>": the operation returns this value.
            Found = UpdateItem    "= <step name>": go to that step. It must come later in the operation;
                                  loops aren't supported.
          Every result on the last step must return a value.

        Conditions and variables:
          After a result, "?" describes the condition that causes it, and "&" adds variables:
            Failure = 403 ? the caller is not authenticated & Audited=yes
          A result has at most one condition. Conditions along a path are joined with "and"; a variable
          shows the last value assigned along the path.

        Comments:
          A - list before an operation's first step is a comment. After a step, - lines are its results.

        Output:
          One row per path: the result taken at each step (blank where the path skipped the step), the
          returned value (Respond), the joined conditions, and one column per variable. Use -f md for a
          Markdown table, or -f json for each scenario's path, response, conditions and variables.
          Use -o - to write to stdout instead of a file.

          If the model is invalid, errors are written to stderr with a code and line number, no output
          is written, and cfweaver exits with code 1. Warnings, such as a step that no path reaches,
          are also written to stderr but don't stop the output.

        Example:
          model.md:

        {Indent(ExampleModel, 4)}

          cfweaver model.md -o scenarios.md -f md writes this table to scenarios.md:

        {Indent(ExampleTable, 4)}

        Full documentation: https://github.com/IanWold/CFWeaver/wiki
        """.ReplaceLineEndings();

    static string Indent(string text, int spaces) =>
        string.Join(Environment.NewLine, text.ReplaceLineEndings("\n").Split('\n').Select(l => l.Length > 0 ? new string(' ', spaces) + l : l));
}
