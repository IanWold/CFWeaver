# Get Checklist

* Authenticate: Success | Failure = 403 ? I am not authorized to write
* Validate: Success | Failure = 400 ? the request is invalid
* GetFromDatabase: Found = GetCountsFromRedis ? the checklist already exists | NotFound ? the checklist does not exist | Error = 500 ? the checklists table errors on select
* AuthenticateGrapi: Success | Failure = 500 ? the server is not authorized for GRAPI | Error = 500 ? the auth server errors
* GetFromGrapi: Found | NotFound = 404 ? GRAPI responds not found | Error = 500 ? GRAPI errors
* GetTypesFromRedis: Success | Error = 500 ? Redis product-type keys error
* InsertIntoDatabase: Success | Error = 500 ? the checklists table errors on insert
* GetCountsFromRedis: Success = 200 | Error = 500 ? Redis product-type-counts keys error

# Update Product IsChecked

* Authenticate: Success | Failure = 403 ? I am not authorized to write
* Validate: Success | Failure = 400 ? the request is invalid
* UpsertIntoDatabase: CompletedInsert = 200 ? the checklist product does not exist | CompletedUpdate = 200 ? the checklist product already exists | NotCompleted = 404 ? the checklists_products table does not update | UpdateError = 500 ? the checklists_products table errors on update | InsertError = 500 ? the checklists_products table errors on insert

# Update ShowCompleted

* Authenticate: Success | Failure = 403 ? I am not authorized to write
* Validate: Success | Failure = 400 ? the request is invalid
* UpdateDatabase: Completed = 200 ? the checklist exists | NotCompleted = 404 ? the checklist does not exist | Error = 500 ? the checklists table errors on update

# Update Products IsHidden

* Authenticate: Success | Failure = 403 ? I am not authorized to write
* Validate: Success | Failure = 400 ? the request is invalid
* UpsertIntoDatabase: CompletedInsert = 200 ? the checklist product does not exist | CompletedUpdate = 200 ? the checklist product already exists | NotCompleted = 404 ? the checklists_products table does not update | UpdateError = 500 ? the checklists_products table errors on update | InsertError = 500 ? the checklists_products table errors on insert

# Upsert Template

* Authenticate: Success | Failure = 403 ? I am not authorized to write
* Validate: Success | Failure = 400 ? the request is invalid
* UpsertIntoDatabase: Completed = 200 | NotCompleted = 404 ? the templates table does not insert | Error = 500 ? the templates table errors on update

# RegistryItemAdded Event

* Authenticate: Success | Failure = 403 ? I am not authorized to write
* Validate: Success | Failure = 400 ? the request is invalid
* GetTypesFromRedis: Success | NotFound = 200 ? the Redis product-type keys return empty | Error = 500 ? Redis keys "product-type" error
* UpsertIntoDatabase: CompletedInsert = 200 ? the checklist product does not exist | CompletedUpdate = 200 ? the checklist product already exists | NotCompleted = 404 ? the checklists_products table does not update | UpdateError = 500 ? the checklists_products table errors on update | InsertError = 500 ? the checklists_products table errors on insert
