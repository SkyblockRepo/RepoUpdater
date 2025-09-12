# Skyblock Repo Tools

A monorepo containing Skyblock Repo autoupdating scripts and api.

> [!WARNING]
> This is not production ready yet! This is very early and all data/schemas are subject to change

## Contributing

### Fixing/adding data to the Repo

- **Fixes to data parsing:** Must be made in the backend ASP.NET application to be persisted.
- **Fixes to a specific file:** Can be made in the `overrides` folder.
  - `json` files will be merged with the data in the database/repo. So making a file `items/MY_ITEM_ID.json` with these contents:
    ```json
    {
      "name": "Corrected Name"
    }
    ```
    Will have this name property overwrite the existing name, but keeps the rest of the data intact.
- **Deleting a file:** For the rare case of a file that shouldn't exist (and excluding it in the logic wouldn't make sense), add a new line to `overrides/exclusions.txt` with the file path.
