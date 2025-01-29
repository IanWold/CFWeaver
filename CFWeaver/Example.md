# Get an Item

* Authenticate: Success | Failure = 403 ? I am not authenticated to access items
* Validate: Success | Failure = 400 ? the request is invalid
* GetFromDatabase: Found ? the item exists | Not found = 404 ? the item does not exist | Error = 500 ? The items table errors on select
* Authorize: Success = 200 | Failure = 401 ? I am not authorized to access the specific item

# Create Item

* Authenticate: Success | Failure = 403 ? I am not authorized to write
* Validate: Success | Failure = 400 ? the request is invalid
* InsertIntoDatabase: Completed = 200 | NotCompleted = 404 ? the items table does not insert | Error = 500 ? the items table errors on insert

# Update Item

* Authenticate: Success | Failure = 403 ? I am not authorized to write
* Validate: Success | Failure = 400 ? the request is invalid
* UpdateOnDatabase: Completed = 200 | NotCompleted = 404 ? the items table does not update | Error = 500 ? the items table errors on update

# Delete Item

* Authenticate: Success | Failure = 403 ? I am not authorized to write
* Validate: Success | Failure = 400 ? the request is invalid
* DeelteFromDatabase: Completed = 200 | NotCompleted = 404 ? the items table does not delete | Error = 500 ? the templates table errors on delete