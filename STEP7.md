### Step 7 — Verification: Full Containerized Smoke Test

Read `AGENTS.md` and `IMPLEMENTATION_PLAN.md` first.

Step 6 is completed and approved. The current baseline is:

* Steps 0–6 completed.
* Latest Step 6 commit: `d338e6a`.
* Working tree is clean.
* Do NOT redo previous steps or change their architecture/design.

Your task now is **Step 7 — Verification only**, exactly as specified in `IMPLEMENTATION_PLAN.md` Section 22.

### Goal

Run the full application smoke test against a clean Docker Compose environment to verify that the complete system works end-to-end, not just through unit/integration tests.

### Required verification

1. Start the clean containerized environment:

   * `docker compose up -d`
   * Verify PostgreSQL becomes healthy.
   * Verify the API container starts successfully.
   * Verify `/health` returns a healthy response.

2. Execute the complete end-to-end flow against the Dockerized API:

   `Register Teacher`
   → `Create Teacher Platform`
   → `Login`
   → `Activate Platform`
   → `/api/platform/me`

3. Verify the expected authorization/tenant behavior:

   * Valid tenant + correct slug → successful protected request.
   * Authenticated Teacher A accessing Teacher B's protected `/api/platform/me` → `403`.
   * Correct PublicId + wrong slug → `301` canonical redirect, preserving the API suffix/path.
   * Invalid/nonexistent PublicId → `404`.
   * Anonymous access to the protected endpoint → `401` where applicable.
   * `/health` remains available and healthy.

4. Verify the environment is actually running from Docker/Compose rather than relying on the previously running development API or database.

5. Check container logs for startup/runtime errors or unexpected warnings that indicate an application problem.

6. After verification, clean up the test environment if appropriate and confirm the repository remains clean except for the intended Step 7 documentation/commit changes.

### Constraints

* Do NOT add new features.
* Do NOT redesign the architecture.
* Do NOT modify JWT design.
* Do NOT modify tenant isolation rules.
* Do NOT introduce Redis, message brokers, microservices, Kubernetes, CI/CD, or other infrastructure.
* Do NOT change existing behavior unless required to fix a genuine Step 7 verification failure.
* If a genuine application defect is discovered, fix only the minimum necessary change, add/update the relevant test if appropriate, and explain exactly why the change was necessary.
* Do not proceed to Step 8.
* Do not perform Git strategy/final repository work yet.

### Deliverable

Create/update the Step 7 verification documentation as required by `IMPLEMENTATION_PLAN.md`, including:

* Commands/environment used.
* Smoke-test results for every required scenario.
* HTTP status codes observed.
* Container health/startup result.
* Any issues encountered and how they were resolved.
* Final repository status.
* Final commit hash, if a commit is required by the implementation plan.

Then stop and wait for my review and approval.

Do not continue beyond Step 7.
