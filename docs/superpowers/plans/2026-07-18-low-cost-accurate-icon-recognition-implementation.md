# Low-Cost Accurate Icon Recognition Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the grayscale-only matcher with cached composite visual features while preserving existing saved profiles.

**Architecture:** `IconTemplateNormalizer` produces a versioned composite signature containing luminance, chroma, and edge grids. Every captured non-empty frame is classified against the small closed set of mappings so transition frames cannot poison a cache. Existing mapping previews are upgraded in memory from their PNG data.

**Tech Stack:** .NET 8, WPF bitmap decoding, xUnit, no new packages.

## Global Constraints

- Keep the 20ms capture loop and immediate first key-down.
- Do not use OCR, OpenCV, AI, or anti-cheat bypass techniques.
- Do not require users to recapture existing saved mappings.
- Do not commit changes.

---

### Task 1: Composite visual signature

**Files:**
- Modify: `src/GameMacro.App/Detection/IconTemplateNormalizer.cs`
- Create: `src/GameMacro.App/Detection/IconVisualSignature.cs`
- Test: `tests/GameMacro.App.Tests/Detection/IconVisualSignatureTests.cs`

- [ ] Write failing tests proving color and edge changes affect the signature while border and bottom labels remain ignored.
- [ ] Run the focused tests and confirm failure because composite signatures are not implemented.
- [ ] Implement fixed-grid luminance, chroma, and edge extraction plus weighted distance.
- [ ] Run the focused tests and confirm they pass.

### Task 2: Change-gated matcher

**Files:**
- Create: `src/GameMacro.App/Detection/DynamicIconRecognizer.cs`
- Test: `tests/GameMacro.App.Tests/Detection/DynamicIconRecognizerTests.cs`

- [ ] Write failing tests proving unchanged frames reuse the previous match and changed frames invoke matching immediately.
- [ ] Run the focused tests and confirm failure because the recognizer does not exist.
- [ ] Implement stateful change detection and match reuse.
- [ ] Run the focused tests and confirm they pass.

### Task 3: Profile migration and runtime integration

**Files:**
- Modify: `src/GameMacro.App/MainWindow.xaml.cs`
- Modify: `src/GameMacro.App/Detection/WindowsSkillCaptureService.cs`
- Test: `tests/GameMacro.App.Tests/Detection/MappingSignatureUpgradeTests.cs`

- [ ] Write a failing test that upgrades a legacy mapping from its saved PNG preview.
- [ ] Implement PNG-backed signature upgrade and invoke it when a profile is selected.
- [ ] Feed captured normalized signatures into `DynamicIconRecognizer` and reset its cache on start, stop, profile change, and mapping save.
- [ ] Run focused recognition tests.

### Task 4: Build and publish

**Files:**
- Output: `artifacts/win-x64-vision2/`

- [ ] Run the relevant App test project.
- [ ] Publish Release win-x64 self-contained output to the new directory.
