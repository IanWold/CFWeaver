using StateFusion;

var content = File.ReadAllText(args.FirstOrDefault() ?? "Example.md");
await Parser.Parse(content).MapAsync(
    success: async document =>
    {
        if (!Directory.Exists("output"))
        {
            Directory.CreateDirectory("output");
        }

        await File.WriteAllTextAsync($"output/index.html", document.Html());
    },
    failure: errors =>
    {
        Console.WriteLine(errors.PrettyPrint());
        return Task.CompletedTask;
    }
);
