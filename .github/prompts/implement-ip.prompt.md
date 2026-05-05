---
mode: agent
description: Execute one Implementation Plan document end-to-end
---

# Implement an IP

## Instructions

You are executing a DotNetToolbox Implementation Plan. Follow these steps exactly.

### Step 1 — Read context

Read the following files before writing any code:

1. `doc/Overview.md` — understand the solution architecture
2. The Spec file referenced in the IP header (e.g. `doc/spec/Spec_Algorithms.md`)
3. The IP file itself: `{{IP_FILE}}`
4. Any existing API docs in `doc/api/` for libraries this IP depends on

### Step 2 — Implement

Create all files listed in the **Deliverables** section of the IP.
Follow the code sketches in the IP as the primary guide.
Apply all rules from `.github/copilot-instructions.md` and relevant `instructions/*.md` files.

### Step 3 — Build

```powershell
dotnet build DotNetToolbox.sln
```

Fix all errors and warnings before proceeding. Warnings are errors in this project.

### Step 4 — Run unit tests

```powershell
dotnet test DotNetToolbox.sln --filter "Category!=Integration"
```

All tests must pass. Fix failures before moving on.

### Step 5 — Run integration tests (if applicable)

If the IP includes integration tests:

```powershell
dotnet test DotNetToolbox.sln --filter "Category=Integration"
```

Requires `TOOLBOX_TEST_CONN` environment variable to be set.
If it is not set, report which tests were skipped and why.



### Step 6 — Generate API doc
After all tests pass, invoke generate-api-doc.prompt.md for this library and confirm doc/api/API_<LibraryName>.md is created or updated.


### Step 7 — Report

Summarise:
- Files created (list)
- Test results (pass/fail counts)
- Any deferred decisions or known limitations
- Any deviation from the IP and the reason
