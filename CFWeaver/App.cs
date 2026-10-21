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
        /// <param name="check">Only check that the model is well-formed; writes no output. With -f json, reports as JSON.</param>
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
                terminal.Error.WriteLine(Diagnostics.Errors.OutputNotSpecified.Result().Errors.PrettyPrint());
                return 1;
            }

            if (!fileSystem.TryReadAllText(input, out var content))
            {
                if (check)
                {
                    return Report(new(Diagnostics.Errors.FileDoesNotExist.Result().Errors, [], []));
                }

                terminal.Error.WriteLine(Diagnostics.Errors.FileDoesNotExist.Result().Errors.PrettyPrint());
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

                    if (check)
                    {
                        return Report(new([], document.Warnings, document.Operations));
                    }

                    foreach (var warning in document.Warnings)
                    {
                        terminal.Error.WriteLine(warning);
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
                    if (check)
                    {
                        return Task.FromResult(Report(new(errors, [], [])));
                    }

                    terminal.Error.WriteLine(errors.PrettyPrint());
                    return Task.FromResult(1);
                }
            );

            int Report(CheckReport report)
            {
                if (format == Format.Json)
                {
                    terminal.Out.WriteLine(report.Json());
                }
                else
                {
                    foreach (var diagnostic in report.Errors.Select(e => e.ToString()).Concat(report.Warnings.Select(w => w.ToString())))
                    {
                        terminal.Error.WriteLine(diagnostic);
                    }

                    if (report.IsWellFormed)
                    {
                        terminal.Out.WriteLine(report.Summary());
                    }
                }

                return report.IsWellFormed ? 0 : 1;
            }
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
            textWriter.WriteLine(Tool.Guide);
        }
    }
}
