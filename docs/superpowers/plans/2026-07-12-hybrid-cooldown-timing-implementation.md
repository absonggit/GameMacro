# Hybrid Cooldown Timing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Start local cooldown and cast timers from physical skill key presses, correct cooldowns with stable OCR values, and render the resulting state over each skill icon.

**Architecture:** A pure `HybridCooldownTracker` owns monotonic end timestamps and merges local key events with OCR observations. A read-only low-level Windows keyboard hook publishes physical key-down events only while the game is foreground. WPF binds tracker snapshots to existing rule cards; no input injection is used.

**Tech Stack:** C# 12, .NET 8 WPF, Win32 `WH_KEYBOARD_LL`, Tesseract 5.2, xUnit 2.x

## Global Constraints

- Base cooldown range: 0–600 seconds; cast time range: 0–30 seconds.
- Physical key-down starts cooldown immediately and cast lock concurrently.
- Stable OCR integer overrides the estimated remaining cooldown.
- Temporary OCR failure does not erase an active local countdown.
- Keyboard hook is read-only and always calls the next hook.
- Current phase never calls `SendInput`.
- Do not commit Git changes.

---

### Task 1: Hybrid Cooldown State Machine

**Files:**
- Modify: `src/GameMacro.Core/Models/MacroRule.cs`
- Create: `src/GameMacro.App/Timing/HybridCooldownTracker.cs`
- Test: `tests/GameMacro.App.Tests/Timing/HybridCooldownTrackerTests.cs`

**Interfaces:**
- Adds `BaseCooldownSeconds`, `CastTimeSeconds`, and `ListenForPhysicalKey` to `MacroRule`.
- Produces `OnPhysicalKey(rule, now)`, `OnOcr(rule, seconds, now)`, and `GetSnapshot(rule, now)`.

- [ ] Test that F1 with base CD 10 and cast 1.5 reports remaining 10 and casting immediately, remaining 8 after two seconds, and not casting after two seconds.
- [ ] Test that OCR value 7 replaces an estimated 8 seconds and that a null OCR observation preserves an active estimate.
- [ ] Run filtered tests and verify missing tracker failure.
- [ ] Implement end timestamps using `DateTimeOffset`, clamped validation, and snapshot states `Ready`, `Cooldown`, `Casting`.
- [ ] Re-run filtered tests and expect PASS.

### Task 2: Read-Only Physical Keyboard Hook

**Files:**
- Modify: `src/GameMacro.App/Platform/NativeMethods.cs`
- Create: `src/GameMacro.App/Platform/PhysicalKeyboardHook.cs`
- Create: `src/GameMacro.App/Platform/VirtualKeyParser.cs`
- Test: `tests/GameMacro.App.Tests/Platform/VirtualKeyParserTests.cs`

**Interfaces:**
- Produces `ushort? VirtualKeyParser.Parse(string)` and `PhysicalKeyboardHook.KeyDown`.

- [ ] Test F1, F12, letters, digits, and unsupported strings.
- [ ] Verify missing parser failure, then implement parsing.
- [ ] Add `SetWindowsHookEx(WH_KEYBOARD_LL)`, `CallNextHookEx`, and `UnhookWindowsHookEx`; publish non-injected `WM_KEYDOWN/WM_SYSKEYDOWN` only.
- [ ] Ensure callback always returns `CallNextHookEx` and dispose removes the hook.
- [ ] Run all platform tests.

### Task 3: Merge OCR, Key Events, and Card UI

**Files:**
- Modify: `src/GameMacro.App/MainWindow.xaml`
- Modify: `src/GameMacro.App/MainWindow.xaml.cs`
- Modify: `src/GameMacro.App/Ocr/CooldownMonitor.cs`
- Modify: `README.md`

**Interfaces:**
- Card overlay displays tracker snapshot; OCR update calls tracker correction; physical key event calls tracker start.

- [ ] Add editable base CD and cast-time fields to the selected rule panel and persist them through JSON.
- [ ] Overlay only the number, `就绪`, or `施法中` at the icon center; keep F-key below the icon.
- [ ] While monitoring, install the read-only hook; match virtual keys to enabled rules only when the selected game is foreground.
- [ ] Feed stable OCR cooldown readings into the tracker; ignore recognizing and failed OCR states.
- [ ] Refresh cards every 100ms from tracker snapshots and stop timer/hook with monitoring.

### Task 4: Verification and Folder Publish

**Files:**
- Modify: `scripts/publish.ps1`
- Output: `artifacts/win-x64-ocr/`

**Interfaces:**
- Produces a self-contained folder containing EXE, Tesseract native DLLs, and `tessdata`.

- [ ] Run `dotnet test GameMacro.sln -c Release` and require zero failures.
- [ ] Publish with `PublishSingleFile=false`.
- [ ] Verify EXE, `x64/leptonica-1.82.0.dll`, `x64/tesseract50.dll`, and `tessdata/eng.traineddata` exist.
- [ ] Launch the published EXE for four seconds, confirm it remains responsive, then close it normally.
