## Context
- The current app is a Windows-only WPF application.
- The request is to add an Avalonia-based UI that preserves WPF and enables future cross-platform expansion.
- Existing Core/Service/Platform.Abstractions layers should be reused, and data/config should be shared.

## Goals / Non-Goals
- Goals:
  - Add an Avalonia UI app for Windows with full feature parity.
  - Keep WPF unchanged and buildable in the same solution.
  - Share LocalApplicationData\ClipMate for settings, database, and logs.
  - Use platform abstractions so non-Windows implementations can be added later.
- Non-Goals:
  - Remove or rewrite the WPF app.
  - Change database schema or storage format.
  - Deliver non-Windows platform implementations in this change.

## Decisions
- Decision: Create `src/ClipMate.Avalonia` and add it to `ClipMate.slnx` as a separate UI entry point.
- Decision: Reuse `ClipMate.Core`, `ClipMate.Service`, and `ClipMate.Platform.Abstractions` as-is.
- Decision: Add Avalonia-specific Windows adapters where WPF-specific types are used (window control, tray, etc.).
- Decision: Centralize app data path, configuration, logging, and service registration in a UI-agnostic bootstrap layer so both WPF and Avalonia use the same wiring.
- Decision: Use Avalonia 11 LTS (latest stable LTS line) for the new UI project.
- Decision: Extract UI-agnostic ViewModel base classes to share logic between WPF and Avalonia, with UI-specific wrappers only where needed.

## Alternatives considered
- Replace WPF with Avalonia (rejected: request requires keeping WPF).
- Duplicate all registration logic in the Avalonia app (rejected: risk of divergence).
- Introduce Prism for Avalonia (rejected for now: avoid new dependency uncertainty).

## Risks / Trade-offs
- Parity drift between UIs: mitigate with a parity checklist and shared logic where possible.
- DB contention if both apps run concurrently: mitigate with single-instance checks and cautious DB access.
- Refactoring registration logic may impact WPF startup: mitigate with incremental changes and tests.

## Migration Plan
- No data migration required; both apps use the same data path.
- Keep WPF as the default app; Avalonia app is additive.
- Avoid running both apps simultaneously on the same profile.

## Open Questions
- None.
