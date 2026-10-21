These instructions are for AI agents using CFWeaver. `cfweaver help agents` prints them, and `cfweaver help skill` prints them as a `SKILL.md` file for agents that support skills.

CFWeaver turns a control flow state model (CFSM), written in Markdown, into one test scenario for every path through it. It doesn't write tests, and it doesn't compare the model with the code it describes: a model CFWeaver accepts is well-formed, not necessarily accurate. Your job is to model the system faithfully; CFWeaver's job is to check the model's structure and list every path.

## Workflow

1. Run `cfweaver --help` for the model syntax, and `cfweaver help recipes` before modeling existing code. If `cfweaver` isn't on your `PATH`, ask the user where it's installed.
2. Find the operations, usually one for each endpoint, command or other unit you'd test. In each operation, find the decision points, the places where the code can go more than one way. Each decision point is a step.
3. Write the model as a Markdown file. Put source references, assumptions and simplifications in each operation's description, the text between its heading and its first step; text before the first operation describes the whole file. CFWeaver copies descriptions into its output, so readers can see what the model is based on.
4. Check the model with `cfweaver model.md --check -f json`, and repeat until `wellFormed` is `true`. Each diagnostic has a `code`, a `line` and a `message`, and `cfweaver help <code>` explains a code with examples. Warnings don't stop the output, but each one usually points to a mistake in the model: fix it, or tell the user why it's intended.
5. Generate the outputs. `cfweaver model.md -o - -f json` writes every scenario as JSON to standard output, for writing tests from. `cfweaver model.md -o report.html` writes a report with a diagram of each operation, for people to review; add `--self-contained` if it will be viewed without internet access.
6. Review the model against the source once more before reporting it as done.

## Rules That Are Easy to Get Wrong

- `=` goes to a step, and `=>` returns a value: `Found = Update`, `NotFound => 404`. A result with neither continues to the next step.
- A result can only go to a later step, so there are no loops. `cfweaver help recipes` shows how to model a loop with a sub-operation.
- Every result on the last step of an operation must return a value.
- Step names are single words, and are unique within their operation.
- After `?`, a result's details are separated by `&`. A detail written `name=value`, with a one-word name, sets a variable; any other detail is the result's condition, and a result has at most one. Variables never go straight after a return value.
- Each scenario's conditions are what its test has to set up, so they should differ between scenarios. Give every result a condition except the one its step normally takes, and never give two results on one step the same condition. This includes a step that calls a sub-operation: the caller's table shows only the conditions written on that step, so sum up there what causes each returned value. Warning 1005 points out scenarios that share conditions.
- `|`, `&`, `=` and `?` are reserved. Wrap code, or any other text containing them, in backticks: `` Invalid => 400 ? `limit < 1 || limit > 100` ``.
- A step with the same name as another operation calls that operation as a sub-operation, so its results must be exactly the operation's return values. To call an operation from a step with a different name, such as one helper used by two steps, or an operation whose name has spaces, put the operation's name in brackets after the step's: `* BindExclude [Parse Filter]: Parsed | Invalid => 400 ? an exclude token is malformed`. The call isn't expanded: each operation keeps its own scenario table.

## Example

```markdown
Models the Items API in ItemsController.cs.

# GET /items/{id}

Source: ItemsController.cs:20-48. Assumes routing has already parsed `id` as an integer.

* Authenticate: Valid | Invalid => 401 ? the request has no valid token
* Lookup: Found => 200 ? Cache=miss | Cached => 200 ? the item is in the cache & Cache=hit | NotFound => 404 ? no item has this id | Failed => 500 ? the database throws

# Authenticate

Source: AuthMiddleware.cs:12-40.

* CheckToken: Valid | Missing => Invalid ? no bearer token is sent | Expired => Invalid ? `token.ExpiresAt <= DateTime.UtcNow`
* LoadUser: Found => Valid | NotFound => Invalid ? the token's user was deleted
```

## Reporting to the User

- Say that the model is well-formed, not that it's correct or verified: CFWeaver doesn't compare it with the code.
- A total scenario count adds up every operation's table, including sub-operations, so it isn't a count of end-to-end tests. The `--check` summary marks the operations that other operations call; report the others' counts separately.
- List the assumptions and simplifications you made, such as loops or retries summarized by their outcomes, and anything you couldn't determine from the source.
