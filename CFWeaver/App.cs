using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;

namespace CFWeaver;

public class App(string[] args, TextWriter textWriter, TextWriter errorWriter, FileSystem fileSystem)
{
    public record Terminal(TextWriter Out, TextWriter Error);

    public class Commands(Terminal terminal, FileSystem fileSystem)
    {
        public enum Format
        {
            Html,
            Md,
            Json
        }

        /// <summary>
        /// Generates test scenarios from a control flow state diagram.
        /// </summary>
        /// <param name="input">The path to the Markdown model file (see Model syntax below).</param>
        /// <param name="output">-o, The path to the output file, or - to write to stdout. Required unless --check is given.</param>
        /// <param name="format">-f, The format to output. [html|md|json]</param>
        /// <param name="check">Only validate the model: report errors and warnings without writing any output.</param>
        [Command("")]
        public async Task<int> Compile(
            [Argument] string input,
            string? output = null,
            Format format = Format.Html,
            bool check = false
        )
        {
            if (!check && output is null)
            {
                terminal.Error.WriteLine(Errors.OutputNotSpecified.Result().Errors.PrettyPrint());
                return 1;
            }

            if (!fileSystem.TryReadAllText(input, out var content))
            {
                terminal.Error.WriteLine(Errors.FileDoesNotExist.Result().Errors.PrettyPrint());
                return 1;
            }

            var toStandardOut = output == "-";
            var log = check || toStandardOut ? TextWriter.Null : terminal.Out;

            log.WriteLine($"Successfully read {input}.");
            log.WriteLine($"Beginning parse...");

            return await Parser.Parse(content).Map(
                success: async document =>
                {
                    log.WriteLine($"Successfully parsed.");

                    foreach (var warning in document.Warnings)
                    {
                        terminal.Error.WriteLine(warning);
                    }

                    if (check)
                    {
                        var operations = document.Operations.Count();
                        var scenarios = document.Operations.Sum(o => o.Scenarios.Count);
                        terminal.Out.WriteLine($"Model is valid: {operations} operation{(operations == 1 ? "" : "s")}, {scenarios} scenario{(scenarios == 1 ? "" : "s")}.");
                        return 0;
                    }

                    var rendered = format switch {
                        Format.Md => document.Markdown(),
                        Format.Json => document.Json(),
                        Format.Html or _ => document.Html()
                    };

                    if (toStandardOut)
                    {
                        terminal.Out.WriteLine(rendered);
                        return 0;
                    }

                    log.WriteLine($"Writing to {output}...");
                    if (fileSystem.GetDirectoryName(output!) is string outputDirectory
                        && !string.IsNullOrWhiteSpace(outputDirectory)
                        && !fileSystem.DirectoryExists(outputDirectory)
                    )
                    {
                        fileSystem.CreateDirectory(outputDirectory);
                    }

                    await fileSystem.WriteAllTextAsync(output!, rendered);

                    log.WriteLine($"Finished.");
                    return 0;
                },
                failure: errors =>
                {
                    terminal.Error.WriteLine(errors.PrettyPrint());
                    return Task.FromResult(1);
                }
            );
        }
    }

    public async Task RunAsync()
    {
        var app = ConsoleApp.Create()
            .ConfigureServices(services =>
            {
                services.AddSingleton(new Terminal(textWriter, errorWriter));
                services.AddSingleton(fileSystem);
            });

        app.Add<Commands>();

        await app.RunAsync(args);

        if (args is [] || args.Any(a => a is "-h" or "--help"))
        {
            textWriter.WriteLine();
            textWriter.WriteLine(Help.Guide);
        }
    }
}
