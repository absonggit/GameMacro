# Full Pixel Icon Template Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace coarse icon signatures with versioned full-pixel templates that accurately distinguish visually similar skills.

**Architecture:** Normalize source and dynamic captures into the same 32×32 center-artwork representation. Compare RGB, luminance, and edges over small translations, then reject unknown or ambiguous results before input is sent.

**Tech Stack:** .NET 8, WPF, xUnit, built-in WPF PNG decoding; no new dependencies.

## Global Constraints

- Keep the existing region selection, automatic source segmentation, card mapping, profile storage, and 10–500ms scan interval.
- Do not use OCR, game memory, process injection, OpenCV, or anti-cheat bypasses.
- Do not create a Git commit.

---

### Task 1: Pixel template representation and normalization

**Files:**
- Create: `src/GameMacro.App/Detection/PixelIconTemplate.cs`
- Create: `src/GameMacro.App/Detection/PixelIconTemplateBuilder.cs`
- Test: `tests/GameMacro.App.Tests/Detection/PixelIconTemplateBuilderTests.cs`

**Interfaces:**
- Produces `PixelIconTemplateBuilder.Create(byte[] bgra, int width, int height)`.
- Produces a versioned fixed-size template containing normalized RGB, luminance, and edge samples.

- [ ] Write tests proving equivalent artwork with border/text differences normalizes closely and different center artwork remains separated.
- [ ] Run the focused test and confirm it fails because the new API does not exist.
- [ ] Implement fixed center cropping, resize, luminance normalization, and gradient extraction.
- [ ] Run the focused tests and confirm they pass.

### Task 2: Robust matcher and rejection

**Files:**
- Create: `src/GameMacro.App/Detection/PixelIconTemplateMatcher.cs`
- Test: `tests/GameMacro.App.Tests/Detection/PixelIconTemplateMatcherTests.cs`

**Interfaces:**
- Consumes two `PixelIconTemplate` values.
- Produces shift-tolerant distance and ranked mapping selection.

- [ ] Write failing tests for F2-vs-1 same-color shapes, shifted known artwork, unknown rejection, and ambiguous rejection.
- [ ] Run focused tests and confirm the expected failures.
- [ ] Implement ±2 pixel translation scoring with absolute and runner-up gates.
- [ ] Run focused tests and confirm they pass.

### Task 3: Profile persistence and automatic migration

**Files:**
- Modify: `src/GameMacro.Core/Models/IconKeyMapping.cs`
- Modify: `src/GameMacro.App/Detection/BatchMappingBuilder.cs`
- Modify: `src/GameMacro.App/Detection/MappingSignatureUpgrade.cs`
- Test: `tests/GameMacro.App.Tests/Services/JsonProfileStoreTests.cs`
- Test: `tests/GameMacro.App.Tests/Detection/MappingSignatureUpgradeTests.cs`

**Interfaces:**
- Adds a serialized pixel-template payload to each `IconKeyMapping`.
- Rebuilds missing/outdated payloads from `PreviewPng`.

- [ ] Write failing persistence and migration tests.
- [ ] Run them and verify failure.
- [ ] Add the model field, cloning, serialization, and PNG migration.
- [ ] Run the tests and verify pass.

### Task 4: Runtime capture and recognition integration

**Files:**
- Modify: `src/GameMacro.App/Detection/WindowsSkillCaptureService.cs`
- Modify: `src/GameMacro.App/Detection/DynamicIconRecognizer.cs`
- Modify: `src/GameMacro.App/MainWindow.xaml.cs`
- Test: `tests/GameMacro.App.Tests/Detection/DynamicIconRecognizerTests.cs`

**Interfaces:**
- Runtime capture returns a pixel template for the selected dynamic region.
- Recognizer returns an `IconMatchResult` only for a reliable known template.

- [ ] Write a failing recognizer test covering a rapid F2-to-1 transition and unknown frame.
- [ ] Run it and verify failure.
- [ ] Connect per-frame pixel capture and ranked matching to the existing input loop.
- [ ] Run the focused test and verify pass.

### Task 5: Documentation, regression suite, and release

**Files:**
- Modify: `README.md`
- Modify: existing matcher tests to cover compatibility behavior.

- [ ] Update usage guidance to recommend scanning the battle-assist skill group.
- [ ] Run `dotnet test GameMacro.sln --no-restore`; require zero failures.
- [ ] Publish a fresh self-contained build to `artifacts/win-x64-pixel-template`.
- [ ] Confirm `GameMacro.App.exe` exists in the release directory.

