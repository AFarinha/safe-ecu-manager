# Roadmap

Safe ECU Calibration Manager is built incrementally. Each phase should remain small, tested and auditable.

## Implemented

- Phase 0: project analysis.
- Phase 1: base WPF application shell.
- Phase 2: SQLite persistence and data model.
- Phase 3: vehicle management.
- Phase 4: safe programmer adapters.
- Phase 5: ECU file import.
- Phase 6: defensive ECU file validation.
- Phase 7: binary comparison.
- Phase 8: HTML technical reports.
- Phase 9: ECU/EDU identification workflow.
- Phase 10: checksum framework with `NotSupported` defaults.
- Phase 11: ECU family support profiles.
- Phase 12: advanced binary difference scanning.
- Phase 13: audit log service and persistence.
- Phase 14: conservative safety limit engine.
- Phase 15: percentage intent profile validation.
- Phase 16: percentage intent engine.
- Phase 17: guided calibration profile catalog.
- Phase 18: controlled parameter validation rules.
- Phase 19: calibration rule engine.
- Phase 20: profile percentage limit validation.
- Phase 21: safety regression tests.
- Phase 22: documentation.

## Planned

- Renault Megane 3 focused workflow: stronger K9K variant, ECU family and software evidence capture.
- Renault candidate ECU profiles, starting with metadata-only Delphi DCM/Bosch EDC identification.
- UI integration for audit records.
- UI integration for guided profile availability.
- More complete technical report sections for audit and safety results.
- ECU family support expansion only when real evidence and test files exist.
- Checksum algorithms only when validated.
- PDF reports after HTML is stable.

## Prohibited In This Version

- ECU writing.
- Flashing.
- Direct hardware control.
- Real map editing.
- Emissions deletes or masking.
- DTC masking.
- Immobilizer or security bypass.
- Reverse engineering of Galletto software.

## Galletto 1260 Status

`ManualWorkflowOnly`.

The user must read ECU files with external software and import files into this application.

## Vehicle Support Status

- Renault Megane 3 1.5 dCi K9K: primary file management and documentation target. Exact ECU, K9K variant and software must be confirmed.
- Opel Corsa C 1.7 DTI Y17DT/Y17DTI: secondary metadata target.

No calibration output is verified for either vehicle.
