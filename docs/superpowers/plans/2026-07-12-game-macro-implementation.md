# Windows Game Macro Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Windows WPF application that edits macro profiles and safely triggers fixed-interval or screen-condition keyboard actions only while the selected game is foreground.

**Architecture:** A WPF MVVM shell owns profile editing and status display. A platform-independent execution core evaluates rules through injected window, capture, detection, clock, and input interfaces; Windows adapters implement those interfaces with Win32 and OpenCvSharp. JSON profile persistence and a single serialized input queue keep configuration and execution deterministic.

**Tech Stack:** C# 12, .NET 8 WPF, CommunityToolkit.Mvvm 8.x, OpenCvSharp4 4.x, System.Text.Json, xUnit 2.x

## Global Constraints

- Target Windows x64 and publish a self-contained single-file `.exe` together with complete source.
- Use only normal Windows APIs: screen capture, `RegisterHotKey`, foreground-window checks, and `SendInput`.
- Do not implement driver input, process-memory access, background-window input, or anti-cheat bypass.
- Never send input unless the configured target window exists, is visible, is not minimized, and is foreground.
- On stop, exit, or error, release every key or mouse button held by the application.
- Do not create Git commits; the user requested local-only development.

---

## Planned File Map

- `src/GameMacro.App/`: WPF application, views, view models, and Windows adapters.
- `src/GameMacro.Core/`: profile model, condition evaluation, scheduling, and service interfaces.
- `tests/GameMacro.Core.Tests/`: deterministic unit tests using fake platform services.
- `tests/GameMacro.App.Tests/`: persistence and adapter-level tests that do not inject real input.
- `scripts/publish.ps1`: reproducible win-x64 release command.
- `README.md`: build, run, calibration, safety boundary, and release instructions.

### Task 1: Solution Skeleton and Profile Model

**Files:**
- Create: `GameMacro.sln`
- Create: `src/GameMacro.Core/GameMacro.Core.csproj`
- Create: `src/GameMacro.Core/Models/MacroProfile.cs`
- Create: `src/GameMacro.Core/Models/MacroRule.cs`
- Create: `src/GameMacro.Core/Models/RuleCondition.cs`
- Create: `tests/GameMacro.Core.Tests/GameMacro.Core.Tests.csproj`
- Create: `tests/GameMacro.Core.Tests/Models/MacroProfileTests.cs`

**Interfaces:**
- Produces: `MacroProfile`, `MacroRule`, `RuleCondition`, `RuleMode`, `ConditionMode`, `DetectionKind`, and `ScreenRegion`.

- [ ] **Step 1: Create the solution and projects**

Run:
```powershell
dotnet new sln -n GameMacro
dotnet new classlib -n GameMacro.Core -o src/GameMacro.Core -f net8.0
dotnet new xunit -n GameMacro.Core.Tests -o tests/GameMacro.Core.Tests -f net8.0
dotnet sln add src/GameMacro.Core/GameMacro.Core.csproj tests/GameMacro.Core.Tests/GameMacro.Core.Tests.csproj
dotnet add tests/GameMacro.Core.Tests/GameMacro.Core.Tests.csproj reference src/GameMacro.Core/GameMacro.Core.csproj
```
Expected: solution contains one library and one test project.

- [ ] **Step 2: Write failing model validation tests**

```csharp
[Fact]
public void Validate_rejects_non_positive_interval()
{
    var rule = new MacroRule { IntervalMs = 0 };
    Assert.Contains(rule.Validate(), x => x.Contains("间隔"));
}

[Fact]
public void OrderedConditionalRules_returns_enabled_rules_by_priority()
{
    var profile = new MacroProfile { Rules =
    [
        new() { Name = "F2", Enabled = true, Mode = RuleMode.Conditional, Priority = 2 },
        new() { Name = "F1", Enabled = true, Mode = RuleMode.Conditional, Priority = 1 }
    ]};
    Assert.Equal(["F1", "F2"], profile.OrderedConditionalRules().Select(x => x.Name));
}
```

- [ ] **Step 3: Run tests and verify failure**

Run: `dotnet test tests/GameMacro.Core.Tests/GameMacro.Core.Tests.csproj`
Expected: compilation fails because model types do not exist.

- [ ] **Step 4: Implement immutable enums/value objects and mutable JSON models**

Implement `MacroRule.Validate()` with exact bounds: interval 20–60,000ms, protection 0–60,000ms, threshold 0–1, and non-empty action key. Implement `MacroProfile.OrderedConditionalRules()` as enabled conditional rules ordered by ascending `Priority`.

- [ ] **Step 5: Run all model tests**

Run: `dotnet test tests/GameMacro.Core.Tests/GameMacro.Core.Tests.csproj`
Expected: PASS.

### Task 2: Execution Engine and Priority Arbitration

**Files:**
- Create: `src/GameMacro.Core/Runtime/RuntimeContracts.cs`
- Create: `src/GameMacro.Core/Runtime/MacroEngine.cs`
- Create: `src/GameMacro.Core/Runtime/RuleRuntimeState.cs`
- Create: `tests/GameMacro.Core.Tests/Runtime/MacroEngineTests.cs`

**Interfaces:**
- Consumes: `MacroProfile`, `MacroRule`, and condition models from Task 1.
- Produces: `IWindowGate.IsTargetForegroundAsync`, `IConditionEvaluator.IsReadyAsync`, `IInputSink.EnqueueAsync`, `IClock.UtcNow/DelayAsync`, and `MacroEngine.StartAsync/StopAsync`.

- [ ] **Step 1: Write failing priority and foreground tests**

```csharp
[Fact]
public async Task Tick_sends_only_highest_priority_ready_rule()
{
    var fixture = EngineFixture.WithReadyRules("F1", "F2");
    await fixture.Engine.TickAsync(CancellationToken.None);
    Assert.Equal(["F1"], fixture.Input.Keys);
}

[Fact]
public async Task Tick_sends_nothing_when_target_is_not_foreground()
{
    var fixture = EngineFixture.WithReadyRules("F1");
    fixture.Window.IsForeground = false;
    await fixture.Engine.TickAsync(CancellationToken.None);
    Assert.Empty(fixture.Input.Keys);
}
```

- [ ] **Step 2: Verify tests fail**

Run: `dotnet test tests/GameMacro.Core.Tests/GameMacro.Core.Tests.csproj --filter MacroEngineTests`
Expected: compilation fails because runtime contracts are missing.

- [ ] **Step 3: Implement one deterministic engine tick**

`TickAsync` must check the window gate first, enqueue due fixed-interval rules independently, evaluate conditional rules in priority order, enqueue the first ready conditional rule only, and update per-rule protection timestamps after successful enqueue.

- [ ] **Step 4: Add cancellation and release tests**

```csharp
[Fact]
public async Task Stop_releases_all_held_inputs()
{
    var fixture = EngineFixture.WithReadyRules();
    await fixture.Engine.StopAsync();
    Assert.Equal(1, fixture.Input.ReleaseAllCount);
}
```

- [ ] **Step 5: Implement loop lifecycle and run tests**

Implement a single cancellable loop using `PeriodicTimer`; serialize `StartAsync` and `StopAsync` with `SemaphoreSlim`; always call `ReleaseAllAsync` in `finally`.

Run: `dotnet test tests/GameMacro.Core.Tests/GameMacro.Core.Tests.csproj`
Expected: PASS.

### Task 3: JSON Profile Storage and Import/Export

**Files:**
- Create: `src/GameMacro.Core/Storage/IProfileStore.cs`
- Create: `src/GameMacro.App/Services/JsonProfileStore.cs`
- Create: `tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj`
- Create: `tests/GameMacro.App.Tests/Services/JsonProfileStoreTests.cs`

**Interfaces:**
- Produces: `LoadAllAsync`, `SaveAsync`, `DeleteAsync`, `ExportAsync`, and `ImportAsync`.

- [ ] **Step 1: Write round-trip and corrupt-file tests**

```csharp
[Fact]
public async Task Save_then_load_preserves_profile()
{
    await store.SaveAsync(sample, CancellationToken.None);
    var loaded = await store.LoadAllAsync(CancellationToken.None);
    Assert.Equal(sample.Rules[0].ActionKey, loaded.Single().Rules[0].ActionKey);
}

[Fact]
public async Task Corrupt_primary_restores_backup_without_deleting_corrupt_file()
{
    await fileSystem.WriteAllTextAsync(primary, "{broken");
    await fileSystem.WriteAllTextAsync(backup, validJson);
    var loaded = await store.LoadAllAsync(CancellationToken.None);
    Assert.Single(loaded);
    Assert.True(fileSystem.Exists(primary + ".corrupt"));
}
```

- [ ] **Step 2: Verify tests fail, then implement atomic JSON storage**

Use `System.Text.Json` with camelCase and enum strings. Save to `.tmp`, copy the previous valid primary to `.bak`, then `File.Move(temp, primary, true)`. Import/export uses a zip containing `profile.json` and `templates/`.

- [ ] **Step 3: Run storage tests**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj`
Expected: PASS.

### Task 4: Windows Window Gate, Hotkeys, Capture, and Input

**Files:**
- Create: `src/GameMacro.App/Platform/NativeMethods.cs`
- Create: `src/GameMacro.App/Platform/WindowsWindowService.cs`
- Create: `src/GameMacro.App/Platform/GlobalHotkeyService.cs`
- Create: `src/GameMacro.App/Platform/WindowsCaptureService.cs`
- Create: `src/GameMacro.App/Platform/SendInputService.cs`
- Create: `tests/GameMacro.App.Tests/Platform/CoordinateConversionTests.cs`

**Interfaces:**
- Implements Task 2 runtime contracts and adds `RegisterToggleHotkey`, `RegisterEmergencyHotkey`, `ListWindows`, and `CaptureRegionAsync`.

- [ ] **Step 1: Write coordinate-conversion tests**

```csharp
[Theory]
[InlineData(0.5, 0.5, 1000, 800, 500, 400)]
[InlineData(0, 0, 1920, 1080, 0, 0)]
public void Relative_point_converts_to_client_pixel(double x, double y, int w, int h, int px, int py)
    => Assert.Equal((px, py), WindowCoordinates.ToPixels(x, y, w, h));
```

- [ ] **Step 2: Implement safe Win32 wrappers**

Declare only required APIs: `EnumWindows`, `GetWindowText`, `IsWindowVisible`, `IsIconic`, `GetForegroundWindow`, `GetClientRect`, `ClientToScreen`, `RegisterHotKey`, `UnregisterHotKey`, and `SendInput`. Check return values and convert failures to descriptive exceptions.

- [ ] **Step 3: Implement serialized input and release tracking**

Track keys/buttons pressed by this application in a `HashSet`; all sends pass through a `Channel<InputAction>` with one reader. `ReleaseAllAsync` sends key-up/button-up for the tracked set and clears it.

- [ ] **Step 4: Run tests without sending real input**

Run: `dotnet test`
Expected: PASS; tests use pure coordinate helpers and fake native API, never real `SendInput`.

### Task 5: Image Detection and Calibration

**Files:**
- Create: `src/GameMacro.App/Detection/OpenCvConditionEvaluator.cs`
- Create: `src/GameMacro.App/Detection/DetectionMetrics.cs`
- Create: `tests/GameMacro.App.Tests/Detection/OpenCvConditionEvaluatorTests.cs`
- Create: `tests/GameMacro.App.Tests/Fixtures/ready.png`
- Create: `tests/GameMacro.App.Tests/Fixtures/cooldown.png`

**Interfaces:**
- Implements: `IConditionEvaluator.IsReadyAsync`.
- Produces: `DetectionResult(bool IsReady, double Score, string Detail)`.

- [ ] **Step 1: Add fixed screenshot fixtures and failing tests**

```csharp
[Fact]
public async Task Brightness_detector_distinguishes_ready_and_cooldown()
{
    var ready = await evaluator.EvaluateAsync(readyRule, ReadyImage, default);
    var cooldown = await evaluator.EvaluateAsync(readyRule, CooldownImage, default);
    Assert.True(ready.IsReady);
    Assert.False(cooldown.IsReady);
}
```

- [ ] **Step 2: Implement three detection strategies**

Brightness/saturation computes normalized HSV means; color detection computes the fraction of pixels within configured HSV tolerance; template detection uses normalized correlation and compares it with the configured threshold. Return a score for UI calibration.

- [ ] **Step 3: Add two-sample stability filter**

The evaluator reports ready only after the same rule meets its condition for `RequiredStableSamples` consecutive captures; reset the counter immediately when a sample fails.

- [ ] **Step 4: Run detection tests**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter Detection`
Expected: PASS for both supplied fixtures.

### Task 6: WPF Shell and Profile Editor

**Files:**
- Create: `src/GameMacro.App/GameMacro.App.csproj`
- Create: `src/GameMacro.App/App.xaml`
- Create: `src/GameMacro.App/App.xaml.cs`
- Create: `src/GameMacro.App/MainWindow.xaml`
- Create: `src/GameMacro.App/MainWindow.xaml.cs`
- Create: `src/GameMacro.App/ViewModels/MainViewModel.cs`
- Create: `src/GameMacro.App/ViewModels/RuleViewModel.cs`
- Create: `src/GameMacro.App/Styles/DarkTheme.xaml`

**Interfaces:**
- Consumes all services from Tasks 2–5.
- Produces commands for create/copy/delete profile, add/delete/reorder rule, select window, save, import/export, start/stop, and emergency stop.

- [ ] **Step 1: Add WPF project and MVVM package**

Run:
```powershell
dotnet new wpf -n GameMacro.App -o src/GameMacro.App -f net8.0
dotnet sln add src/GameMacro.App/GameMacro.App.csproj
dotnet add src/GameMacro.App/GameMacro.App.csproj reference src/GameMacro.Core/GameMacro.Core.csproj
dotnet add src/GameMacro.App/GameMacro.App.csproj package CommunityToolkit.Mvvm
```

- [ ] **Step 2: Implement the approved three-pane layout**

Use a 280px profile list on the left. On the right, use an auto-height toolbar, a horizontally scrolling reorderable skill-card queue, a flexible condition editor, and an auto-height status bar. Bind all editable state through `MainViewModel`; code-behind is limited to window-message and drag/drop bridging.

- [ ] **Step 3: Implement modern dark styling**

Use neutral charcoal surfaces, one blue accent, 8px corner radius, Segoe UI, clear focus states, and green/gray readiness indicators. Do not copy the reference game's art, font, textures, or ornamental borders.

- [ ] **Step 4: Build the application**

Run: `dotnet build GameMacro.sln -c Debug`
Expected: build succeeds with zero errors.

### Task 7: Region Selection Overlay and Live Calibration

**Files:**
- Create: `src/GameMacro.App/Views/RegionSelectorWindow.xaml`
- Create: `src/GameMacro.App/Views/RegionSelectorWindow.xaml.cs`
- Create: `src/GameMacro.App/ViewModels/CalibrationViewModel.cs`
- Modify: `src/GameMacro.App/MainWindow.xaml`
- Modify: `src/GameMacro.App/ViewModels/MainViewModel.cs`
- Create: `tests/GameMacro.App.Tests/ViewModels/CalibrationViewModelTests.cs`

**Interfaces:**
- Produces: `SelectRegionAsync(nint targetWindow)` returning a normalized `ScreenRegion`; live `PreviewImage`, `Score`, and `IsReady` properties.

- [ ] **Step 1: Write normalized-region tests**

```csharp
[Fact]
public void Selection_is_saved_relative_to_client_area()
{
    var region = CalibrationViewModel.Normalize(new(200, 100, 100, 50), new(100, 50, 400, 200));
    Assert.Equal(new ScreenRegion(.25, .25, .25, .25), region);
}
```

- [ ] **Step 2: Implement transparent selection overlay**

Hide the main window, display a topmost transparent overlay on the target window client rectangle, support mouse drag selection and Escape cancellation, convert the selected rectangle to normalized client coordinates, then restore the main window.

- [ ] **Step 3: Implement live calibration**

While the calibration panel is visible, capture at 5 FPS, render the crop, display detector score and ready/cooldown state, and stop the preview task when the panel closes or selection changes.

- [ ] **Step 4: Run tests and build**

Run: `dotnet test && dotnet build GameMacro.sln -c Debug`
Expected: all tests pass and build succeeds.

### Task 8: Integration, Safety Shutdown, Publish, and Documentation

**Files:**
- Modify: `src/GameMacro.App/App.xaml.cs`
- Modify: `src/GameMacro.App/ViewModels/MainViewModel.cs`
- Create: `tests/GameMacro.App.Tests/Integration/RunLifecycleTests.cs`
- Create: `scripts/publish.ps1`
- Create: `README.md`

**Interfaces:**
- Connects profile storage, hotkeys, window gate, capture, detector, engine, and input queue into one application lifecycle.

- [ ] **Step 1: Write lifecycle integration tests**

```csharp
[Fact]
public async Task Foreground_loss_pauses_and_recovery_resumes()
{
    await harness.ToggleAsync();
    harness.Window.IsForeground = false;
    await harness.AdvanceAsync(200);
    Assert.Empty(harness.Input.Keys);
    harness.Window.IsForeground = true;
    await harness.AdvanceAsync(200);
    Assert.NotEmpty(harness.Input.Keys);
}
```

- [ ] **Step 2: Wire hotkeys and shutdown**

Register the selected profile toggle hotkey and one application-wide emergency hotkey. On session ending, window close, unhandled exception, or explicit emergency stop: stop the engine, release held input, unregister hotkeys, and flush profile storage.

- [ ] **Step 3: Add reproducible publish script**

```powershell
$ErrorActionPreference = 'Stop'
dotnet test "$PSScriptRoot\..\GameMacro.sln" -c Release
dotnet publish "$PSScriptRoot\..\src\GameMacro.App\GameMacro.App.csproj" `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -o "$PSScriptRoot\..\artifacts\win-x64"
```

- [ ] **Step 4: Document setup and calibration**

Document required Windows version, SDK build steps, selecting a target window, adding fixed and conditional rules, calibrating the first-row skill icons, setting toggle/emergency hotkeys, importing/exporting profiles, and the explicit no-bypass safety boundary.

- [ ] **Step 5: Perform final verification**

Run:
```powershell
dotnet test GameMacro.sln -c Release
powershell -ExecutionPolicy Bypass -File scripts/publish.ps1
Get-ChildItem artifacts/win-x64
```
Expected: all tests pass and `GameMacro.App.exe` exists in `artifacts/win-x64`.
