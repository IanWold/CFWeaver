# Wiki Changes

Updates for the [CFWeaver wiki](https://github.com/IanWold/CFWeaver/wiki) covering CFWeaver 1.1.0. Each section below is the complete new content of one page, starting from the live wiki: open the page in the wiki editor, select all, and paste the block over it. Home is unchanged.

## What Changed

**CLI Reference**
- `OutputFormat`: every format includes operation descriptions; HTML and Markdown diagrams draw return values as end states; the HTML report needs an internet connection for its stylesheet and Mermaid.
- `--check`: "valid" is now "well-formed", with an example of the per-operation summary, a note that a well-formed model isn't necessarily accurate, and the `-f json` report with an example and a description of its diagnostics.
- New `--check -f json` example; errors and warnings go into the report rather than standard error in that mode.
- `--version`, and where the version is recorded in the outputs.

**Parser Errors**
- New errors `307` (variables written after a return or goto value instead of after `?`) and `308` (more than one condition, usually from `&` inside a condition).
- The warning is now numbered: `1001` No path reaches this step. The Warnings introduction says codes start at `1001` and mentions the `--check -f json` report.

**Control Flow State Model**
- Conditions: a result has at most one condition.
- Variables: names must be a single word (letters, digits, `_`, `-` or `.`), anything else after `?` is the condition, and variables don't need a condition. Notes that variables can't follow a return value directly.
- New Reserved Characters subsection.
- Comments is now Descriptions and Comments: text before an operation's first step is its description and is copied into the output.

## CLI Reference

````markdown
```
cfweaver [<InputFileName>]
    [-o|--output <OutputFileName>]
    [-f|--format <OutputFormat>]
    [--check]
```

### Arguments

`InputFileName`

_Required._ The relative or absolute path to the input file containing the control flow state diagram. This file must exist.

### Options

`OutputFileName`

_Required_ unless `--check` is given. The relative or absolute path to the output file, or `-` to write the output to standard output. If the directory or file doesn't exist, CFWeaver creates them.

`OutputFormat`

Optional. The format to output: `html`, `md` or `json`. If unspecified, defaults to `html`. Every format includes each operation's [description](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#descriptions-and-comments) and its scenarios; the HTML and Markdown formats also include a diagram of each operation, with its return values drawn as end states. The HTML report loads its stylesheet and the Mermaid library, which draws the diagrams, from public CDNs, so it needs an internet connection to display fully.

`--check`

Optional. Only checks that the model is well-formed, and writes no output even if an output path is given. If the model is well-formed, CFWeaver prints a summary with each operation's scenario count; otherwise it prints the errors. Warnings and exit codes are the same as for a normal run.

```
Warning 1001 (line 12 "* Notify: Sent = 204"): No path reaches this step.
Model is well-formed: 2 operations, 9 scenarios.
  Get an Item: 5 scenarios
  Delete an Item: 4 scenarios
```

A well-formed model isn't necessarily an accurate one: `--check` doesn't compare the model with the system it describes.

With `-f json`, `--check` prints a JSON report to standard output instead, including any errors and warnings, and writes nothing to standard error:

```json
{
  "generator": "CFWeaver 1.1.0",
  "wellFormed": true,
  "operations": [
    {
      "name": "Get an Item",
      "scenarios": 5
    },
    {
      "name": "Delete an Item",
      "scenarios": 4
    }
  ],
  "diagnostics": [
    {
      "severity": "warning",
      "code": 1001,
      "line": 12,
      "text": "* Notify: Sent = 204",
      "message": "No path reaches this step"
    }
  ]
}
```

Each diagnostic's `severity` is `error` or `warning`. `line` and `text` give the line of the model it refers to, and are `null` for errors about the whole run, such as a missing input file.

### Examples

```
cfweaver input.md -o C:/CFWeaverOutput/output.html

cfweaver C:/CFWeaver/myApi.md -o output.md -f md

cfweaver myApi.md -o - -f json

cfweaver myApi.md --check

cfweaver myApi.md --check -f json
```

### Output, Errors and Exit Codes

Progress messages are written to standard output. When the output is written to standard output with `-o -`, the progress messages are left out, so standard output contains only the result.

Errors and warnings are written to standard error, except with `--check -f json`, which includes them in its report. The model's errors and warnings are described in [Parser Errors](https://github.com/IanWold/CFWeaver/wiki/Parser-Errors); error `2` means the input file doesn't exist, and error `3` means no output path was given.

CFWeaver exits with code `0` when it writes the output, or when `--check` finds the model well-formed, even if there are warnings. It exits with code `1` when the input file doesn't exist, no output path was given, or the model has errors, and in that case it writes no output.

## Help and Version

```
cfweaver [-h|--help]

cfweaver --version
```

`--help`, or running `cfweaver` with no arguments, lists the arguments and options and then prints a short guide to the model syntax with a worked example.

`--version` prints the version of CFWeaver. The JSON output and the `--check -f json` report record it in their `generator` field, and the HTML report in a `generator` meta tag.
````

## Parser Errors

````markdown
## `1` Input contains no operation definitions

The input markdown file must contain one or more [operations](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#operations) defined as H1 headers:

```markdown
# Operation 1

Steps...
```

## `101` Operation contains no step definitions

The operation does not enumerate any [steps](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#steps) as a markdown list using asterisks (`*`).

```markdown
# Operation 1

* Step1: ...
```

## `102` Operation has no name

The operation must have a name.

Inavlid:

```markdown
#
```

Valid:

```markdown
# Operation
```

## `201` Step definitions must include a single `:` separating the step name from the step results

There must be a colon (`:`) separating the step name from its results.

Invalid:

```markdown
* Step Result
```

Valid:

```markdown
* Step: Result = 200
```

## `202` Step has no name

The step must have a name to the left of the colon:

Invalid:

```markdown
* : Result
```

Valid:

```markdown
* Step: Result = 200
```

## `203` Step names must not contain spaces

The step name cannot have spaces.

Invalid:

```markdown
* First Step: Result
```

Valid:

```markdown
* FirstStep: Result = 200
```

## `204` Step names must be unique within an operation

Each step in an operation must have a different name. The same step name can be used in different operations.

Invalid:

```markdown
* Validate: Valid | NotValid = 400
* Validate: Valid = 200 | NotValid = 422
```

Valid:

```markdown
* ValidateRequest: Valid | NotValid = 400
* ValidateItem: Valid = 200 | NotValid = 422
```

## `205` Step has no results

A step must have at least one [result](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#results), either after the colon or as a `-` list below the step.

Invalid:

```markdown
* Authenticate
* Validate: Valid = 200 | NotValid = 400
```

Valid:

```markdown
* Authenticate
    - Success
    - Failure = 403
* Validate: Valid = 200 | NotValid = 400
```

## `206` Step must come after an operation heading

Every step must be listed under an [operation](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#operations) header. To write a list before the first operation, use `-` instead of `*`.

Invalid:

```markdown
* Note: this file models the Items API

# Get Item

* Authenticate: Success = 200 | Failure = 403
```

Valid:

```markdown
- Note: this file models the Items API

# Get Item

* Authenticate: Success = 200 | Failure = 403
```

## `301` Result has no name

Results must have names.

Invalid:

```markdown
* Step: Success | | Failure
```

Valid:

```markdown
* Step: Success = 200 | NotFound = 404 | Failure = 500
```

## `302` No return or goto value given for result

If a result name is succeeded by an equal sign (`=`) it must be followed by a return value or a step name.

Invalid:

```markdown
* Step: Result =
```

Valid:

```markdown
* Step: Result = 200
```

## `303` Result must not contain variables with duplicate names

If a result defines variables, it can not define the same variable more than once.

Invalid:

```markdown
* Step: Result = 302 ? myVar = yes & myVar = no
```

Valid:

```markdown
* Step: Result = 302 ? myVar = yes & myOtherVar = no
```

## `304` Result goes to a step that does not exist

When a result [branches](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#branching) to another step, that step must be defined in the same operation.

Invalid:

```markdown
* GetFromDatabase: Found = UpdateOnDatabase | NotFound = 404
* UpdateInDatabase: Completed = 200 | Error = 500
```

Valid:

```markdown
* GetFromDatabase: Found = UpdateInDatabase | NotFound = 404
* UpdateInDatabase: Completed = 200 | Error = 500
```

## `305` Result must go to a later step

A result can only [branch](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#branching) forward, to a step defined below it. CFWeaver doesn't support loops, so a result can't go back to an earlier step or to its own step.

Invalid:

```markdown
* Authenticate: Success | Failure = 403
* Validate: Valid = 200 | Retry = Authenticate
```

Valid:

```markdown
* Authenticate: Success | Failure = 403
* Validate: Valid = 200 | NotValid = 400
```

## `306` Result on the final step must have a return value

A result without a return value continues to the next step, so the results on the last step of an operation have nowhere to go. Each of them must [return](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#returning) a value.

Invalid:

```markdown
* Authenticate: Success | Failure = 403
* Validate: Valid | NotValid = 400
```

Valid:

```markdown
* Authenticate: Success | Failure = 403
* Validate: Valid = 200 | NotValid = 400
```

## `307` Return or goto value must be a number or a single step name; variables go after `?`

After a result's `=` comes either a number to [return](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#returning) or the name of a step to [branch](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#branching) to, and nothing else. [Variables](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#variables) and conditions go after a `?`.

Invalid:

```markdown
* Step: Success = 200 & Flag=yes
```

Valid:

```markdown
* Step: Success = 200 ? Flag=yes
```

## `308` Result has more than one condition; `&` separates variables, so it can't appear in a condition

Everything after a result's `?` is split at each `&`. Each part written `name=value`, where the name is a single word, is a [variable](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#variables), and any other part is the result's condition. A result can have only one condition, so a condition can't contain `&`.

Invalid:

```markdown
* Step: Failure = 400 ? sort & order are both missing | Success = 200
```

Valid:

```markdown
* Step: Failure = 400 ? sort and order are both missing | Success = 200
```

# Warnings

Warnings point out likely mistakes in a model that is otherwise well-formed. Their codes start at `1001`. CFWeaver writes them to standard error, or into its report with `--check -f json`, still writes its output, and exits with code `0`.

## `1001` No path reaches this step

No path through the operation reaches the step, so it never appears in a scenario. This usually means the results before it all return or branch past it.

```markdown
* First: Done = 200 | Skip = Third
* Second: Done = 201
* Third: Done = 202
```

Here `First` either returns `200` or branches to `Third`, so `Second` is never reached.
````

## Control Flow State Model

````markdown
The Control Flow State Model (CFSM) is defined as a Markdown file with specific syntax constraints. In theory, this makes the CFSM itself easy to render to facilitate collaboration by various stakeholders who may or may not be interested in the output from CFWeaver.

## Operations

At the highest level, the CFSM models one or more _operations_, where an _operation_ is considered to be a discrete unit of test. For example, if we are using CFWeaver to model a REST API for integration tests, each CFWeaver _operation_ would most likely correspond 1-to-1 with an endpoint in the API. CFWeaver can product tests for any of the traditional levels of testing (i.e. unit, integration, e2e, and so on) but was developed with integration tests primarily in mind.

The Markdown CFSM must contain at least one _operation_. Each _operation_ is an H1 header in markdown, and every _step_ must come after an _operation_ header.

```markdown
# Get all Items

* Step...
* Step...
* Step...

# Update an Item by ID

* Step...
* Step...
* Step...
```

## Steps

Each _operation_ contains one or more _steps_ in the form of a list bulleted with asterisks (`*`). _Steps_ correlate to the specific decision points within the logic of the operation, where the code might take different paths or return based on the result of some action. This is the most crucial part of the CFSM since _steps_ define the control flow of the code.

In the REST API example, I might have _steps_ being authentication, validation, and data access, since each of these actions might yield different results that change the flow of the code. As a general rule, a _step_ will almost always be an action followed by a condition that acts based on the result of the action; an "authentication" _step_ will have the action of checking whether the request is authorized followed by the condition that the endpoint will return if it is not authorized.

Each _step_ follows the form `<name>:<results>`, with `<name>` following the same rules as identifiers in most languages: it should contain only alphanumeric characters and not spaces or other special characters. Each _step_ in an _operation_ must have a different name.

```markdown
# Update Item by ID

* Authenticate: Results...
* Validate: Results...
* UpdateInDatabase: Results...
```

## Results

Recall that a _step_ is an action that has multiple results plus some conditional routing to different logic in the code. The CFWeaver _result_ is how we model those results-plus-conditions for each step. A _step_ should have at least two _results_, but CFWeaver doesn't strictly require more than one _result_ per action.

In our code, any _result_ might lead to one of a few different routings through the control flow:

* The _result_ might indicate that the _operation_ cannot proceed, so it returns without further processing,
* The _result_ might be considered "normal" or "successful" and allows the control flow to proceed to the next _step_ in the sequence, or
* The _result_ might indicate that some processing is unnecessary and branches the flow to some _step_ other than the next one in the sequence.

We can model each of these three scenarios in the CFSM. _Results_ are listed after the step name (following a semicolon), delimited with pipes (`|`) between them. So, a list of results could be: `Success | NotFound | Failure`, each _result_ name following the typical identifier rules (like _step_ names).

### Returning

By default, CFWeaver assumes that a _result_ is "normal" and would lead the control flow to proceed to the next _step_ in sequence. If we want to return out of the _operation_ we will need to specify that after the _result_ name. We do that with an equals sign (`=`) after the _result_ name.

```markdown
# Update Item by ID

* Authenticate: Success | Failure = 403
* Validate: Valid | NotValid = 400
* UpdateInDatabase: Updated = 200 | NotFound = 404 | Error = 500
```

What is being modeled above can be expanded to:

* Authentication can either succeed and proceed to validation, or fail and return 403
* Validation can either be valid and proceed to updating in database, or be invalid and return 400
* Updating the item in the database will return 200 if it succeeds, but if the item isn't found we return 404. Any other errors return 500. Because a _result_ without a return value proceeds to the next _step_, every _result_ on the last _step_ of an _operation_ must return a value.

### Branching

Branching can be modeled by specifying a subsequent step in the place of a return value for a particular result. The _step_ must be defined later in the same _operation_; CFWeaver doesn't support loops, so a _result_ can't branch back to an earlier _step_.

```markdown
# Upsert Item

* GetFromDatabase: Found = UpdateOnDatabase | NotFound = InsertOnDatabase | Error = 500
* UpdateOnDatabase: Completed = 200 | NotCompleted = 400 | Error = 500
* InsertOnDatabase: Completed = 200 | Error = 500
```

If no path through the _operation_ reaches a _step_, for example because every _result_ before it returns or branches past it, CFWeaver warns about it, since that's usually a mistake in the model.

### Conditions

A result can optionally specify the conditions that cause it to happen. While conditions aren't strictly required, they are vital to the utility of CFWeaver. When computing the possible control flow paths, CFWeaver keeps track of the conditions associated with each path. These conditions ultimately determine the nature of a test scenario for each path; the system under test will need to be made to meet these conditions for the test.

Conditions can be written out as sentences and follow a question mark (`?`) at the end of the result definition. A result has at most one condition, and the conditions of the results along a path are chained together with "and" when the scenario table is generated.

```markdown
# Delete Item

* Authenticate: Success | Failure = 403 ? the client is not authenticated to delete
* Validate: Valid | NotValid = 400 ? the item id is less than 1
* DeleteFromDatabase: Deleted = 200 ? the item exists | NotFound = 200 ? the item does not exist | Error = 500 ? the database errors on DELETE
```

### Variables

Results can specify zero or more _variables_ to assign for the step, each of which gets its own column in the output table. Variables are defined in the same space as conditions, with a syntax that is similar to URL query strings.

After the `?`, a variable can be defined with `myVar = value`, where `myVar` is the name of the variable and `value` is its value. The name must be a single word made of letters, digits, `_`, `-` or `.`; anything after the `?` that isn't in this form is the result's condition. Multiple variables, and the condition, are separated with `&`. A result can set variables without having a condition, as `Success` does here:

```markdown
# Create Item

* Authenticate: Success = 201 ? DidError = no | Failure = 403 ? the client is not authenticated to create & DidError = yes
```

Notice in the above example the variable `DidError` comes _after_ the condition specification. This order is not required. Furthermore, here we can say that conditions are a special kind of variable which don't require a name to be specified. The above is equivalent to the following:

```markdown
# Create Item

* Authenticate: Success = 201 ? DidError = no | Failure = 403 ? Condition = the client is not authenticated to create & DidError = yes
```

```markdown
# Create Item

* Authenticate: Success = 201 ? DidError = no | Failure = 403 ? DidError = yes & the client is not authenticated to create
```

If more than one _result_ along a path assigns the same variable, the output table shows the value from the latest one. Conditions are the exception: as described above, they are chained together with "and".

Variables always go after the `?`. Writing them straight after a return value, as in `Success = 201 & DidError = no`, is an [error](https://github.com/IanWold/CFWeaver/wiki/Parser-Errors).

### Reserved Characters

`|` separates _results_, and `&` separates a _result_'s variables and condition, so neither can appear in a _result_ name, condition or variable value. A _result_ name also can't contain `=` or `?`, since the first of each marks where its return value and its condition and variables begin. After the `?`, both are ordinary text, so a condition such as `limit + offset = 10000` works as written. Any other punctuation, including commas, quotes and `<`, is fine: CFWeaver escapes it in the HTML and CSV output.

### Multi-Line Results

Writing results all out on a single line is a great help for simple steps, but can easily get long and difficult to parse if multiple results or variables are used. To make it easier to write and read, results can be specified on lines below the step definition:

```markdown
# Create Item

* Authenticate
    - Success ? DidError = no
    - Failure = 403 ? the client is not authenticated to create & DidError = yes
* Validate
    - Valid
    - NotValid = 400 ? the item id is less than 1
* GetFromDatabase
    - Found = UpdateOnDatabase
    - NotFound = InsertOnDatabase
    - Error = 500 ? DidError = yes
* UpdateOnDatabase
    - Completed = 200
    - Error = 500 ? DidError = yes
* InsertOnDatabase
    - Completed = 200
    - Error = 500 ? DidError = yes
```

In this example, specifying the results using `-` as the list bullet is equivalent to enumerating the results with `|` on the same line as the step definition.

## Descriptions and Comments

CFWeaver's parser is incredibly unsophisticated, however this is to our advantage. Text between an _operation_'s header and its first _step_, including `-` lists, is that _operation_'s description, and text before the first _operation_ describes the whole file. CFWeaver copies descriptions into the HTML, Markdown and JSON output, which makes them a good place for source references, assumptions and preconditions. Use `-` rather than `*` for lists in a description, since `*` starts a _step_.

After an _operation_'s first _step_, `-` lines are read as the preceding _step_'s [multi-line results](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#multi-line-results), and any other text is ignored, so comments there don't appear in the output.

```markdown
This file contains the control flow models for the Items operations.

# Get Item

This endpoint is located at `/items/{itemId}`.

- `itemId`: int > 0

* Authenticate: Success | Failure = 403
* Validate: Valid | NotValid = 400
* GetFromDatabase: Found = 200 | NotFound = 404 | Error = 500
```

Here, the first line describes the whole file, and the endpoint's location and the `itemId` list describe the Get Item operation.
````
