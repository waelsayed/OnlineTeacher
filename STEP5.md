## Step 5 — Dockerization & Deployment Hardening

Step 4 is approved.

Now proceed with **Step 5 only**, according to `IMPLEMENTATION_PLAN.md`.

Before changing anything:

1. Read `AGENTS.md`.
2. Read the completed implementation plan and Step 4 decisions.
3. Inspect the current Docker/hosting configuration.
4. Do not repeat or redesign completed work.

### Goal

Prepare the current OnlineTeacher backend for reliable containerized execution.

### Scope

Implement only the Dockerization/deployment hardening defined for Step 5, including where applicable:

* Dockerfile for the ASP.NET Core API using a proper multi-stage build.
* Production runtime image.
* Run the API in Docker together with PostgreSQL through Docker Compose.
* Environment-based configuration for:

  * PostgreSQL connection
  * JWT signing key
  * JWT configuration
  * other required runtime settings
* No secrets hard-coded in source control.
* Proper PostgreSQL health check / API dependency startup behavior.
* API health endpoint if it is part of the approved Step 5 plan.
* Reliable startup/shutdown behavior.
* Verify EF Core migrations/startup behavior in the containerized environment.
* Keep structured console logging suitable for Docker.
* Update `.env.example` and Docker documentation as necessary.

### Important architectural constraints

Do NOT:

* add microservices
* add Redis
* add message brokers/event bus
* add Kubernetes
* add CI/CD
* add cloud-specific infrastructure
* redesign the application architecture
* introduce unrelated features
* start Student/Course/Wall/etc. features
* change the tenant architecture
* change JWT/authorization behavior unless required to make Docker execution work

Keep the current:

* ASP.NET Core Web API
* .NET 10
* PostgreSQL
* EF Core
* layered architecture
* tenant isolation
* JWT authentication
* permission authorization

### Verification

Verify from a clean Docker environment, not only from Visual Studio:

1. PostgreSQL starts healthy.
2. API builds successfully into a Docker image.
3. API starts successfully in Compose.
4. Database initialization/migrations work as designed.
5. Register Teacher.
6. Create Platform.
7. Activate Platform.
8. Login.
9. Access protected `/api/platform/me`.
10. Verify wrong-tenant access is still rejected.
11. Verify canonical slug redirect still works.
12. Verify existing tests still pass.

Run:

```bash
dotnet build --warnaserror
dotnet test
docker compose config
docker compose build
docker compose up -d
```

Then verify the running containerized API and PostgreSQL.

Expected:

* Build: 0 warnings / 0 errors
* All unit tests pass
* All integration tests pass
* Docker Compose starts cleanly
* No secrets committed
* Working tree clean except intentional changes

Create one focused commit:

```text
chore: harden dockerized application runtime
```

Update `IMPLEMENTATION_PLAN.md` with the completed Step 5 status and decision log only where necessary.

### STOP CONDITION

Step 5 only.

Do NOT proceed to Step 6 or start implementing product features.

After completion, report:

* files changed
* Docker architecture/configuration
* environment variables introduced
* migration/startup behavior
* manual container verification
* build/test results
* Docker verification results
* commit hash
* remaining concerns

Then STOP and wait for human review.
