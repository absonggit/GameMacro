# Dynamic Icon Key Mapping Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the rotation/CD macro with one shared capture region that continuously maps the visible icon to a configured key.

**Architecture:** `MacroProfile` owns one normalized detection region and a list of lightweight icon mappings. A pure matcher selects the closest enabled template from one captured signature, while the WPF timer sends exactly that mapping's key every scan tick and switches immediately when the best match changes.

**Tech Stack:** .NET 8, WPF, xUnit, Windows BitBlt capture, Windows SendInput, System.Text.Json.

## Global Constraints

- Do not calculate skill CD, global CD, cast time, network margin, priority, or rotation groups.
- Do not use OCR, game memory, process injection, or anti-cheat bypasses.
- Scan interval defaults to 20ms and validates within 10–500ms.
- Each completed scan sends at most one key; a persistent match sends again on every scan.
- Do not delete legacy profile files and do not commit Git changes.

---

### Task 1: Versioned shared-region mapping model

**Files:**
- Create: `src/GameMacro.Core/Models/IconKeyMapping.cs`
- Modify: `src/GameMacro.Core/Models/MacroProfile.cs`
- Modify: `src/GameMacro.Core/Models/ProfileInputValidator.cs`
- Test: `tests/GameMacro.Core.Tests/Models/DynamicIconProfileTests.cs`

**Interfaces:**
- Produces: `IconKeyMapping`, `MacroProfile.DetectionX/Y/Width/Height`, `MacroProfile.ScanIntervalMs`, `MacroProfile.IconMappings`, `MacroProfile.HasDetectionRegion`.
- Produces: `ProfileInputValidator.Validate(MacroProfile)` errors for invalid interval, missing templates, and hotkey conflicts.

- [ ] **Step 1: Write failing serialization and validation tests**

```csharp
[Fact]
public void Dynamic_mapping_configuration_round_trips()
{
    var profile = new MacroProfile { Version = 2, DetectionX = .6, DetectionY = .5,
        DetectionWidth = .05, DetectionHeight = .05, ScanIntervalMs = 20,
        IconMappings = [new() { ActionKey = "F1", PreviewPng = "png", Signature = [1, 2], MatchThreshold = .2 }] };
    var restored = JsonSerializer.Deserialize<MacroProfile>(JsonSerializer.Serialize(profile))!;
    Assert.Equal("F1", restored.IconMappings.Single().ActionKey);
    Assert.True(restored.HasDetectionRegion);
}

[Fact]
public void Toggle_hotkey_cannot_equal_enabled_mapping_key()
{
    var profile = ValidProfile();
    profile.ToggleHotkey = profile.IconMappings[0].ActionKey;
    Assert.Contains(ProfileInputValidator.Validate(profile), error => error.Contains("冲突"));
}
```

- [ ] **Step 2: Run model tests and verify RED**

Run: `dotnet test tests/GameMacro.Core.Tests/GameMacro.Core.Tests.csproj --filter DynamicIconProfileTests`
Expected: compile failure because the dynamic mapping properties and type do not exist.

- [ ] **Step 3: Add the minimal version-2 model and validation**

```csharp
public sealed class IconKeyMapping
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool Enabled { get; set; } = true;
    public string ActionKey { get; set; } = "F1";
    public double[] Signature { get; set; } = [];
    public string PreviewPng { get; set; } = string.Empty;
    public double MatchThreshold { get; set; }
    [JsonIgnore] public bool IsCalibrated => Signature.Length > 0 && MatchThreshold > 0 && !string.IsNullOrWhiteSpace(PreviewPng);
}
```

Set new profiles to `Version = 2`; retain legacy fields only for JSON compatibility, but runtime and UI must use `IconMappings`.

- [ ] **Step 4: Run core model tests and verify GREEN**

Run: `dotnet test tests/GameMacro.Core.Tests/GameMacro.Core.Tests.csproj`
Expected: all core tests pass.

### Task 2: Best-template matcher

**Files:**
- Create: `src/GameMacro.App/Detection/IconKeyMappingMatcher.cs`
- Test: `tests/GameMacro.App.Tests/Detection/IconKeyMappingMatcherTests.cs`

**Interfaces:**
- Consumes: `IEnumerable<IconKeyMapping>` and a captured `double[]` signature.
- Produces: `IconMatchResult? Match(double[] sample, IEnumerable<IconKeyMapping> mappings)` where result contains mapping and distance.

- [ ] **Step 1: Write failing matcher tests**

```csharp
[Fact]
public void Selects_closest_enabled_mapping_below_threshold()
{
    var farther = new IconKeyMapping { ActionKey = "F1", Signature = [0d, 0d], PreviewPng = "a", MatchThreshold = .5 };
    var closest = new IconKeyMapping { ActionKey = "F2", Signature = [.2, .2], PreviewPng = "b", MatchThreshold = .5 };
    Assert.Same(closest, IconKeyMappingMatcher.Match([.18, .18], [farther, closest])!.Mapping);
}

[Fact]
public void Returns_null_when_no_mapping_reaches_threshold()
{
    var mapping = new IconKeyMapping { Signature = [0d, 0d], PreviewPng = "a", MatchThreshold = .1 };
    Assert.Null(IconKeyMappingMatcher.Match([1d, 1d], [mapping]));
}

[Fact]
public void Matcher_has_no_persistent_icon_or_change_gate()
{
    var a = new IconKeyMapping { ActionKey = "F1", Signature = [0d], PreviewPng = "a", MatchThreshold = .1 };
    var b = new IconKeyMapping { ActionKey = "F2", Signature = [1d], PreviewPng = "b", MatchThreshold = .1 };
    Assert.Equal(["F1", "F1", "F2"], new[] { new[] { 0d }, new[] { 0d }, new[] { 1d } }
        .Select(sample => IconKeyMappingMatcher.Match(sample, [a, b])!.Mapping.ActionKey));
}
```

- [ ] **Step 2: Run matcher tests and verify RED**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter IconKeyMappingMatcherTests`
Expected: compile failure because `IconKeyMappingMatcher` does not exist.

- [ ] **Step 3: Implement stateless closest-match selection**

```csharp
public static IconMatchResult? Match(double[] sample, IEnumerable<IconKeyMapping> mappings)
    => mappings.Where(x => x.Enabled && x.IsCalibrated)
        .Select(x => new IconMatchResult(x, IconStateClassifier.Distance(sample, x.Signature)))
        .Where(x => x.Distance <= x.Mapping.MatchThreshold)
        .OrderBy(x => x.Distance).ThenBy(x => x.Mapping.Id).FirstOrDefault();
```

Expose or extract the existing signature distance calculation rather than duplicating its math.

- [ ] **Step 4: Run detection tests and verify GREEN**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter "FullyQualifiedName~Detection"`
Expected: all detection tests pass.

### Task 3: Shared-region capture adapter

**Files:**
- Modify: `src/GameMacro.App/Detection/WindowsSkillCaptureService.cs`
- Test: `tests/GameMacro.App.Tests/Detection/NormalizedRegionTests.cs`

**Interfaces:**
- Produces: `CaptureRegion(MacroProfile)` returning `CapturedSkillImage`.
- Produces: `CaptureRegionSignature(MacroProfile)` returning `double[]`.
- Reuses: `ReadyBaselineCalibration.Create(IReadOnlyList<double[]>)` for a five-frame mapping template and threshold.

- [ ] **Step 1: Add failing shared-profile region conversion test**

```csharp
[Fact]
public void Profile_shared_region_converts_to_client_pixels()
{
    var profile = new MacroProfile { DetectionX = .5, DetectionY = .25, DetectionWidth = .1, DetectionHeight = .2 };
    Assert.Equal(new PixelRegion(500, 200, 100, 160), SharedDetectionRegion.ToPixels(profile, 1000, 800));
}
```

- [ ] **Step 2: Run the focused test and verify RED**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter NormalizedRegionTests`
Expected: compile failure because `SharedDetectionRegion` does not exist.

- [ ] **Step 3: Add shared-region conversion and capture overloads**

```csharp
public static PixelRegion ToPixels(MacroProfile profile, int width, int height)
    => NormalizedRegion.ToPixels(profile.DetectionX, profile.DetectionY,
        profile.DetectionWidth, profile.DetectionHeight, width, height);
```

Add `CaptureRegion(MacroProfile profile)` returning `CapturedSkillImage` and `CaptureRegionSignature(MacroProfile profile)` returning `double[]`. Both methods must resolve the target client origin and size, convert the shared normalized region through `SharedDetectionRegion.ToPixels`, then call the existing `CaptureScreen`. The first method encodes the PNG through `PngPreviewCodec.EncodeBase64`; both generate the signature through `IconStateClassifier.CreateSignature`. They must not capture the full window.

- [ ] **Step 4: Run all detection tests and verify GREEN**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter "FullyQualifiedName~Detection"`
Expected: all detection tests pass.

### Task 4: Replace WPF workflow and runtime loop

**Files:**
- Replace: `src/GameMacro.App/MainWindow.xaml`
- Replace: `src/GameMacro.App/MainWindow.xaml.cs`
- Modify: `src/GameMacro.App/Services/JsonProfileStore.cs`
- Test: `tests/GameMacro.App.Tests/Services/JsonProfileStoreTests.cs`

**Interfaces:**
- Consumes: shared-region capture, `IconKeyMappingMatcher.Match`, `SendInputService.EnqueueAsync` and existing global hotkey APIs.
- Produces: profile editing, region selection, mapping capture/edit/delete, start/stop, and status display.

- [ ] **Step 1: Add failing version-2 profile-store tests**

Verify that saving/loading retains the shared region, interval, preview PNG and mapping signature, while loading a legacy profile leaves the legacy file intact and returns a non-runnable version-2 editing profile.

- [ ] **Step 2: Run store tests and verify RED**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter JsonProfileStoreTests`
Expected: new migration behavior test fails.

- [ ] **Step 3: Implement the simplified editor UI**

Replace the old seven-row skill/group editor with these named controls: `ProfilesList`, `ProfileNameBox`, `WindowCombo`, `RegionPreview`, `RegionStatusText`, `MappingsList`, `ActionKeyCombo`, `MappingStatusText`, `ToggleHotkeyCombo`, `ScanIntervalBox`, `ToggleButton`, and `StatusText`. Bind mapping cards to `PreviewPng`, `ActionKey`, and `Enabled`; give each card a delete `×`. Include buttons wired to `SelectRegion_Click`, `AddMapping_Click`, `RecaptureMapping_Click`, `ApplyMapping_Click`, `SaveProfile_Click`, and `Toggle_Click`. Remove every axis list, drag/drop handler, cooldown field and repeat field.

- [ ] **Step 4: Implement immediate repeated dispatch**

```csharp
private async Task AutomationTickAsync()
{
    if (_tickInProgress || _profile is null) return;
    _tickInProgress = true;
    try {
        if (!await _windows.IsTargetForegroundAsync(_profile, CancellationToken.None)) return;
        var sample = _capture.CaptureRegionSignature(_profile);
        var match = IconKeyMappingMatcher.Match(sample, _profile.IconMappings);
        if (match is not null) await _input.EnqueueAsync(match.Mapping.ActionKey, CancellationToken.None);
        UpdateRuntimeStatus(match);
    } finally { _tickInProgress = false; }
}
```

Set `_automationTimer.Interval` from `ScanIntervalMs` at start. Do not include stable-frame confirmation, icon-change gating or scheduler calls.

- [ ] **Step 5: Run app tests and build**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj`
Expected: all app tests pass.

Run: `dotnet build GameMacro.sln -c Release --no-restore`
Expected: build succeeds with zero errors.

### Task 5: Documentation, regression verification and publish

**Files:**
- Replace: `README.md`
- Modify only if required by publish output: `scripts/publish.ps1`

**Interfaces:**
- Produces: end-user setup instructions and `artifacts/win-x64-auto/GameMacro.App.exe`.

- [ ] **Step 1: Rewrite README for the new workflow**

Replace the usage section with the exact order: choose target window → frame the dynamic slot once → choose a key and capture the currently visible icon → repeat for every possible icon → set 10–500ms scan interval and non-conflicting toggle hotkey → save → return game to foreground → start. State that a recognized icon sends its key every scan and switches on the first matching scan after the icon changes. State that unknown icons send nothing and that the program uses only screenshots plus ordinary `SendInput`.

- [ ] **Step 2: Run complete verification**

Run: `dotnet test GameMacro.sln -c Release --no-restore`
Expected: every test passes with zero failures.

- [ ] **Step 3: Publish win-x64**

Run: `powershell -ExecutionPolicy Bypass -File scripts/publish.ps1`
Expected: `artifacts/win-x64-auto/GameMacro.App.exe` exists with a fresh timestamp.

- [ ] **Step 4: Perform GUI smoke test**

Launch the published executable, verify it remains running and responsive, then close it. Confirm the simplified dynamic-region mapping UI loads without an unhandled exception.

- [ ] **Step 5: Confirm no Git commit was created**

Run: `git status --short`
Expected: local modified/untracked files are visible and no commit operation was performed.
