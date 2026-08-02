# Cooldown OCR Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Capture each selected skill icon, recognize its 1–3 digit integer cooldown, and display the icon and stable cooldown state without sending input.

**Architecture:** A pure parser and two-frame stabilizer sit in `GameMacro.Core`-compatible application logic and receive raw OCR strings. A Tesseract adapter preprocesses the captured bitmap, runs digit-only OCR, and returns diagnostics; a cancellable WPF monitor updates card view models at 200ms intervals. Icon PNG files are stored beside profile resources and referenced by each rule.

**Tech Stack:** C# 12, .NET 8 WPF, System.Drawing.Common 8, Tesseract 5.x, xUnit 2.x

## Global Constraints

- Recognize only 1–3 digit positive integers; no decimals or text.
- Require two consecutive identical results before publishing a cooldown.
- Require two consecutive no-digit results before publishing `Ready`.
- OCR monitoring must never call `SendInput` or start `MacroEngine`.
- Save a PNG icon after every successful region selection and show it on the skill card.
- Keep all work local and do not create Git commits.

---

### Task 1: OCR Parsing and Two-Frame Stability

**Files:**
- Create: `src/GameMacro.App/Ocr/CooldownTextParser.cs`
- Create: `src/GameMacro.App/Ocr/CooldownStabilizer.cs`
- Test: `tests/GameMacro.App.Tests/Ocr/CooldownRecognitionTests.cs`

**Interfaces:**
- Produces: `int? CooldownTextParser.Parse(string rawText)` and `CooldownDisplayState CooldownStabilizer.Push(int? value)`.

- [ ] Write tests asserting `" 47\n" -> 47`, `"3.5" -> null`, four digits -> null, two `47` frames -> cooldown 47, and two null frames -> ready.
- [ ] Run `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter CooldownRecognitionTests` and verify missing-type failure.
- [ ] Implement regex `^\d{1,3}$`, require value greater than zero, and implement the two-frame state machine with states `Recognizing`, `Cooldown`, and `Ready`.
- [ ] Re-run the filtered test and expect PASS.

### Task 2: Tesseract Adapter and Preprocessing

**Files:**
- Modify: `src/GameMacro.App/GameMacro.App.csproj`
- Create: `src/GameMacro.App/Ocr/ICooldownOcr.cs`
- Create: `src/GameMacro.App/Ocr/TesseractCooldownOcr.cs`
- Create: `src/GameMacro.App/Ocr/OcrPreprocessor.cs`
- Create: `tests/GameMacro.App.Tests/Ocr/OcrPreprocessorTests.cs`
- Add: `src/GameMacro.App/tessdata/eng.traineddata`

**Interfaces:**
- Produces: `OcrResult Recognize(CaptureFrame frame)` containing raw text, parsed seconds, and preprocessed PNG bytes.

- [ ] Add a test using a generated 48x48 bitmap with a centered white `47` on a dark background; assert preprocessing returns a 4x image and non-empty PNG.
- [ ] Verify the test fails because `OcrPreprocessor` is missing.
- [ ] Add the Tesseract package, grayscale the central 80% of the icon, scale 4x with nearest-neighbor interpolation, apply a high-contrast threshold, and encode PNG.
- [ ] Configure Tesseract with `tessedit_char_whitelist=0123456789` and `PageSegMode.SingleWord`, then parse through `CooldownTextParser`.
- [ ] Copy `tessdata/**` to output and run all OCR tests.

### Task 3: Icon Persistence and Card Display

**Files:**
- Modify: `src/GameMacro.Core/Models/MacroRule.cs`
- Create: `src/GameMacro.App/Services/RuleIconStore.cs`
- Modify: `src/GameMacro.App/MainWindow.xaml`
- Modify: `src/GameMacro.App/MainWindow.xaml.cs`
- Test: `tests/GameMacro.App.Tests/Services/RuleIconStoreTests.cs`

**Interfaces:**
- Adds: `MacroRule.IconPath` and `RuleIconStore.SaveAsync(Guid profileId, Guid ruleId, CaptureFrame frame)`.

- [ ] Write a test that saves a 2x2 capture and asserts a readable PNG exists at `<root>/<profileId>/icons/<ruleId>.png`.
- [ ] Verify failure because `RuleIconStore` is missing.
- [ ] Implement atomic PNG save and assign the returned path after region selection.
- [ ] Add an `Image` to each skill card bound through a path-to-bitmap converter, with a neutral placeholder when missing.
- [ ] Run storage tests and build WPF in Release.

### Task 4: OCR Monitor, Diagnostics, and Publish

**Files:**
- Create: `src/GameMacro.App/Ocr/CooldownMonitor.cs`
- Modify: `src/GameMacro.App/MainWindow.xaml`
- Modify: `src/GameMacro.App/MainWindow.xaml.cs`
- Modify: `README.md`
- Modify: `scripts/publish.ps1`
- Test: `tests/GameMacro.App.Tests/Ocr/CooldownMonitorTests.cs`

**Interfaces:**
- Produces: monitor events `(ruleId, state, rawText, seconds, previewPng)` every stable update and a single-shot test method.

- [ ] Write a fake-OCR monitor test that emits `47,47,46,46,null,null` and assert published states are cooldown 47, cooldown 46, ready.
- [ ] Verify the test fails because `CooldownMonitor` is missing.
- [ ] Implement a cancellable 200ms loop that captures enabled rules sequentially and never invokes `MacroEngine` or `IInputSink`.
- [ ] Add `Start CD Monitor` / `Stop Monitor` controls, card labels for `47 秒`, `就绪`, `识别中`, and an OCR test panel showing raw text and preview.
- [ ] Run `dotnet test GameMacro.sln -c Release`, publish win-x64 single-file output, and verify `artifacts/win-x64/GameMacro.App.exe` exists.
