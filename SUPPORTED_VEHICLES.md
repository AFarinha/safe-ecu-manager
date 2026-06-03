# Supported Vehicles

Support in this project means safe file management and documentation unless explicitly stated otherwise.

No vehicle currently has verified real calibration output support.

## Renault Megane 3 1.5 dCi

- Priority: primary.
- Engine code: K9K, exact variant to be confirmed.
- Fuel: diesel.
- Probable ECU: Delphi DCM or Bosch EDC, to be confirmed by label, diagnostic data or file evidence.
- Programmer workflow: file-only/manual workflow until a validated toolchain exists.
- Current support: file management, metadata, backup, SHA-256, validation, comparison, raw map workspace preview and reports.
- Direct writing: `NotSupported`.
- Real map changes: `NotSupported`.
- Checksum support: `NotSupported` unless a validated algorithm is added later.

Required evidence:

- Exact K9K variant.
- ECU hardware reference.
- ECU software reference.
- Software version/calibration identifier.
- ECU family confirmed by label, diagnostic data or trusted documentation.
- Original file size.
- Original file SHA-256 hash.
- File origin and read method.
- Programmer/tool used.
- Backup date.

## Opel Corsa C 1.7 DTI

- Priority: secondary.
- Engine codes: Y17DT / Y17DTI.
- Fuel: diesel.
- Probable ECU/EDU: Isuzu/Delco/Delphi-related, to be confirmed on the real vehicle.
- Programmer workflow: EOBD Programmer 1260 / Galletto 1260 manual workflow.
- Current support: file management, metadata, backup, SHA-256, validation, comparison and reports.
- Direct writing: `NotSupported`.
- Real map changes: `NotSupported`.
- Checksum support: `NotSupported` unless a validated algorithm is added later.

Required evidence:

- Engine code.
- ECU/EDU hardware reference.
- ECU/EDU software reference.
- Software version.
- Original file size.
- Original file SHA-256 hash.
- File origin and read method.
- Programmer used.
- Backup date.

## Important Rule

The application must never assume that all cars of the same model use the same ECU, software version or file layout.

Without confirmation, support remains `Unknown` or `NotSupported`.
