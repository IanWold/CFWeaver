using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;

namespace CFWeaver;

public class App(string[] args, TextWriter textWriter, FileSystem fileSystem)
{
    public class Commands(TextWriter textWriter, FileSystem fileSystem)
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
            if (!fileSystem.TryReadAllText(input, out var content))
            {
                textWriter.WriteLine(Errors.FileDoesNotExist);
                return;
            }

            textWriter.WriteLine($"Successfully read {input}.");
            textWriter.WriteLine($"Beginning parse...");

            await Parser.Parse(content).MapAsync(
                success: async document =>
                {
                    textWriter.WriteLine($"Successfully parsed.");
                    textWriter.WriteLine($"Writing to {output}...");
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

                    textWriter.WriteLine($"Finished.");
                },
                failure: errors =>
                {
                    textWriter.WriteLine(errors.PrettyPrint());
                    return Task.CompletedTask;
                }
            );
        }
    }
    
    public async Task RunAsync()
    {
        var app = ConsoleApp.Create()
            .ConfigureServices(services =>
            {
                services.AddSingleton(textWriter);
                services.AddSingleton(fileSystem);
            });

        app.Add<Commands>();
        
        await app.RunAsync(args);
    }
}
