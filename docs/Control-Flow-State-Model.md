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

We can model each of these three scenarios in the CFSM. _Results_ are listed after the step name (following a colon), delimited with pipes (`|`) between them. So, a list of results could be: `Success | NotFound | Failure`, each _result_ name following the typical identifier rules (like _step_ names).

### Returning

By default, CFWeaver assumes that a _result_ is "normal" and would lead the control flow to proceed to the next _step_ in sequence. If we want to return out of the _operation_ we will need to specify that after the _result_ name. We do that with `=>` after the _result_ name, followed by the value to return.

```markdown
# Update Item by ID

* Authenticate: Success | Failure => 403
* Validate: Valid | NotValid => 400
* UpdateInDatabase: Updated => 200 | NotFound => 404 | Error => 500
```

What is being modeled above can be expanded to:

* Authentication can either succeed and proceed to validation, or fail and return 403
* Validation can either be valid and proceed to updating in database, or be invalid and return 400
* Updating the item in the database will return 200 if it succeeds, but if the item isn't found we return 404. Any other errors return 500. Because a _result_ without a return value proceeds to the next _step_, every _result_ on the last _step_ of an _operation_ must return a value.

A return value doesn't have to be a number. It can be any text, such as an exception that's thrown or a value a method returns, so CFWeaver can model code other than HTTP endpoints:

```markdown
# Save Item

* Validate: Valid | Invalid => throws ArgumentException
* SaveToDatabase: Saved => Result.Ok(item) | Conflict => Result.Conflict
```

A return value can contain spaces and most punctuation, but not the [reserved characters](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#reserved-characters) `|`, `&`, `=` and `?`.

### Branching

Branching can be modeled with an equals sign (`=`) after the _result_ name, followed by the name of the _step_ to go to. The _step_ must be defined later in the same _operation_; CFWeaver doesn't support loops, so a _result_ can't branch back to an earlier _step_.

```markdown
# Upsert Item

* GetFromDatabase: Found = UpdateOnDatabase | NotFound = InsertOnDatabase | Error => 500
* UpdateOnDatabase: Completed => 200 | NotCompleted => 400 | Error => 500
* InsertOnDatabase: Completed => 200 | Error => 500
```

`=` always names a _step_, and `=>` always returns a value, so `NotFound = 404` is an error, because there's no _step_ named `404`. In the other direction, if a _result_ returns the name of a _step_, as in `Found => UpdateOnDatabase`, CFWeaver warns about it, since that's usually a branch written with the wrong token.

If no path through the _operation_ reaches a _step_, for example because every _result_ before it returns or branches past it, CFWeaver warns about it, since that's usually a mistake in the model.

### Conditions

A result can optionally specify the conditions that cause it to happen. While conditions aren't strictly required, they are vital to the utility of CFWeaver. When computing the possible control flow paths, CFWeaver keeps track of the conditions associated with each path. These conditions ultimately determine the nature of a test scenario for each path; the system under test will need to be made to meet these conditions for the test.

Conditions can be written out as sentences and follow a question mark (`?`) at the end of the result definition. A result has at most one condition, and the conditions of the results along a path are chained together with "and" when the scenario table is generated.

```markdown
# Delete Item

* Authenticate: Success | Failure => 403 ? the client is not authenticated to delete
* Validate: Valid | NotValid => 400 ? the item id is less than 1
* DeleteFromDatabase: Deleted => 200 ? the item exists | NotFound => 200 ? the item does not exist | Error => 500 ? the database errors on DELETE
```

Each scenario's conditions describe what its test has to set up, so a well-written model gives every scenario different conditions, and the Condition column reads as a test plan. The simplest way to get there is to give every _result_ a condition except the one its _step_ normally takes, like `Success` and `Valid` above, and never to give two _results_ on the same _step_ the same condition. CFWeaver [warns](https://github.com/IanWold/CFWeaver/wiki/Parser-Errors#warnings) when two scenarios in an _operation_ end up with the same conditions. The examples before this section leave conditions out to keep them short.

### Variables

Results can specify zero or more _variables_ to assign for the step, each of which gets its own column in the output table. Variables are defined in the same space as conditions, with a syntax that is similar to URL query strings.

After the `?`, a variable can be defined with `myVar = value`, where `myVar` is the name of the variable and `value` is its value. The name must be a single word made of letters, digits, `_`, `-` or `.`; anything after the `?` that isn't in this form is the result's condition. Multiple variables, and the condition, are separated with `&`. A result can set variables without having a condition, as `Success` does here:

```markdown
# Create Item

* Authenticate: Success => 201 ? DidError = no | Failure => 403 ? the client is not authenticated to create & DidError = yes
```

Notice in the above example the variable `DidError` comes _after_ the condition specification. This order is not required. Furthermore, here we can say that conditions are a special kind of variable which don't require a name to be specified. The above is equivalent to the following:

```markdown
# Create Item

* Authenticate: Success => 201 ? DidError = no | Failure => 403 ? Condition = the client is not authenticated to create & DidError = yes
```

```markdown
# Create Item

* Authenticate: Success => 201 ? DidError = no | Failure => 403 ? DidError = yes & the client is not authenticated to create
```

If more than one _result_ along a path assigns the same variable, the output table shows the value from the latest one. Conditions are the exception: as described above, they are chained together with "and".

Variables always go after the `?`. Writing them straight after a return value, as in `Success => 201 & DidError = no`, is an [error](https://github.com/IanWold/CFWeaver/wiki/Parser-Errors).

### Reserved Characters

`|` separates _results_, and `&` separates a _result_'s variables and condition, so neither can appear in a _result_ name, return value, condition or variable value. A _result_ name or return value also can't contain `=` or `?`, since the first `=` marks where its return value (`=>`) or branch (`=`) begins, and the first `?` where its condition and variables begin. After the `?`, both are ordinary text, so a condition such as `limit + offset = 10000` works as written. Any other punctuation, including commas, quotes and `<`, is fine: CFWeaver escapes it in the HTML and CSV output.

Text between backticks is literal: none of these characters is reserved inside it, which makes it easy to copy code into a model.

```markdown
# Search Items

* Validate: Valid | Invalid => 400 ? `limit < 1 || limit > 100` & Query = `limit=0&offset=5`
* Search: Found => `Results.Ok(items)` | Failed => 500 ? the search index errors
```

The backticks stay in the output, so the Markdown and HTML reports show the text as code. As in Markdown, text between two backticks can contain a single one: ``` ``it's a ` mark`` ```. A backtick that's never closed is an [error](https://github.com/IanWold/CFWeaver/wiki/Parser-Errors).

### Multi-Line Results

Writing results all out on a single line is a great help for simple steps, but can easily get long and difficult to parse if multiple results or variables are used. To make it easier to write and read, results can be specified on lines below the step definition:

```markdown
# Create Item

* Authenticate
    - Success ? DidError = no
    - Failure => 403 ? the client is not authenticated to create & DidError = yes
* Validate
    - Valid
    - NotValid => 400 ? the item id is less than 1
* GetFromDatabase
    - Found = UpdateOnDatabase ? an item with this id exists
    - NotFound = InsertOnDatabase ? no item has this id
    - Error => 500 ? the database errors on select & DidError = yes
* UpdateOnDatabase
    - Completed => 200
    - Error => 500 ? the database errors on update & DidError = yes
* InsertOnDatabase
    - Completed => 200
    - Error => 500 ? the database errors on insert & DidError = yes
```

In this example, specifying the results using `-` as the list bullet is equivalent to enumerating the results with `|` on the same line as the step definition.

## Sub-operations

Some logic is shared by many _operations_, like authentication, or is involved enough to deserve its own model, like loading an item from a cache or a database. You can model it as an _operation_ of its own and call it from a _step_: a _step_ with the same name as another _operation_ is read as calling it, and its _results_ should be that _operation_'s return values.

```markdown
# Get Item

* Authenticate: Valid | Invalid => 401 ? the request has no valid token
* Lookup: Found => 200 | NotFound => 404 ? no item has this id

# Authenticate

* CheckToken: Valid | Missing => Invalid ? no token is sent | Expired => Invalid ? the token has expired
* LoadUser: Found => Valid | NotFound => Invalid ? the token's user was deleted
```

Here the `Authenticate` _step_ in Get Item calls the Authenticate _operation_, which returns `Valid` or `Invalid`. Get Item handles both: `Valid` continues to `Lookup`, and `Invalid` returns 401. Because _step_ names can't contain spaces, only an _operation_ whose name is a single word can be called this way, and the names must match exactly.

CFWeaver doesn't combine the _operations_' scenarios. Each keeps its own table, so Get Item has one scenario for `Invalid`, and Authenticate's table shows the three ways to cause it. Get Item's table shows only the conditions written on its own _results_, which is why `Invalid` has a condition that sums up what causes it. CFWeaver does check that the two agree, and [warns](https://github.com/IanWold/CFWeaver/wiki/Parser-Errors#warnings) if the _step_ leaves out one of the _operation_'s return values, or has a _result_ the _operation_ never returns.

In the output, each _operation_ links to the _sub-operations_ it calls and to the _operations_ that call it, and notes that a caller's table shows only the value each call returns. [Modeling Recipes](https://github.com/IanWold/CFWeaver/wiki/Modeling-Recipes) shows how _sub-operations_ can model loops, and when a _sub-operation_ needs more than one return value.

## Descriptions and Comments

CFWeaver's parser is incredibly unsophisticated, however this is to our advantage. Text between an _operation_'s header and its first _step_, including `-` lists, is that _operation_'s description, and text before the first _operation_ describes the whole file. CFWeaver copies descriptions into the HTML, Markdown and JSON output, which makes them a good place for source references, assumptions and preconditions. Use `-` rather than `*` for lists in a description, since `*` starts a _step_.

After an _operation_'s first _step_, `-` lines are read as the preceding _step_'s [multi-line results](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#multi-line-results), and any other text is ignored, so comments there don't appear in the output.

```markdown
This file contains the control flow models for the Items operations.

# Get Item

This endpoint is located at `/items/{itemId}`.

- `itemId`: int > 0

* Authenticate: Success | Failure => 403 ? the client is not authenticated
* Validate: Valid | NotValid => 400 ? the item id is less than 1
* GetFromDatabase: Found => 200 | NotFound => 404 ? no item has this id | Error => 500 ? the database errors
```

Here, the first line describes the whole file, and the endpoint's location and the `itemId` list describe the Get Item operation.