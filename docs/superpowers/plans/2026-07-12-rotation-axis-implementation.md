# Rotation Axis Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add burst, filler, and short-CD insertion axes driven exclusively by captured PNG availability.

**Architecture:** Store role flags on each skill and implement a stateful `RotationAxisScheduler` independent of WPF. The window classifies every enabled icon each tick, asks the scheduler for one candidate, applies the existing three-frame debounce, then reports successful sends back to advance the axis.

**Tech Stack:** .NET 8, WPF, xUnit, System.Text.Json

## Global Constraints

- Do not use OCR, game memory, predicted cooldowns, or anti-cheat bypasses.
- Do not commit changes.
- PNG readiness is mandatory for every send.

---

### Task 1: Axis scheduler

**Files:**
- Create: `src/GameMacro.App/Timing/RotationAxisScheduler.cs`
- Create: `tests/GameMacro.App.Tests/Timing/RotationAxisSchedulerTests.cs`
- Modify: `src/GameMacro.Core/Models/MacroRule.cs`

- [ ] Write tests for complete burst readiness, locked burst order, insertion priority, filler continuation, and no repeat before PNG changes.
- [ ] Run tests and confirm failure before implementation.
- [ ] Add role flags and implement the minimal scheduler.
- [ ] Run focused tests to green.

### Task 2: Runtime integration

**Files:**
- Modify: `src/GameMacro.App/MainWindow.xaml.cs`

- [ ] Classify all enabled skills once per tick.
- [ ] Select through `RotationAxisScheduler`, preserve three-frame debounce, and advance only after a successful key send.
- [ ] Reset scheduler state on start and stop.

### Task 3: UI and emergency-control removal

**Files:**
- Modify: `src/GameMacro.App/MainWindow.xaml`
- Modify: `src/GameMacro.App/MainWindow.xaml.cs`
- Modify: `src/GameMacro.Core/Models/ProfileInputValidator.cs`
- Modify: `tests/GameMacro.Core.Tests/Models/MacroProfileTests.cs`

- [ ] Add three role checkboxes to the selected skill settings row.
- [ ] Remove icon status overlay.
- [ ] Remove emergency hotkey UI, registration, validation, and button behavior.
- [ ] Verify old JSON remains readable.

### Task 4: Documentation and verification

**Files:**
- Modify: `README.md`

- [ ] Document the burst/filler/insertion execution order.
- [ ] Run all tests and Release build.
- [ ] Publish to `artifacts/win-x64-auto` and perform a startup smoke test.
