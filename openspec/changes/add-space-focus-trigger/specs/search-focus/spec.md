## ADDED Requirements
### Requirement: Space Key Focus Trigger
When the main window is visible and the search box is not focused, the system SHALL focus the search box when Space is pressed and SHALL NOT append a space character to the search query.

#### Scenario: Space pressed while search box is unfocused
- **WHEN** the main window is visible
- **AND** the search box is not focused
- **AND** the user presses Space without Ctrl/Alt/Win modifiers
- **THEN** the search box gains focus
- **AND** the search query remains unchanged

### Requirement: Normal Input When Focused
When the search box is focused, the system SHALL handle keyboard input normally.

#### Scenario: Space pressed while search box is focused
- **WHEN** the search box is focused
- **AND** the user presses Space
- **THEN** the space character is inserted into the search query
