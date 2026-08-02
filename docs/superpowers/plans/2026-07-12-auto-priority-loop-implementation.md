# Auto Priority Loop Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Automatically send the highest-priority ready skill while respecting fixed skill cooldowns, cast locks, and a one-second global cooldown.

**Architecture:** A deterministic `AutoRotationScheduler` selects the next rule using `HybridCooldownTracker` snapshots and one global lock timestamp. A cancellable loop checks foreground state, sends one key through `IInputSink`, and starts timers only after successful sending. WPF cards contain text only.

**Tech Stack:** C# 12, .NET 8 WPF, Win32 `SendInput`, xUnit 2.x

## Global Constraints

- Global cooldown is exactly 1 second.
- Lock duration after a skill is `CastTimeSeconds + 1 second`.
- Send only while the configured game is foreground.
- Start timers only after successful `SendInput`.
- F8 toggles; F12 stops immediately.
- No OCR, image capture, memory reading, driver input or anti-cheat bypass.
- Do not commit Git changes.

### Task 1: Deterministic Priority Scheduler

- [ ] Test priority selection, skill cooldown skipping, and one-second/cast lock behavior.
- [ ] Verify missing scheduler failure.
- [ ] Implement `TrySelect`, `RecordSuccessfulCast`, and `IsGloballyLocked`.
- [ ] Re-run scheduler tests.

### Task 2: Reliable SendInput Layout

- [ ] Test x64 native `INPUT` size is 40 bytes and supported key parsing.
- [ ] Verify current layout failure.
- [ ] Correct the explicit union size and expose a layout-size diagnostic.
- [ ] Make send failures return an error without starting scheduler timers.

### Task 3: Automatic Runtime and Text-Only Cards

- [ ] Create a cancellable 20ms loop using foreground gate, scheduler, tracker and input sink.
- [ ] Replace physical-hook-only F8 behavior with automatic loop start/stop.
- [ ] Remove card image and icon-path presentation; show name, large key and timer state.
- [ ] Display global CD/runtime errors in status bar.

### Task 4: Verify and Publish

- [ ] Run Release tests and build with zero failures/errors/warnings.
- [ ] Publish to `artifacts/win-x64-auto` as a self-contained folder.
- [ ] Verify no OCR native files exist.
- [ ] Launch for four seconds, verify responsive, then close normally.
