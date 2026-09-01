Approved. Exit Plan Mode and start implementation.

Execute **Step 0 only** from `IMPLEMENTATION_PLAN.md`.

### Step 0 — Scaffolding & Tooling

Implement only the following:

1. Create the .NET 10 solution and project structure according to `AGENTS.md` and `IMPLEMENTATION_PLAN.md`.
2. Configure the projects for:

   * ASP.NET Core Web API
   * .NET 10
   * C#
   * PostgreSQL
   * Docker / Docker Compose
3. Add only the required NuGet packages specified in the implementation plan.
4. Create/update:

   * `.gitignore`
   * `.env.example`
   * `docker-compose.yml`
5. Configure PostgreSQL using:

   * `postgres:16-alpine`
   * Persistent named Docker volume
   * Environment-based database name, username, password, and port
   * PostgreSQL healthcheck using `pg_isready`
6. Do not commit or create a real `.env` file.
7. Keep the scaffold clean and minimal. Do not introduce unnecessary libraries, abstractions, or architecture beyond what is required for Step 0.

### Important Domain Constraint

The following routing/database rule is already approved and must be preserved in all future implementation:

* `PublicId` is globally unique.
* `Slug` is **NOT globally unique**.
* Create a unique index/constraint for `PublicId`.
* Create a non-unique index for `Slug`.
* Do not implement any global slug-duplicate prevention or concurrency guard.
* URL resolution remains:

  * PublicId not found → `404`
  * PublicId found + slug matches current canonical slug → `200`
  * PublicId found + slug does not match current canonical slug → `301` redirect to the canonical URL
* The route must validate both `PublicId` and `Slug`; never treat PublicId alone as a valid route when the supplied slug is wrong.

This constraint is relevant for later steps, but **do not implement the domain/database logic now unless it is strictly required for the scaffold**.

### Scope Control

Do NOT implement Step 1 or later steps.

Specifically, do not implement yet:

* Domain entities
* Business services
* Authentication
* JWT
* Permissions
* Tenant resolution
* EF Core DbContext
* Migrations
* API endpoints
* Authorization
* Business rules
* Integration tests

### Verification

After completing Step 0:

1. Run `dotnet restore`.
2. Run `dotnet build`.
3. Validate the Docker Compose configuration.
4. Start PostgreSQL with Docker Compose.
5. Verify the PostgreSQL container becomes healthy.
6. Verify the application solution/project structure is valid.

### Reporting

Before stopping, provide a concise implementation report containing:

* Files created
* Files modified
* Projects created
* NuGet packages and versions
* Docker/PostgreSQL configuration
* Verification commands and their results
* Any warnings or deviations from `AGENTS.md` / `IMPLEMENTATION_PLAN.md`

Update the `Implementation Status` and `Decision Log` in `IMPLEMENTATION_PLAN.md` as appropriate.

`IMPLEMENTATION_PLAN.md` and `UPDATEPLAN1.md` are currently untracked. Commit both together with the Step 0 scaffold.

After Step 0 is complete and verified, **STOP**.

Do not proceed to Step 1 until I explicitly approve it.
