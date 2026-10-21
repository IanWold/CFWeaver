Every error and warning CFWeaver reports, with examples. `cfweaver help <code>` prints a single entry, such as `cfweaver help 309`, and `cfweaver help errors` prints this whole page.

## `1` Input contains no operation definitions

The input markdown file must contain one or more [operations](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#operations) defined as H1 headers:

```markdown
# Operation 1

Steps...
```

## `2` Specified input file does not exist

The input file must exist. A relative path is resolved from the current directory.

## `3` No output path given; use `-o <path>`, `-o -` for stdout, or `--check` to only validate the model

CFWeaver needs somewhere to write its output. Give a file with `-o`, use `-o -` to write to standard output, or use `--check` to only check that the model is well-formed.

## `4` --self-contained applies only to HTML output

`--self-contained` embeds the diagram library in the HTML report, so it can't be used with `-f md` or `-f json`.

## `5` No help topic with that name; run `cfweaver help` to list the topics

`cfweaver help` takes `agents`, `skill`, `recipes`, `errors`, or an error or warning code such as `309`. Run `cfweaver help` on its own to list the topics.

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
* Step: Result => 200
```

## `202` Step has no name

The step must have a name to the left of the colon:

Invalid:

```markdown
* : Result
```

Valid:

```markdown
* Step: Result => 200
```

## `203` Step names must not contain spaces

The step name cannot have spaces.

Invalid:

```markdown
* First Step: Result
```

Valid:

```markdown
* FirstStep: Result => 200
```

## `204` Step names must be unique within an operation

Each step in an operation must have a different name. The same step name can be used in different operations.

Invalid:

```markdown
* Validate: Valid | NotValid => 400 ? the request is invalid
* Validate: Valid => 200 | NotValid => 422 ? the item is invalid
```

Valid:

```markdown
* ValidateRequest: Valid | NotValid => 400 ? the request is invalid
* ValidateItem: Valid => 200 | NotValid => 422 ? the item is invalid
```

## `205` Step has no results

A step must have at least one [result](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#results), either after the colon or as a `-` list below the step.

Invalid:

```markdown
* Authenticate
* Validate: Valid => 200 | NotValid => 400 ? the request is invalid
```

Valid:

```markdown
* Authenticate
    - Success
    - Failure => 403 ? the caller isn't authenticated
* Validate: Valid => 200 | NotValid => 400 ? the request is invalid
```

## `206` Step must come after an operation heading

Every step must be listed under an [operation](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#operations) header. To write a list before the first operation, use `-` instead of `*`.

Invalid:

```markdown
* Note: this file models the Items API

# Get Item

* Authenticate: Success => 200 | Failure => 403 ? the caller isn't authenticated
```

Valid:

```markdown
- Note: this file models the Items API

# Get Item

* Authenticate: Success => 200 | Failure => 403 ? the caller isn't authenticated
```

## `207` Step calls an operation that does not exist

A step can name the operation it calls in brackets after its own name, to call it as a [sub-operation](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#sub-operations). That operation must be defined in the same file, with exactly that name.

Invalid:

```markdown
# Bind Query

* BindFilter [ParseFilter]: Parsed => 200 | Invalid => 400 ? a filter token is malformed

# Parse Filter

* Split: Parsed => Parsed | Malformed => Invalid ? the token has no colon
```

Valid:

```markdown
# Bind Query

* BindFilter [Parse Filter]: Parsed => 200 | Invalid => 400 ? a filter token is malformed

# Parse Filter

* Split: Parsed => Parsed | Malformed => Invalid ? the token has no colon
```

## `208` Step calls the operation it belongs to

A step can't name its own operation in brackets, since an operation can't call itself.

Invalid:

```markdown
# Parse Filter

* Split [Parse Filter]: Parsed => Parsed | Malformed => Invalid ? the token has no colon
```

Valid:

```markdown
# Parse Filter

* Split: Parsed => Parsed | Malformed => Invalid ? the token has no colon
```

## `301` Result has no name

Results must have names.

Invalid:

```markdown
* Step: Success | | Failure
```

Valid:

```markdown
* Step: Success => 200 | NotFound => 404 ? no item has this id | Failure => 500 ? the database errors
```

## `302` No return or goto value given for result

A result's `=>` must be followed by a value to [return](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#returning), and its `=` by the name of a step to [branch](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#branching) to.

Invalid:

```markdown
* Step: Result =
```

Valid:

```markdown
* Step: Result => 200
```

## `303` Result must not contain variables with duplicate names

If a result defines variables, it can not define the same variable more than once.

Invalid:

```markdown
* Step: Result => 302 ? myVar = yes & myVar = no
```

Valid:

```markdown
* Step: Result => 302 ? myVar = yes & myOtherVar = no
```

## `304` Result goes to a step that does not exist; use `=>` to return a value

When a result [branches](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#branching) to another step with `=`, that step must be defined in the same operation. Because `=` always names a step, a value written after it, such as `NotFound = 404`, also gives this error; use `=>` to [return](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#returning) a value. Models written for earlier versions of CFWeaver, which returned numbers with `=`, give this error for each of their return values.

Invalid:

```markdown
* GetFromDatabase: Found = UpdateOnDatabase | NotFound = 404 ? no item has this id
* UpdateInDatabase: Completed => 200 | Error => 500 ? the update fails
```

Valid:

```markdown
* GetFromDatabase: Found = UpdateInDatabase | NotFound => 404 ? no item has this id
* UpdateInDatabase: Completed => 200 | Error => 500 ? the update fails
```

## `305` Result must go to a later step

A result can only [branch](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#branching) forward, to a step defined below it. CFWeaver doesn't support loops, so a result can't go back to an earlier step or to its own step.

Invalid:

```markdown
* Authenticate: Success | Failure => 403 ? the caller isn't authenticated
* Validate: Valid => 200 | Retry = Authenticate
```

Valid:

```markdown
* Authenticate: Success | Failure => 403 ? the caller isn't authenticated
* Validate: Valid => 200 | NotValid => 400 ? the request is invalid
```

## `306` Result on the final step must return a value with `=>`

A result without a return value continues to the next step, so the results on the last step of an operation have nowhere to go. Each of them must [return](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#returning) a value with `=>`.

Invalid:

```markdown
* Authenticate: Success | Failure => 403 ? the caller isn't authenticated
* Validate: Valid | NotValid => 400 ? the request is invalid
```

Valid:

```markdown
* Authenticate: Success | Failure => 403 ? the caller isn't authenticated
* Validate: Valid => 200 | NotValid => 400 ? the request is invalid
```

## `307` A goto must be a single step name, and a return value can't contain `&` or `=`; variables go after `?`

After a result's `=` comes the name of a single step to [branch](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#branching) to, and after its `=>` a value to [return](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#returning), which can't contain `&` or `=`. [Variables](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#variables) and conditions go after a `?`.

Invalid:

```markdown
* Step: Success => 200 & Flag=yes
```

Valid:

```markdown
* Step: Success => 200 ? Flag=yes
```

## `308` Result has more than one condition; `&` separates variables, so it can't appear in a condition

Everything after a result's `?` is split at each `&`. Each part written `name=value`, where the name is a single word, is a [variable](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#variables), and any other part is the result's condition. A result can have only one condition, so a condition can't contain `&`.

Invalid:

```markdown
* Step: Failure => 400 ? sort & order are both missing | Success => 200
```

Valid:

```markdown
* Step: Failure => 400 ? sort and order are both missing | Success => 200
```

## `309` Result has a backtick that is never closed; literal text goes between a pair of backticks

Text between backticks is [literal](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#reserved-characters), so reserved characters such as `|` and `&` inside it don't split the result. When a backtick is never closed, CFWeaver can't tell where the literal text was meant to end, so it reports an error rather than guess. To include a backtick in literal text, wrap the text in two backticks instead of one.

Invalid:

```markdown
* Step: Low => 400 ? `limit < 1 | Ok => 200
```

Valid:

```markdown
* Step: Low => 400 ? `limit < 1` | Ok => 200
```

# Warnings

Warnings point out likely mistakes in a model that is otherwise well-formed. Their codes start at `1001`. CFWeaver writes them to standard error, or into its report with `--check -f json`, still writes its output, and exits with code `0`.

## `1001` No path reaches this step

No path through the operation reaches the step, so it never appears in a scenario. This usually means the results before it all return or branch past it.

```markdown
* First: Done => 200 | Skip = Third ? the item is cached
* Second: Done => 201
* Third: Done => 202
```

Here `First` either returns `200` or branches to `Third`, so `Second` is never reached.

## `1002` Result returns the name of a step; use `=` to go to that step

The result [returns](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#returning) text that is also the name of a step in the operation. That usually means it was meant to [branch](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#branching) to the step, which is written with `=`.

```markdown
* GetFromDatabase: Found => UpdateInDatabase | NotFound => 404 ? no item has this id
* UpdateInDatabase: Completed => 200 | Error => 500 ? the update fails
```

Here `Found` returns the text `UpdateInDatabase` instead of going to that step, so no path reaches `UpdateInDatabase`, and CFWeaver warns about that too. Writing `Found = UpdateInDatabase` fixes both.

## `1003` Step looks like it calls an operation, but doesn't handle one of its return values

The step has the same name as another operation, so it's read as calling it as a [sub-operation](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#sub-operations), and its results should be that operation's return values. One of those return values isn't among the step's results, so the model doesn't say what happens when the operation returns it. The warning names the operation and the return value.

```markdown
# Get Item

* Authenticate: Valid | Invalid => 401 ? the request has no valid token
* Lookup: Found => 200 | NotFound => 404 ? no item has this id

# Authenticate

* CheckToken: Valid | Missing => Invalid ? no token is sent
* LoadUser: Found => Valid | NotFound => Invalid ? the token's user was deleted | Error => Error ? the user store errors
```

Here Authenticate can return `Error`, but the `Authenticate` step in Get Item has no `Error` result. Adding one, such as `Error => 500 ? the user store errors`, fixes it. If the step isn't meant to call the operation, rename one of them.

## `1004` Step looks like it calls an operation that never returns one of its results

The step has the same name as another operation, so it's read as calling it as a [sub-operation](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#sub-operations), but one of the step's results isn't a value that operation returns. This usually means a typo, or that the operation no longer returns that value. The warning names the operation and the result.

```markdown
# Get Item

* Authenticate: Valid | Invalid => 401 ? the request has no valid token | Expired => 401 ? the token has expired
* Lookup: Found => 200 | NotFound => 404 ? no item has this id

# Authenticate

* CheckToken: Valid | Missing => Invalid ? no token is sent | Expired => Invalid ? the token has expired
* LoadUser: Found => Valid | NotFound => Invalid ? the token's user was deleted
```

Here Authenticate returns `Invalid` when the token has expired, so the `Expired` result in Get Item never happens.

## `1005` Results lead to scenarios with the same conditions; give them different conditions

Two or more scenarios in the operation have exactly the same [conditions](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#conditions), so the conditions don't say how to set up a test for one rather than the other. CFWeaver reports the step where those scenarios split, and names its results that lead to them. Give those results different conditions: every result except the one the step normally takes needs a condition, and no two can be the same.

```markdown
* Authorize: Allowed | Denied => 403 | Expired => 401
* Delete: Deleted => 204 | Failed => 500 ? the database errors
```

Here the scenarios through `Allowed` and `Deleted`, through `Denied`, and through `Expired` all have no conditions, so the warning names `Allowed`, `Denied` and `Expired`. `Allowed` is the step's usual result, so it can stay as it is; giving the other two conditions, such as `Denied => 403 ? the caller doesn't own the item` and `Expired => 401 ? the caller's session has expired`, fixes it.

On a step that calls a [sub-operation](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#sub-operations), the caller's table shows only the conditions written on that step's results, so give each returned value a condition that sums up what causes it. The sub-operation's own table has the details.