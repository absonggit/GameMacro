# Closed-loop Skill Detection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add non-OCR visual confirmation so cooldowns start only after the game icon visibly enters cooldown.

**Architecture:** A pure feature matcher classifies normalized icon samples, a debouncer stabilizes classifications, and a release state machine separates key dispatch from game-side confirmation. A Windows capture service supplies client-relative samples; WPF provides region selection and two-state calibration.

**Tech Stack:** .NET 8, WPF, Win32 GDI/User32, xUnit, System.Text.Json.

## Global Constraints

- Do not use OCR or game-memory access.
- Do not bypass anti-cheat or inject a driver.
- Do not commit changes to Git.
- A sent key is not a successful cast until cooldown is visually confirmed.

---

### Task 1: Persist Visual Calibration

**Files:**
- Modify: `src/GameMacro.Core/Models/MacroRule.cs`
- Test: `tests/GameMacro.Core.Tests/Models/MacroProfileTests.cs`

- [ ] Write a failing JSON round-trip test for normalized region coordinates and ready/cooldown feature arrays.
- [ ] Run the model test and verify it fails because calibration properties do not exist.
- [ ] Add `DetectionX/Y/Width/Height`, `ReadySignature`, and `CooldownSignature` with validation.
- [ ] Run model tests and verify they pass.

### Task 2: Classify and Debounce Icon State

**Files:**
- Create: `src/GameMacro.App/Detection/IconStateClassifier.cs`
- Create: `src/GameMacro.App/Detection/StableStateDetector.cs`
- Test: `tests/GameMacro.App.Tests/Detection/IconStateClassifierTests.cs`
- Test: `tests/GameMacro.App.Tests/Detection/StableStateDetectorTests.cs`

- [ ] Write failing tests for nearest-reference classification, ambiguous samples, and required consecutive samples.
- [ ] Run detection tests and verify expected failures.
- [ ] Implement normalized mean-centered signatures, distance-margin classification, and consecutive-state detection.
- [ ] Run detection tests and verify they pass.

### Task 3: Release Confirmation State Machine

**Files:**
- Create: `src/GameMacro.App/Timing/ClosedLoopReleaseController.cs`
- Modify: `src/GameMacro.App/Timing/AutoRotationScheduler.cs`
- Test: `tests/GameMacro.App.Tests/Timing/ClosedLoopReleaseControllerTests.cs`

- [ ] Write failing tests proving key dispatch does not start CD, two cooldown frames confirm success, and 800ms timeout permits retry without starting CD.
- [ ] Run controller tests and verify expected failures.
- [ ] Implement `WaitingReady`, `AwaitingCooldown`, and confirmed/timeout transitions.
- [ ] Run timing tests and verify they pass.

### Task 4: Capture Client-relative Skill Samples

**Files:**
- Modify: `src/GameMacro.App/Platform/NativeMethods.cs`
- Create: `src/GameMacro.App/Detection/WindowsSkillCaptureService.cs`
- Create: `src/GameMacro.App/RegionSelectionWindow.xaml`
- Create: `src/GameMacro.App/RegionSelectionWindow.xaml.cs`

- [ ] Add pure coordinate conversion tests for normalized client rectangles.
- [ ] Run tests and verify coordinate API is missing.
- [ ] Implement window-client capture and a transparent drag-selection overlay that returns normalized coordinates.
- [ ] Run coordinate and detection tests.

### Task 5: Wire Calibration and Automation UI

**Files:**
- Modify: `src/GameMacro.App/MainWindow.xaml`
- Modify: `src/GameMacro.App/MainWindow.xaml.cs`
- Modify: `README.md`

- [ ] Add buttons for region selection, ready recording, and cooldown recording plus calibration status.
- [ ] Require complete calibration before starting and pause on capture/window errors.
- [ ] Feed captured samples through classifier/controller and send only controller-requested keys.
- [ ] Update README with calibration workflow and closed-loop semantics.

### Task 6: Verify and Publish

**Files:**
- Modify: `scripts/publish.ps1` only if output naming is stale.

- [ ] Run `dotnet test GameMacro.sln -c Release --no-restore`; expect all tests pass.
- [ ] Run `dotnet build GameMacro.sln -c Release --no-restore`; expect zero warnings and errors.
- [ ] Publish self-contained win-x64 output to `artifacts/win-x64-auto`.
- [ ] Launch the published executable for four seconds, verify it responds, then close it.
