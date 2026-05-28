# Galletto 1260 Manual Workflow

The EOBD Programmer 1260 / Galletto 1260 is represented only as a manual workflow device in this application.

## What The Application Does

- Records the programmer name and manual workflow mode.
- Imports ECU files read externally.
- Calculates SHA-256 hashes.
- Stores file metadata and backup references.
- Associates files with vehicles and ECU/EDU records.
- Runs defensive validation.
- Compares files.
- Generates technical reports.

## What The Application Does Not Do

- It does not read directly from the cable.
- It does not write to the ECU.
- It does not flash.
- It does not enter boot mode or bench mode.
- It does not control FTDI.
- It does not send K-Line commands.
- It does not perform seed-key/security access.
- It does not reverse engineer the original Galletto software.

## Safe Workflow

1. Use external Galletto 1260 software to read the ECU file.
2. Save the original file immediately.
3. Import the file into Safe ECU Calibration Manager.
4. Confirm vehicle, engine code, ECU/EDU reference and software version where possible.
5. Store the SHA-256 hash and metadata.
6. Generate a report before any further analysis.

Direct integration can only be considered in a future phase if safe documentation, SDK, API or validated protocol information exists.
