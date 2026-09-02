### Step 8 — Git Strategy: Final Repository Cleanup & History Review

Read `AGENTS.md` and `IMPLEMENTATION_PLAN.md` first.

Steps 0–7 are completed and approved.

Current baseline:

* Steps 0–7: `[x] Completed`
* Latest Step 7 commit: `ce17356`
* Working tree is clean.
* Dockerized smoke test passed completely.
* No production code or tests were modified in Step 7.

Your task now is **Step 8 only — Git strategy / final repository cleanup and history review**.

### Goal

Prepare the repository's Git history and working tree for a clean, professional project state without changing the application's architecture or functionality.

### Required work

1. Inspect the complete Git history for the implementation work:

   * Review commits from Step 0 through Step 7.
   * Check commit ordering and commit messages.
   * Identify accidental, temporary, duplicate, or debugging commits if any exist.
   * Review whether the history is understandable and logically organized.

2. Inspect the repository state:

   * `git status`
   * tracked/untracked files
   * `.gitignore`
   * repository documentation
   * generated/build/runtime files that should not be committed.

3. Verify that no secrets or environment-specific credentials are committed:

   * JWT signing keys
   * passwords
   * database credentials
   * private keys
   * real production `.env` files
   * Docker/runtime secrets
   * other sensitive configuration.

   `.env.example` is expected to remain tracked; real secrets must not.

4. Verify that generated artifacts are not unnecessarily tracked:

   * `bin/`
   * `obj/`
   * test/container artifacts
   * local Docker/runtime data
   * IDE/user-specific files
   * temporary logs/files.

5. Review `.gitignore` and make only the minimum necessary corrections if something that should be ignored is missing.

6. Review the commit history and determine whether any cleanup is actually necessary.

   **Important:** Do NOT rewrite Git history just for cosmetic reasons.

   If the existing history is already clean and understandable, preserve it as-is.

7. If history cleanup is genuinely necessary, use the safest minimal approach and document:

   * what was changed,
   * why it was necessary,
   * whether commit hashes changed,
   * and any implications for the remote repository.

8. Do NOT push to `origin` unless explicitly instructed.

9. Do NOT modify application code, tests, architecture, database design, JWT behavior, tenant isolation, or API behavior as part of this step.

10. Do NOT introduce any new infrastructure, features, dependencies, CI/CD, Redis, brokers, Kubernetes, etc.

### Final verification

After the Git review/cleanup:

* `git status` must be clean.
* Verify the final commit history.
* Verify no secrets or unwanted generated files are tracked.
* Verify `.gitignore` is appropriate.
* Run a final `dotnet build --warnaserror` and `dotnet test` if the repository state was changed; otherwise, at minimum confirm that no application files were modified.
* Do not make unrelated changes merely to produce a commit.

### Documentation

Update `IMPLEMENTATION_PLAN.md` with the Step 8 result and decision-log entry as specified by the existing plan.

If the repository convention requires a Step 8 documentation file, create it and commit it.

If no changes are necessary, do not create artificial changes just to produce a commit. Clearly document that the repository was reviewed and no cleanup was required.

### Commit

Only create a commit if there are legitimate Step 8 changes.

Use a clear commit message such as:

`chore: finalize git repository strategy`

Do not push the commit.

### IMPORTANT STOP CONDITION

This is **Step 8 only**.

After completing the Git strategy review and any necessary cleanup:

1. Report exactly what was inspected.
2. Report every change made, if any.
3. Report the final commit hash if a commit was created.
4. Report final `git status`.
5. Report whether any secrets/unwanted generated files were found.
6. Report whether history was rewritten or preserved.
7. Report test/build results if executed.

Then **STOP**.

Do NOT continue to any future step.
Do NOT start new features or refactoring.
Wait for my explicit review and approval.
