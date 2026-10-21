```
cfweaver [<InputFileName>]
    [-o|--output <OutputFileName>]
    [-f|--format <OutputFormat>]
    [--check]
    [--self-contained]
```

### Arguments

`InputFileName`

_Required._ The relative or absolute path to the input file containing the control flow state diagram. This file must exist.

### Options

`OutputFileName`

_Required_ unless `--check` is given. The relative or absolute path to the output file, or `-` to write the output to standard output. If the directory or file doesn't exist, CFWeaver creates them.

`OutputFormat`

Optional. The format to output: `html`, `md` or `json`. If unspecified, defaults to `html`. Every format includes each operation's [description](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#descriptions-and-comments) and its scenarios; the HTML and Markdown formats also include a diagram of each operation, with its return values drawn as end states. In the JSON output, each scenario's `response` is a number if the returned value is an integer, such as `404`, and a string otherwise. Each operation also links to the [sub-operations](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#sub-operations) it calls and to the operations that call it; in the JSON output, these are its `calls` and `calledBy` lists. The HTML report includes its stylesheet, but loads the Mermaid library, which draws the diagrams, from a public CDN, so its diagrams need an internet connection unless `--self-contained` is given.

`--check`

Optional. Only checks that the model is well-formed, and writes no output even if an output path is given. If the model is well-formed, CFWeaver prints a summary with each operation's scenario count, and for a sub-operation, the operations that call it; otherwise it prints the errors. Warnings and exit codes are the same as for a normal run.

```
Warning 1001 (line 12 "* Notify: Sent => 204"): No path reaches this step.
Model is well-formed: 2 operations, 9 scenarios.
  Get an Item: 5 scenarios
  Delete an Item: 4 scenarios
```

A well-formed model isn't necessarily an accurate one: `--check` doesn't compare the model with the system it describes.

With `-f json`, `--check` prints a JSON report to standard output instead, including any errors and warnings and each operation's `calledBy` list, and writes nothing to standard error.

Each diagnostic's `severity` is `error` or `warning`. `line` and `text` give the line of the model it refers to, and are `null` for errors about the whole run, such as a missing input file.

`--self-contained`

Optional, and only for HTML output. Embeds the Mermaid library in the HTML report, so its diagrams display without an internet connection. This makes the report about 5.5 MB larger.

### Examples

```
cfweaver input.md -o C:/CFWeaverOutput/output.html

cfweaver C:/CFWeaver/myApi.md -o output.md -f md

cfweaver myApi.md -o - -f json

cfweaver myApi.md --check

cfweaver myApi.md --check -f json

cfweaver myApi.md -o report.html --self-contained
```

### Output, Errors and Exit Codes

Progress messages are written to standard output. When the output is written to standard output with `-o -`, the progress messages are left out, so standard output contains only the result.

Errors and warnings are written to standard error, except with `--check -f json`, which includes them in its report. The model's errors and warnings are described in [Parser Errors](https://github.com/IanWold/CFWeaver/wiki/Parser-Errors); error `2` means the input file doesn't exist, error `3` means no output path was given, error `4` means `--self-contained` was given with a format other than HTML, and error `5` means `cfweaver help` was given a topic it doesn't have.

CFWeaver exits with code `0` when it writes the output, or when `--check` finds the model well-formed, even if there are warnings. It exits with code `1` when the input file doesn't exist, no output path was given, `--self-contained` was given without HTML output, or the model has errors, and in that case it writes no output.

## Help and Version

```
cfweaver [-h|--help]

cfweaver help [<topic>]

cfweaver --version
```

`--help`, or running `cfweaver` with no arguments, lists the arguments and options and then prints a short guide to the model syntax with a worked example.

`cfweaver help` prints longer guides that are built into CFWeaver, so they're available without internet access. With no topic, it lists the topics:

* `recipes` prints [Modeling Recipes](https://github.com/IanWold/CFWeaver/wiki/Modeling-Recipes).
* `errors` prints [Parser Errors](https://github.com/IanWold/CFWeaver/wiki/Parser-Errors), which describes every error and warning.
* An error or warning code, such as `309`, prints just that entry from Parser Errors.

An unknown topic is error `5`.

`--version` prints the version of CFWeaver. The JSON output and the `--check -f json` report record it in their `generator` field, and the HTML report in a `generator` meta tag.