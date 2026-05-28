# Security Policy

Safe ECU Calibration Manager is designed around defensive defaults. When the system does not have enough evidence, support or validation data, it must block.

## Blocked Operations

The current version does not implement:

- ECU writing or flashing.
- Boot mode, bench mode or recovery.
- Direct Galletto 1260 control.
- FTDI control.
- K-Line or proprietary commands.
- Seed-key/security access.
- ECU unlocking or tuning protection bypass.
- Immobilizer bypass.
- DPF, EGR, AdBlue/SCR or emissions system removal.
- DTC masking or permanent fault hiding.
- Real map modification.
- Automatic generation of modified ECU files.

## Technical Risks

ECU files can be incomplete, corrupted, read with the wrong tool, associated with the wrong vehicle, or from a different software version. The application therefore records hashes, metadata, source, read method and programmer information.

Checksum validation only means that a supported structural validation did not detect an error. It does not mean that calibration content is mechanically safe.

## Mechanical Risks

Unsafe calibration changes can damage engines, turbochargers, injectors, clutches, gearboxes and emissions systems. Future calibration workflows must pass through conservative safety limits, dependency checks and audit logging.

## Legal Risks

The application must not be used to defeat inspection, emissions compliance, diagnostics, immobilizer systems or manufacturer protections.

## Backup Requirements

A verified original backup is mandatory before any future operation that could produce a calibration change proposal.

## Future Hardware Operations

Any future ECU writing workflow would require validated documentation, API/SDK/protocol support, checksum support, stable power supply guidance, explicit confirmation and a dedicated safety phase.

Unknown or `NotSupported` always blocks.
