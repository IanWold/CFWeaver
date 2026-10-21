## Install

1. [Download](https://github.com/IanWold/CFWeaver/releases) CFWeaver for your platform
2. Extract the ZIP file to the location of your choice
3. Ensure the CFWeaver executable is visible to your terminal

## Learn

* [Syntax for control flow modeling](https://github.com/IanWold/CFWeaver/wiki/Control-Flow-State-Model)
    * [Parser errors](https://github.com/IanWold/CFWeaver/wiki/Parser-Errors)
    * [Modeling recipes](https://github.com/IanWold/CFWeaver/wiki/Modeling-Recipes)
* [Using CFWeaver with AI agents](https://github.com/IanWold/CFWeaver/wiki/AI-Agents)
* [CLI reference](https://github.com/IanWold/CFWeaver/wiki/CLI-Reference)

## Quick Start

Save the following as `example.md`:

```
# Get an Item

* Authenticate: Success | Failure => 403 ? I am not authenticated to access items
* Validate: Success | Failure => 400 ? the request is invalid
* GetFromDatabase: Found ? the item exists | Not found => 404 ? the item does not exist | Error => 500 ? The items table errors on select
* Authorize: Success => 200 | Failure => 401 ? I am not authorized to access the specific item
```

In your terminal, run `cfweaver example.md -o output.html`.

Run `cfweaver --help` for a summary of the options and the model syntax, and `cfweaver help recipes` for advice on modeling common patterns.