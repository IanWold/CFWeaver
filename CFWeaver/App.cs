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
            Md
        }

        /// <summary>
        /// Generates test scenarios from a control flow state diagram.
        /// </summary>
        /// <param name="input">The path to the Markdown model file (see Model syntax below).</param>
        /// <param name="output">-o, The path to the output file.</param>
        /// <param name="format">-f, The format to output. [html|md]</param>
        [Command("")]
        public async Task<int> Compile(
            [Argument] string input,
            string output,
            Format format = Format.Html
        )
        {
            if (!fileSystem.TryReadAllText(input, out var content))
            {
                terminal.Error.WriteLine(Errors.FileDoesNotExist.Result().Errors.PrettyPrint());
                return 1;
            }

            terminal.Out.WriteLine($"Successfully read {input}.");
            terminal.Out.WriteLine($"Beginning parse...");

            return await Parser.Parse(content).Map(
                success: async document =>
                {
                    terminal.Out.WriteLine($"Successfully parsed.");
                    terminal.Out.WriteLine($"Writing to {output}...");
                    if (fileSystem.GetDirectoryName(output) is string outputDirectory
                        && !string.IsNullOrWhiteSpace(outputDirectory)
                        && !fileSystem.DirectoryExists(outputDirectory)
                    )
                    {
                        fileSystem.CreateDirectory(outputDirectory);
                    }

                    await fileSystem.WriteAllTextAsync(output, format switch {
                        Format.Md => document.Markdown(),
                        Format.Html or _ => document.Html()
                    });

                    terminal.Out.WriteLine($"Finished.");
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
