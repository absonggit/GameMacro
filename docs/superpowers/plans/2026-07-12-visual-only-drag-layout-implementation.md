# Visual-only Scheduling and Drag Layout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove all runtime timing locks and make the 16-slot skill queue unobstructed and visibly draggable.

**Architecture:** Keep PNG classification and three-frame debounce unchanged. Make the visual scheduler independent of global timing, then revise the WPF list container so actions no longer consume list width and drag feedback is applied to item containers.

**Tech Stack:** .NET 8, WPF, xUnit

## Global Constraints

- Do not add OCR, game-memory access, or bypass behavior.
- Do not commit changes.
- Maximum 16 skills in two rows of eight.

---

### Task 1: Remove visual scheduling lock

**Files:**
- Modify: `tests/GameMacro.App.Tests/Timing/AutoRotationSchedulerTests.cs`
- Modify: `src/GameMacro.App/Timing/AutoRotationScheduler.cs`
- Modify: `src/GameMacro.App/MainWindow.xaml.cs`

- [ ] Replace the half-second test with a test that records a send and immediately selects a ready visual skill.
- [ ] Run the focused test and confirm it fails.
- [ ] Remove the lock check from `TrySelectVisual` and make `RecordKeySent` non-blocking.
- [ ] Remove public-CD status text and run the focused test again.

### Task 2: Fix list layout and card deletion

**Files:**
- Modify: `src/GameMacro.App/MainWindow.xaml`
- Modify: `src/GameMacro.App/MainWindow.xaml.cs`

- [ ] Move the add button above the list and remove the external delete button.
- [ ] Stretch the two-row list across the available content width.
- [ ] Add a card-level `×` button bound to the card rule and a confirmation handler.

### Task 3: Add drag feedback and verify

**Files:**
- Modify: `src/GameMacro.App/MainWindow.xaml`
- Modify: `src/GameMacro.App/MainWindow.xaml.cs`
- Modify: `README.md`

- [ ] Add drag-over/leave events and container visual-state helpers.
- [ ] Fade the source card, highlight the target card, and restore both after drop/cancel.
- [ ] Update README, run all tests, build, publish, and launch-smoke the executable.
