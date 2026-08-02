# Calibration Preview and Compact Cards Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Persist and display the exact ready/cooldown captures while replacing oversized skill cards with compact ready-icon cards.

**Architecture:** Each captured frame produces both the existing numeric signature and a PNG Base64 preview. The model persists previews; a WPF converter decodes them for binding. The card and calibration panels bind directly to those properties.

**Tech Stack:** .NET 8, WPF imaging, System.Text.Json, xUnit.

## Global Constraints

- Do not use OCR or external image files.
- Keep old profile JSON compatible.
- Do not commit changes to Git.
- The card always displays the ready preview.

---

### Task 1: Persist Preview Data

**Files:**
- Modify: `src/GameMacro.Core/Models/MacroRule.cs`
- Modify: `tests/GameMacro.Core.Tests/Models/MacroProfileTests.cs`

- [ ] Add a failing JSON round-trip test for `ReadyPreviewPng` and `CooldownPreviewPng`.
- [ ] Run the focused test and verify compilation fails because the properties are absent.
- [ ] Add both string properties and include them in the visual calibration state.
- [ ] Run the focused test and verify it passes.

### Task 2: Capture PNG and Signature From One Frame

**Files:**
- Create: `src/GameMacro.App/Detection/CapturedSkillImage.cs`
- Create: `src/GameMacro.App/Detection/PngPreviewCodec.cs`
- Modify: `src/GameMacro.App/Detection/WindowsSkillCaptureService.cs`
- Test: `tests/GameMacro.App.Tests/Detection/PngPreviewCodecTests.cs`

- [ ] Add a failing test that encodes a 2×2 BGRA buffer and decodes it as a valid frozen WPF image.
- [ ] Run the test and verify the codec is missing.
- [ ] Implement PNG encoding/decoding and return `CapturedSkillImage(Signature, PreviewPng)` from one capture.
- [ ] Run codec and detection tests.

### Task 3: Bind Previews and Compact Cards

**Files:**
- Create: `src/GameMacro.App/Converters/Base64PngImageConverter.cs`
- Modify: `src/GameMacro.App/MainWindow.xaml`
- Modify: `src/GameMacro.App/MainWindow.xaml.cs`
- Modify: `README.md`

- [ ] Add the converter resource and two labeled 80×80 calibration previews with visible placeholders.
- [ ] Change cards to 92×128, show a 56×56 ready image, overlay cooldown, and retain key/enable controls.
- [ ] Store both signature and preview on record; clear both when the region changes.
- [ ] Build and inspect XAML compilation; update usage documentation.

### Task 4: Verify and Publish

**Files:**
- Output: `artifacts/win-x64-auto`

- [ ] Run all Release tests; expect zero failures.
- [ ] Run Release build; expect zero warnings and errors.
- [ ] Publish self-contained win-x64 output and verify no OCR files exist.
- [ ] Launch for four seconds, verify responsiveness, and close cleanly.
