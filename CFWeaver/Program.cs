using CFWeaver;
using ConsoleAppFramework;

var app = ConsoleApp.Create();
app.Add<Commands>();
await app.RunAsync(args);

public class Commands
{
    public enum Format
    {
        Html,
        Md
    }

    /// <summary>
    /// Generates test scenarios from a control flow state diagram.
    /// </summary>
    /// <param name="input">The input control flow state diagram.</param>
    /// <param name="output">-o, The path to the output file.</param>
    /// <param name="format">-f, The format to output. [html|md]</param>
    [Command("")]
    public async Task Compile(
        [Argument] string input,
        string output,
        Format format = Format.Html
    )
    {
        var content = File.ReadAllText(input);
        await Parser.Parse(content).MapAsync(
            success: async document =>
            {
                if (Path.GetDirectoryName(output) is string outputDirectory
                    && !string.IsNullOrWhiteSpace(outputDirectory)
                    && !Directory.Exists(outputDirectory)
                )
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                await File.WriteAllTextAsync(output, format switch {
                    Format.Md => document.Markdown(),
                    Format.Html or _ => document.Html()
                });
            },
            failure: errors =>
            {
                Console.WriteLine(errors.PrettyPrint());
                return Task.CompletedTask;
            }
        );
    }
}
