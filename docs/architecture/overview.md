# Architecture overview

Bilito uses a modular monolith. Each business capability will live under `src/Modules` and expose its application behavior through the API composition root. Identity is the first prepared module; only its persistence foundation exists in V0.

The API is the composition root. Infrastructure owns EF Core and passwordless identity implementation details. Contracts are deliberately separate from persistence models.

The external React frontend and the internal developer Backoffice are both HTTP clients of the API:

```text
React Frontend  ----HTTP----> Bilito.Api <----HTTP---- Bilito.Backoffice
                                      |
                                      v
                         Application / Infrastructure / Database
```

`Bilito.Backoffice` is a standalone Blazor WebAssembly client. It references only the public Identity contracts and never references Domain, Application, Infrastructure, EF Core, SQL Server, or Bilito.Database.
