---
mode: agent
description: Generate API reference documentation from implemented source code
---

# Generate API Reference Documentation

## Instructions

Generate an API reference document for a completed DotNetToolbox library.
The output file goes in `doc/api/` and must be suitable for calling code (consumers of the library).

### Step 1 — Identify source

Determine which library to document from the user's request or the current context:

- `DotNetToolbox.Algorithms` → source in `src/DotNetToolbox.Algorithms/`
- `DotNetToolbox.Data.Csv` → source in `src/DotNetToolbox.Data.Csv/`
- `DotNetToolbox.Data.SqlServer` → source in `src/DotNetToolbox.Data.SqlServer/`

### Step 2 — Read source files

Read all public `.cs` files in the library's source directory.
Focus on:
- Public classes, records, and structs
- Public interfaces
- All public methods, properties, and constructors
- Existing XML doc comments

### Step 3 — Read the Spec

Read the corresponding spec in `doc/spec/` to ensure the API doc accurately reflects **intended** behaviour,
not just what was implemented.

### Step 4 — Generate the API doc

Create `doc/api/API_<LibraryName>.md` with the following structure:

```markdown
# API Reference: <Library Name>

## Overview
One paragraph describing what the library does and when to use it.

## Namespace: <Namespace>

### Class: <ClassName>
> Brief one-line summary from XML doc.

**Thread Safety:** [thread-safe / not thread-safe / immutable]

#### Constructor(s)

| Constructor | Description |
|---|---|
| `ClassName(param1, param2)` | What it creates |

#### Methods

| Method | Returns | Description |
|---|---|---|
| `MethodName(param1, param2)` | `ReturnType` | What it does |

#### Properties

| Property | Type | Description |
|---|---|---|
| `PropertyName` | `Type` | What it represents |

#### Exceptions

| Exception | When Thrown |
|---|---|
| `ArgumentNullException` | When X is null |

### Usage Example

\`\`\`csharp
// Minimal working example
\`\`\`
```

### Step 5 — Validate

Check that:
- Every public type in source has a section in the API doc
- No `internal` types are documented
- Usage examples compile (mentally verify — no build required)
- The doc is consistent with `doc/spec/Spec_*.md`

### Step 6 — Report

State:
- Output file created
- List of public types documented
- Any spec/implementation discrepancies found
