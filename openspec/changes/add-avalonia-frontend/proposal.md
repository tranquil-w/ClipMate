# Change: Add Avalonia frontend alongside WPF

## Why
The project needs a cross-platform UI path while keeping the current WPF app intact. We will start by implementing the Avalonia app on Windows only, reuse existing core/service layers, and share the same local data and configuration so users can switch without migration.

## What Changes
- Add a new Avalonia UI project in `src/ClipMate.Avalonia` and include it in `ClipMate.slnx`.
- Keep the existing WPF project unchanged and buildable.
- Implement full feature parity on Windows using the existing Core/Service/Platform.Abstractions layers.
- Share the same LocalApplicationData\ClipMate storage for settings, database, and logs.
- Introduce a cross-platform-ready composition path for platform services (Windows only for now).

## Impact
- Affected specs: `avalonia-frontend` (new)
- Affected code: new Avalonia project; shared app bootstrap/composition; potential refactors for UI-agnostic registration
