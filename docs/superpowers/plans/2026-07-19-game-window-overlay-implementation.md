# Game Window Overlay Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a target-game overlay that shows the selected profile and controls profile switching and recognition state safely.

**Architecture:** MainWindow remains the single owner of profile selection and automation. A new GameOverlayWindow is a presentation-only surface driven by a tested profile filter and relative placement helper.

**Tech Stack:** .NET 8, WPF, xUnit, Win32 window foreground APIs.

## Global Constraints

- Only one profile may run at a time.
- Profile switching is allowed only while recognition is stopped.
- The overlay is visible only for the target game foreground context.
- Do not create a Git commit.

---

### Task 1: Overlay profile selection policy

**Files:**
- Create: `src/GameMacro.App/Overlay/OverlayProfilePolicy.cs`
- Test: `tests/GameMacro.App.Tests/Overlay/OverlayProfilePolicyTests.cs`

**Interfaces:**
- `ProfilesForTarget(IEnumerable<MacroProfile>, MacroProfile)` returns same-target profiles.
- `CanSwitch(bool isRunning)` returns false while running.

- [ ] Write failing tests for process filtering, title fallback, and the running-state switch lock.
- [ ] Run focused tests and confirm the policy type is missing.
- [ ] Implement ordinal-ignore-case filtering and switch policy.
- [ ] Run focused tests and confirm pass.

### Task 2: Relative overlay placement

**Files:**
- Create: `src/GameMacro.App/Overlay/OverlayPlacement.cs`
- Test: `tests/GameMacro.App.Tests/Overlay/OverlayPlacementTests.cs`

**Interfaces:**
- Converts saved normalized coordinates into screen pixels inside target client bounds.
- Converts a dragged screen location back into clamped normalized coordinates.

- [ ] Write failing round-trip and clamping tests.
- [ ] Run focused tests and confirm failure.
- [ ] Implement pure coordinate conversion methods.
- [ ] Run focused tests and confirm pass.

### Task 3: Overlay window UI

**Files:**
- Create: `src/GameMacro.App/GameOverlayWindow.xaml`
- Create: `src/GameMacro.App/GameOverlayWindow.xaml.cs`
- Modify: `src/GameMacro.App/GameMacro.App.csproj` only if WPF item discovery requires it.

**Interfaces:**
- Exposes `ProfileSelectionRequested`, `ToggleRequested`, and `OverlayMoved` events.
- `UpdateState` refreshes profile list, current name, running state, and control availability.

- [ ] Implement compact dark WPF window with profile selector, state badge, and toggle button.
- [ ] Make it topmost, absent from taskbar, draggable only while stopped, and non-owning of runtime logic.
- [ ] Build the app and correct XAML/compiler errors.

### Task 4: Main window integration and persistence

**Files:**
- Modify: `src/GameMacro.Core/Models/MacroProfile.cs`
- Modify: `src/GameMacro.App/MainWindow.xaml`
- Modify: `src/GameMacro.App/MainWindow.xaml.cs`
- Modify: `tests/GameMacro.App.Tests/Services/JsonProfileStoreTests.cs`

**Interfaces:**
- Adds persisted `ShowGameOverlay` with default true.
- MainWindow owns overlay visibility timer, selection synchronization, and start/stop synchronization.

- [ ] Write a failing JSON round-trip assertion for `ShowGameOverlay` and normalized position.
- [ ] Run focused test and confirm failure.
- [ ] Add profile persistence and the main-window overlay checkbox.
- [ ] Wire overlay events to existing profile selection and `ToggleMonitoring` methods.
- [ ] Hide overlay outside target foreground context and reposition it relative to the game client.
- [ ] Run focused tests and build successfully.

### Task 5: Documentation and release verification

**Files:**
- Modify: `README.md`

- [ ] Document visibility, stop-before-switch behavior, and drag-position persistence.
- [ ] Run `dotnet test GameMacro.sln --no-restore` and require zero failures.
- [ ] Publish to `artifacts/win-x64-game-overlay`.
- [ ] Confirm `GameMacro.App.exe` exists in the new directory.

