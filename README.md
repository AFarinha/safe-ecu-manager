# Safe ECU Calibration Manager

Safe ECU Calibration Manager is a Windows-first desktop application for safe ECU/EDU file management, documentation, binary comparison, reporting, auditability and future controlled calibration workflows.

The current application is intentionally conservative. It does not write to ECUs, flash ECUs, control hardware directly, bypass protection, alter emissions systems, or generate real modified calibration files.

## Current Capabilities

- Vehicle and ECU/EDU records.
- Manual ECU/EDU identification support.
- Safe programmer capability descriptions.
- File-only ECU file import workflow.
- SHA-256 hashing and duplicate detection.
- Defensive ECU file validation.
- Binary comparison and advanced difference scanning.
- HTML technical report generation.
- Audit log infrastructure.
- Checksum framework that returns `NotSupported` unless a validated algorithm exists.
- Guided calibration profile structure with blocking rules.
- Conservative safety limit and percentage intent validation engines.
- English UI text with Portuguese available as an application language.

## Architecture

The solution follows a modular Clean Architecture direction:

- `src/SafeEcu.App`: WPF desktop UI.
- `src/SafeEcu.Domain`: entities and domain models.
- `src/SafeEcu.Application`: use cases, validation and application services.
- `src/SafeEcu.Infrastructure`: SQLite persistence, files, logging and reports.
- `src/SafeEcu.Programmers`: safe programmer adapters.
- `src/SafeEcu.Calibration`: binary comparison services.
- `tests/SafeEcu.Tests`: unit and service tests.

Critical logic is kept outside the UI so it can be tested without a vehicle, ECU or cable.

## Stack

- C# / .NET 10
- WPF
- SQLite with EF Core
- xUnit
- HTML reports initially

## Run

```powershell
dotnet restore SafeEcuCalibrationManager.slnx
dotnet build SafeEcuCalibrationManager.slnx
dotnet run --project src\SafeEcu.App\SafeEcu.App.csproj
```

## Test

```powershell
dotnet test SafeEcuCalibrationManager.slnx
```

## Galletto 1260 Manual Workflow

The application does not control the EOBD Programmer 1260 / Galletto 1260 directly.

Expected workflow:

1. Read the ECU file using external Galletto 1260 software.
2. Save the original file.
3. Import the file into this application.
4. Store metadata, backup path and SHA-256 hash.
5. Associate the file with a vehicle and ECU/EDU record.
6. Validate, compare and report inside this application.

Direct read, direct write, flashing, K-Line commands, FTDI control, seed-key/security access and reverse engineering are not implemented.

## Initial Vehicle Targets

- Renault Megane 3 1.5 dCi K9K: primary target for file-only management, identification, comparison and future verified map workspace work.
- Opel Corsa C 1.7 DTI Y17DT/Y17DTI: secondary metadata target retained for documentation and regression coverage.

The application never assumes that all vehicles of the same model use the same ECU or software version.

## Limitations

- No ECU writing.
- No flashing, boot mode, bench mode or recovery.
- No direct hardware control.
- No real map editing.
- No real checksum algorithms unless validated in a future phase.
- No DPF/EGR/AdBlue/SCR removal or masking.
- No DTC masking or permanent fault hiding.
- No immobilizer, tuning protection or security bypass.

If support is unknown, the system must block rather than guess.

## Roadmap

The project is developed incrementally by phases. See [ROADMAP.md](ROADMAP.md).
