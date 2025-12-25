# Change: Add clipboard item min height and enforce height range

## Why
Users need a minimum height setting to control compactness while preventing invalid min/max combinations.

## What Changes
- Add a minimum clipboard item height setting (default 56).
- Change the default maximum clipboard item height to 56.
- Validate and enforce the min/max relationship when settings change.
- Update both WPF and Avalonia settings UI to edit the new minimum height.

## Impact
- Affected specs: settings
- Affected code: settings service, view models, WPF settings view, Avalonia settings view, appsettings defaults
