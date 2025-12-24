## 1. Scaffold
- [ ] 1.1 Create `src/ClipMate.Avalonia` with an Avalonia app entry and add it to `ClipMate.slnx`.
- [ ] 1.2 Add a shared bootstrap/composition layer for config, logging, app data path, and DI registration.

## 2. Composition and platform wiring
- [ ] 2.1 Move or mirror shared service registrations into the bootstrap layer without breaking WPF.
- [ ] 2.2 Extract UI-agnostic ViewModel base classes into a shared project and adapt WPF to use them.
- [ ] 2.3 Wire the Avalonia app to register Core/Service/Platform.Abstractions and Windows implementations.
- [ ] 2.4 Implement Avalonia-specific Windows adapters for window control, tray, and any WPF-only services.

## 3. UI parity
- [ ] 3.1 Implement MainWindow, clipboard list, and settings views in Avalonia XAML.
- [ ] 3.2 Port behaviors: no-activate, window position modes, search focus trigger, and global hotkeys.
- [ ] 3.3 Implement clipboard item rendering and actions (favorite, pin, delete-after-paste).
- [ ] 3.4 Implement theme switching, resources, and tray interactions to match WPF behavior.

## 4. Validation
- [ ] 4.1 Build `ClipMate.slnx` on Windows and confirm both apps compile.
- [ ] 4.2 Run Core/Service/Platform.Abstractions unit tests.
- [ ] 4.3 Smoke test Avalonia app: hotkey toggle, search, paste, settings persistence, tray menu.
