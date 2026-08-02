# Batch Skill Icon Segmentation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let users frame an arbitrary skill-bar area once, automatically extract non-empty skill icons, assign keys on cards, and save the resulting mappings.

**Architecture:** A pure BGRA segmenter detects textured square components against the selected-region background and returns ordered icon crops. A shared normalizer removes borders and bottom key labels before feature extraction, so batch-source icons can match the existing dynamic-slot capture despite different labels and sizes. The WPF editor keeps scan results in a pending collection and only replaces saved runtime mappings when the user explicitly saves all mappings.

**Tech Stack:** .NET 8, WPF, xUnit, existing BitBlt capture, existing PNG/signature utilities; no new packages.

## Global Constraints

- Dynamic monitoring region and continuous immediate key dispatch remain unchanged.
- Batch segmentation cannot assume a fixed row count, column count, icon count, icon size, or gap.
- Empty slots, duplicate icons, tiny noise, and non-square components are filtered.
- No OCR, OpenCV, model file, game memory, injection, or network service.
- Existing profile files are not deleted and no Git commit is created.

---

### Task 1: Icon normalization shared by templates and runtime samples

**Files:**
- Create: `src/GameMacro.App/Detection/IconTemplateNormalizer.cs`
- Modify: `src/GameMacro.App/Detection/WindowsSkillCaptureService.cs`
- Test: `tests/GameMacro.App.Tests/Detection/IconTemplateNormalizerTests.cs`

**Interfaces:**
- Produces: `NormalizedIcon Normalize(byte[] bgra, int width, int height)`.
- Produces: `double[] CreateSignature(byte[] bgra, int width, int height)`.
- Runtime `CaptureRegionSignature` uses the same normalizer as batch templates.

- [ ] Write a failing test with two icons that share the same central artwork but have different borders and bottom key labels; normalized signatures must be equal within `0.01` distance.
- [ ] Run `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter IconTemplateNormalizerTests --no-restore` and verify it fails because the normalizer is missing.
- [ ] Implement proportional cropping: remove 10% from left/right, 8% from top, and 28% from bottom, then create a signature from the retained BGRA pixels.
- [ ] Run all detection tests and verify they pass.

### Task 2: Arbitrary-layout icon segmentation

**Files:**
- Create: `src/GameMacro.App/Detection/SkillIconSegmenter.cs`
- Test: `tests/GameMacro.App.Tests/Detection/SkillIconSegmenterTests.cs`

**Interfaces:**
- Produces: `SkillIconSegmentationResult Segment(byte[] bgra, int width, int height)`.
- Result contains ordered `DetectedSkillIcon` values with `PixelRegion Region`, full crop pixels, width, height, preview PNG, normalized signature, and match threshold.

- [ ] Write failing synthetic-image tests for differently sized and spaced icons, multi-row ordering, an empty bordered slot, tiny noise, and duplicate artwork.
- [ ] Run the focused tests and verify RED because `SkillIconSegmenter` is missing.
- [ ] Implement background estimation from perimeter pixels, foreground-distance mask, small-radius mask dilation, connected-component extraction, square/size filtering, inner texture/edge filtering, overlap merging, normalized-signature duplicate removal, and stable Y/X ordering.
- [ ] Run all segmentation and detection tests and verify GREEN.

### Task 3: Profile source-region fields and rectangular capture

**Files:**
- Modify: `src/GameMacro.Core/Models/MacroProfile.cs`
- Modify: `src/GameMacro.App/Detection/WindowsSkillCaptureService.cs`
- Test: `tests/GameMacro.Core.Tests/Models/DynamicIconProfileTests.cs`
- Test: `tests/GameMacro.App.Tests/Detection/NormalizedRegionTests.cs`

**Interfaces:**
- Produces profile fields `SourceX`, `SourceY`, `SourceWidth`, `SourceHeight`, `SourcePreviewPng`, and `HasSourceRegion`.
- Produces `CapturedRegion CaptureRegion(MacroProfile profile, NormalizedRegion region)` containing BGRA pixels, dimensions, signature, and PNG.

- [ ] Add failing JSON round-trip and normalized-coordinate tests for the source region.
- [ ] Run the focused tests and verify RED.
- [ ] Add the version-compatible profile fields and a single rectangular capture path reused by dynamic, batch, and manual-supplement capture.
- [ ] Run core and detection tests and verify GREEN.

### Task 4: Pending mapping editor and batch scan UI

**Files:**
- Create: `src/GameMacro.App/ViewModels/PendingIconMapping.cs`
- Modify: `src/GameMacro.App/MainWindow.xaml`
- Modify: `src/GameMacro.App/MainWindow.xaml.cs`
- Test: `tests/GameMacro.App.Tests/Detection/BatchMappingBuilderTests.cs`
- Create: `src/GameMacro.App/Detection/BatchMappingBuilder.cs`

**Interfaces:**
- Produces `Build(IEnumerable<DetectedSkillIcon>)` pending cards and `Save(IEnumerable<PendingIconMapping>)` validated `IconKeyMapping` values.
- WPF exposes `AvailableKeys`, `_pendingMappings`, scan, manual supplement, delete pending item, and save-all actions.

- [ ] Write failing tests proving scan results do not mutate saved mappings, unassigned cards block save, and save converts every pending card to a calibrated runtime mapping.
- [ ] Run focused tests and verify RED.
- [ ] Implement `PendingIconMapping` and pure builder/validation code.
- [ ] Replace the old one-at-a-time mapping panel with source preview, “框选并扫描技能”, “手动补充图标”, pending cards with key dropdown/delete, and “保存全部映射”.
- [ ] Make scan replace only `_pendingMappings`; make save-all atomically replace `profile.IconMappings`; load existing mappings into pending cards for editing.
- [ ] Keep the dynamic-area selection, timer interval, hotkey, matcher, foreground check, and per-scan SendInput behavior unchanged.
- [ ] Run all app tests and Release build.

### Task 5: Documentation, full verification, publish, and smoke test

**Files:**
- Modify: `README.md`

- [ ] Document separate dynamic and batch-source regions, automatic non-grid extraction, empty-slot filtering, per-card key binding, manual supplement, and explicit save-all behavior.
- [ ] Run `dotnet test GameMacro.sln -c Release --no-restore`; require zero failures.
- [ ] Run `powershell -ExecutionPolicy Bypass -File scripts/publish.ps1`; verify fresh files in `artifacts/win-x64-auto`.
- [ ] Launch the published executable for three seconds, verify it remains running and responsive with title “动态图标按键助手”, then close it.
- [ ] Confirm the existing profile file timestamp was not changed by the smoke test and no Git commit was created.

