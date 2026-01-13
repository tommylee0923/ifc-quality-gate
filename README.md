# IfcQaTool

IfcQaTool is a lightweight, ruleset-driven IFC quality assurance (QA) and validation tool
built in **C#/.NET** using **xBIM**.

It is designed to run as a **command-line quality gate** for IFC files and output (JSON / CSV) that can be used in CI pipelines or future web apps.

The tool focuses on:
- Rule-based IFC validation
- Deterministic outputs
- Configurable rulesets (JSON)
- Automation-ready exit codes


## Requirements
- .NET SDK 7.0 or later
- IFC files compatible with xBIM (IFC2x3 / IFC4)


## Build & Run
Run directly using `dotnet`:

````bash
  dotnet run --project src/IfcQa.Cli -- check path/to/model.ifc
````

Specify a ruleset and output directory:

````bash
  dotnet run --project src/IfcQa.Cli -- check model.ifc --rules rulesets/basic-ifcqa.json --out out
````

Generate a catalog of available Psets and Qtos:

````bash
  dotnet run --project src/IfcQa.Cli -- catalog model.ifc --out out
````


## Rulesets

Rules are defined in JSON files and loaded at runtime.

Each ruleset contains:
- name        Human-readable name
- version     Ruleset version
- rules[]     List of rule definitions

Example (simplified):

{
  "name": "Basic IFC QA",
  "version": "0.2.0",
  "rules": [
    {
      "id": "R002",
      "type": "MissingContainment"
    },
    {
      "id": "P101",
      "type": "RequirePset",
      "ifcClass": "IfcWall",
      "pset": "Pset_WallCommon"
    }
  ]
}

Rulesets are validated on load:
- Duplicate rule IDs cause failure
- Missing required fields cause failure
- Unknown rule types cause failure


## Outputs

check command writes the following files into the output directory:

issues.json
-----------
Structured list of all detected issues.
Each issue includes:
- RuleId
- Severity (Info / Warning / Error)
- IfcClass
- GlobalId
- Name
- Message

issues.csv
----------
Same issues in CSV format for spreadsheets and reporting.

report.json
-----------
Summary metadata including:
- IFC file path
- Ruleset name/version/path
- Output directory
- Issue counts by severity
- Per-rule issue counts
- Unique elements affected


## Exit Codes (Quality Gate)
------------------------
The CLI can act as a quality gate using --fail-on.

Default behavior:
- Fails only on Error

Examples:

  IfcQa.Cli check model.ifc
    -> exit 0 if no Errors

  IfcQa.Cli check model.ifc --fail-on Warning
    -> exit 1 if any Warning or Error exists

Supported values:
- Error   (default)
- Warning
- Info
- None

Exit codes:
- 0 = pass
- 1 = quality gate failed
- 2 = invalid ruleset or input error


## Roadmap

- Relationship-based rules (IfcRelSpaceBoundary, semantic checks)
- Stable DTOs for web API integration
- IFC file hashing and run metadata
- CI examples and sample IFC fixtures


## License

MIT (or specify otherwise)
