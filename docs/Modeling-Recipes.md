The model syntax is small; most of the work is deciding what to model. These recipes cover patterns that come up often, especially when modeling existing code. They're suggestions, not rules: the best model is the one that makes the scenarios you need to test clear. `cfweaver help recipes` prints this page.

## What Needs a Step

A _step_ is a point where the code can go more than one way. Code that always runs the same way, like logging or building a request, doesn't need a _step_ unless it can fail in a way you want to test.

## Exceptions

Give each _step_ that can throw a single `Failed` _result_, rather than one per exception type, unless the code handles different exceptions differently. If the code catches a timeout and returns 504, the timeout is its own _result_.

```markdown
# Save Item

* Validate: Valid | Invalid => 400 ? the body is missing a name
* Save: Saved => 201 | Conflict => 409 ? an item with the same name exists | Failed => 500 ? the database throws
```

## Validation Order

When several inputs can be invalid, order the _steps_ the way the code checks them. A request with both a bad limit and a bad sort follows the first failure, so the table has one row for each check rather than one for every combination of invalid inputs.

```markdown
# Search Items

* ValidateLimit: Valid | TooLarge => 400 ? limit is over 100
* ValidateSort: Valid | Unknown => 400 ? sort names a field that doesn't exist
* Search: Found => 200 | Failed => 500
```

If the code collects every validation error before responding, a single _step_ is enough: `Validate: Valid | Invalid => 400`.

## Loops

A loop that stops at its first failure has the same outcomes as a single item: every item succeeds, or one of them fails. Model one item as an _operation_ of its own, and give the _step_ that loops the same name, so it's read as calling it as a [sub-operation](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model#sub-operations).

```markdown
# Import Items

* ParseFile: Parsed | Malformed => 400 ? the file isn't valid CSV
* ImportRow: Imported | Rejected => 422
* Commit: Committed => 200 | Failed => 500

# ImportRow

* Validate: Valid | MissingName => Rejected ? the row has no name
* Insert: Inserted => Imported | Duplicate => Rejected ? a row with the same name exists
```

If the loop carries on past failures, model the outcomes the code acts on instead, such as `AllImported` and `SomeRejected`.

## Retries

Model a retry by its outcomes, not its attempts: success on the first attempt, success after a retry, and retries exhausted. Put the number of attempts and the delays in the description.

```markdown
# FetchPrice

Retries up to 3 times, waiting 200 ms between attempts.

* CallService: FirstAttempt => Price ? the first attempt succeeds | AfterRetry => Price ? an attempt fails but a retry succeeds | Exhausted => Unavailable ? every attempt fails
```

## State From Outside the Request

State that a test has to set up before the request, such as an open circuit breaker, a cached item or a feature flag, goes in the condition of the _result_ it causes. Don't model how that state came about, such as the failures that opened the circuit.

```markdown
# Get Recommendations

* Breaker: Closed | Open => 503 ? the circuit breaker is open
* CallService: Returned => 200 | Failed => 503 ? the recommendation service errors
```

## Framework Behavior

Behavior you don't model, such as routing, deserialization or serialization handled by a framework, belongs in the _operation_'s description as an assumption. CFWeaver copies descriptions into its output, so readers can see what the scenarios take for granted.

```markdown
# GET /items/{id}

Assumes the framework has matched the route and parsed `id` as an integer; other routes return 404 before this code runs.

* Authorize: Allowed | Denied => 403 ? the caller can't read items
* Lookup: Found => 200 | Missing => 404
```

## When the Caller Needs to Know Why

A caller sees only the value a _sub-operation_ returns, not how it got there. Below, `FetchItem` returns `Unavailable` both when the circuit breaker is open and when the service errors. That's fine, because the endpoint does the same thing in both cases.

```markdown
# GET /items/{id}

* FetchItem: Found => 200 | NotFound => 404 | Unavailable => 503

# FetchItem

* Breaker: Closed | Open => Unavailable ? the circuit breaker is open
* CallService: Ok => Found | Missing => NotFound ? the service returns 404 | Error => Unavailable ? the service returns 500
```

If the endpoint treated them differently, for example by passing on the service's status code, which an open breaker doesn't have, `FetchItem` would need a separate return value for each case. Sharing one value would let the model describe paths the code can't take.

## Conditions Are Descriptions

CFWeaver joins the conditions along each path with "and", but it doesn't check that they can all be true at once. When one _result_ rules out a later _step_, branch past that _step_ so the impossible path isn't in the table.

```markdown
# Upsert Item

* Lookup: Found | Missing = Insert ? no item has this id
* Update: Updated => 200 | Conflict => 409 ? the item changed since it was read
* Insert: Inserted => 201
```

Without `= Insert`, the table would include scenarios that update an item that doesn't exist, and CFWeaver would warn that no path reaches `Insert`.