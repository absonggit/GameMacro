# Single-capture Calibration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace two-state manual calibration with one ready-state region selection that automatically captures an icon, baseline, and tolerance.

**Architecture:** A pure baseline calibrator averages multiple signatures and derives hysteresis thresholds. Runtime classification compares current samples only with that baseline. The region-selection workflow performs five captures before committing the calibration atomically.

**Tech Stack:** .NET 8, WPF, xUnit, existing Win32 capture service.

## Global Constraints

- One region selection is the only calibration action.
- No OCR, cooldown reference image, or external image file.
- Failed capture must preserve the previous calibration.
- Do not commit changes to Git.

---

### Task 1: Single-baseline Model and Classifier

**Files:**
- Modify: `src/GameMacro.Core/Models/MacroRule.cs`
- Create: `src/GameMacro.App/Detection/ReadyBaselineCalibration.cs`
- Modify: `src/GameMacro.App/Detection/IconStateClassifier.cs`
- Test: `tests/GameMacro.App.Tests/Detection/ReadyBaselineCalibrationTests.cs`
- Test: `tests/GameMacro.App.Tests/Detection/IconStateClassifierTests.cs`

- [ ] Add failing tests for mean baseline, jitter-derived thresholds, ready/change/unknown hysteresis, and new calibration completeness.
- [ ] Run focused tests and verify expected failures.
- [ ] Add `ReadyThreshold` and `ChangeThreshold`; implement baseline calibration and single-reference classification.
- [ ] Run focused tests and verify they pass.

### Task 2: Use Single-baseline Runtime Detection

**Files:**
- Modify: `src/GameMacro.App/MainWindow.xaml.cs`
- Test: `tests/GameMacro.App.Tests/Timing/ClosedLoopReleaseControllerTests.cs`

- [ ] Update controller tests to use ready versus changed states without a cooldown reference.
- [ ] Feed the single-reference classifier into the existing consecutive-frame controller.
- [ ] Ensure startup requires new-format calibration thresholds.

### Task 3: Atomic Five-frame Capture and Simplified UI

**Files:**
- Modify: `src/GameMacro.App/MainWindow.xaml`
- Modify: `src/GameMacro.App/MainWindow.xaml.cs`
- Modify: `README.md`

- [ ] Remove ready/cooldown record buttons, both preview panels, and the right-side explanation.
- [ ] Rename the remaining action to “框选并捕获技能”.
- [ ] After selection, capture five frames at 50ms intervals and only then replace region, baseline, thresholds, and ready PNG.
- [ ] Show only “未捕获/已捕获”; update documentation.

### Task 4: Verify and Publish

**Files:**
- Output: `artifacts/win-x64-auto`

- [ ] Run complete Release tests and build with zero failures/warnings/errors.
- [ ] Publish self-contained win-x64 output and verify no OCR files.
- [ ] Launch for four seconds, verify responsiveness, and close cleanly.
