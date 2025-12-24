## ADDED Requirements
### Requirement: Avalonia Frontend Project
The system MUST add a new Avalonia UI application project that builds on Windows and coexists with the existing WPF app in `ClipMate.slnx`.

#### Scenario: Build both apps
- **WHEN** building `ClipMate.slnx` on Windows
- **THEN** both `ClipMate` (WPF) and `ClipMate.Avalonia` projects are included and can be built independently

### Requirement: Feature Parity on Windows
The Avalonia app MUST provide the same user-visible functionality as the WPF app on Windows, including:
- Clipboard history capture for text, images, and file lists
- Search with space-triggered focus behavior
- Global hotkey to show/hide the window and optional favorite filter hotkey
- Smart paste behavior (simulate Ctrl+V) and optional delete-after-paste for non-favorites
- Favorites, pin-to-top support, and duplicate detection
- History limit with automatic cleanup (favorites excluded)
- Window position modes (follow caret, follow mouse, screen center) and no-activate behavior
- Theme switching (light, dark, system)
- Tray icon and background operation
- Auto-start, silent start, always-run-as-admin, and configurable log level

#### Scenario: Core workflow
- **WHEN** a user invokes the hotkey, searches, and selects an item
- **THEN** the Avalonia app filters the history and pastes the selected item

#### Scenario: Settings change
- **WHEN** a user updates settings in the Avalonia app
- **THEN** the app applies the settings and persists them

### Requirement: Shared Storage and Configuration
The Avalonia app MUST use the same `LocalApplicationData\\ClipMate` storage location as the WPF app for settings, database, and logs.

#### Scenario: Shared history
- **WHEN** a clipboard item is captured by one app
- **THEN** it is visible in the other app without data migration

### Requirement: Cross-Platform Ready Composition
The Avalonia app MUST use platform abstractions for clipboard, hotkey, tray, and windowing services, with Windows implementations wired by default and a structure that allows future non-Windows implementations.

#### Scenario: Windows runtime wiring
- **WHEN** the Avalonia app runs on Windows
- **THEN** it uses `ClipMate.Platform.Windows` services for platform functionality
