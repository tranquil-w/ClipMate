## ADDED Requirements

### Requirement: Minimum clipboard item height setting
The system SHALL provide a configurable minimum clipboard item height setting with a default value of 56.

#### Scenario: Default minimum height
- **WHEN** the application loads settings without an explicit minimum height
- **THEN** the minimum clipboard item height is 56

## MODIFIED Requirements

### Requirement: Clipboard item height range validation
The system SHALL enforce that the minimum clipboard item height is less than or equal to the maximum clipboard item height.

#### Scenario: User sets minimum greater than maximum
- **WHEN** a user sets the minimum height to a value greater than the maximum height
- **THEN** the system adjusts values to preserve min <= max and persists the adjusted values

### Requirement: Default maximum clipboard item height
The system SHALL use 56 as the default maximum clipboard item height when no explicit value is configured.

#### Scenario: Default maximum height
- **WHEN** the application loads settings without an explicit maximum height
- **THEN** the maximum clipboard item height is 56
