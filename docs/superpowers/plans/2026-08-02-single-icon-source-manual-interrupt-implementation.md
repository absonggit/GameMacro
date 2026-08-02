# Single Icon Source and Manual Interrupt Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add exact single-icon capture and configurable physical manual-priority keys that pause automation until one second after the last key is released.

**Architecture:** Reuse the existing region selector and pixel-template pipeline for a single captured icon, appending it to the pending mapping collection without touching the saved batch source region. Store interrupt keys per profile, decode physical keyboard events through a small Win32 hook wrapper, and isolate timing/state in a pure `ManualInterruptGate` that the automation loop checks before capture or input.

**Tech Stack:** .NET 8, WPF, xUnit, Windows `WH_KEYBOARD_LL`, existing GDI screen capture and `SendInput` pipeline.

## Global Constraints

- Supported keys remain exactly `F1-F12`, `0-9`, `A-Z`, and `~`.
- Physical manual keys may equal skill mapping keys, but may not equal the profile toggle hotkey.
- Injected `SendInput` keyboard events must never activate manual interruption.
- The resume delay is fixed at 1 second after the last held interrupt key is released.
- Existing JSON profiles without `InterruptKeys` load as an empty list.
- Single-icon capture appends to pending mappings and never changes the saved batch source region.
- Do not change icon recognition thresholds, dynamic-region behavior, or the game overlay controls.
- Keep all work local and do not create Git commits.

---

### Task 1: Persist and validate interrupt keys

**Files:**
- Modify: `src/GameMacro.Core/Models/MacroProfile.cs`
- Modify: `src/GameMacro.Core/Models/ProfileInputValidator.cs`
- Modify: `tests/GameMacro.Core.Tests/Models/DynamicIconProfileTests.cs`

**Interfaces:**
- Produces: `MacroProfile.InterruptKeys : List<string>`.
- Produces: validation errors for unsupported keys, duplicates, and equality with `ToggleHotkey`.
- Preserves: interrupt keys may equal any `IconKeyMapping.ActionKey`.

- [ ] **Step 1: Write failing profile tests**

Add tests that serialize and restore `InterruptKeys`, verify missing JSON fields become an empty list, reject `F5` when `ToggleHotkey == "F5"`, reject duplicates, reject unsupported names, and accept an interrupt key equal to a mapping action key.

```csharp
[Fact]
public void Profile_round_trip_preserves_interrupt_keys()
{
    var profile = ValidProfile();
    profile.InterruptKeys = ["Q", "F3"];

    var restored = JsonSerializer.Deserialize<MacroProfile>(JsonSerializer.Serialize(profile))!;

    Assert.Equal(["Q", "F3"], restored.InterruptKeys);
}

[Fact]
public void Validation_allows_interrupt_key_to_equal_skill_key_but_not_toggle_key()
{
    var profile = ValidProfile();
    profile.InterruptKeys = [profile.IconMappings[0].ActionKey];
    Assert.DoesNotContain(ProfileInputValidator.Validate(profile), error => error.Contains("优先打断键"));

    profile.InterruptKeys = [profile.ToggleHotkey];
    Assert.Contains(ProfileInputValidator.Validate(profile), error => error.Contains("启停热键冲突"));
}
```

- [ ] **Step 2: Run tests and verify RED**

Run: `dotnet test tests/GameMacro.Core.Tests/GameMacro.Core.Tests.csproj --filter DynamicIconProfileTests --no-restore`

Expected: compile failure because `MacroProfile.InterruptKeys` does not exist.

- [ ] **Step 3: Add the model property and validation**

Add to `MacroProfile`:

```csharp
public List<string> InterruptKeys { get; set; } = [];
```

In `ProfileInputValidator.Validate`, validate every value with `InputKeyOptions.All`, reject case-insensitive duplicates, and reject equality with `ToggleHotkey`. Do not include interrupt keys in the existing skill-key conflict set.

- [ ] **Step 4: Run tests and verify GREEN**

Run: `dotnet test tests/GameMacro.Core.Tests/GameMacro.Core.Tests.csproj --filter DynamicIconProfileTests --no-restore`

Expected: all filtered tests pass.

### Task 2: Implement the manual interruption state gate

**Files:**
- Create: `src/GameMacro.App/Platform/ManualInterruptGate.cs`
- Create: `tests/GameMacro.App.Tests/Platform/ManualInterruptGateTests.cs`

**Interfaces:**
- Produces: `ManualInterruptGate(Func<DateTimeOffset>? clock = null, TimeSpan? resumeDelay = null)`.
- Produces: `void KeyDown(ushort virtualKey)`, `void KeyUp(ushort virtualKey)`, `bool IsHeld(ushort virtualKey)`, `bool IsPaused`, and `void Reset()`.
- Default resume delay: `TimeSpan.FromSeconds(1)`.

- [ ] **Step 1: Write failing gate tests**

Cover immediate pause, the one-second deadline, multiple held keys, repeated `KeyDown`, a new press during the delay, unknown `KeyUp`, and `Reset`.

```csharp
[Fact]
public void Last_key_up_starts_one_second_resume_delay()
{
    var now = DateTimeOffset.Parse("2026-08-02T00:00:00Z");
    var gate = new ManualInterruptGate(() => now);
    gate.KeyDown(0x31);
    gate.KeyDown(0x32);

    gate.KeyUp(0x31);
    Assert.True(gate.IsPaused);
    gate.KeyUp(0x32);
    now += TimeSpan.FromMilliseconds(999);
    Assert.True(gate.IsPaused);
    now += TimeSpan.FromMilliseconds(1);
    Assert.False(gate.IsPaused);
}
```

- [ ] **Step 2: Run tests and verify RED**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter ManualInterruptGateTests --no-restore`

Expected: compile failure because `ManualInterruptGate` does not exist.

- [ ] **Step 3: Implement the gate**

Use `HashSet<ushort>` for held keys and a nullable `DateTimeOffset` resume deadline. `KeyDown` adds the key and clears the deadline. `KeyUp` only acts on a key in the set; after removing the final key it sets `clock() + resumeDelay`. `IsPaused` returns true while the set is non-empty or the clock is before the deadline. `Reset` clears both fields.

- [ ] **Step 4: Run tests and verify GREEN**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter ManualInterruptGateTests --no-restore`

Expected: all gate tests pass.

### Task 3: Decode and monitor physical keyboard events

**Files:**
- Create: `src/GameMacro.App/Platform/PhysicalKeyboardEventArgs.cs`
- Create: `src/GameMacro.App/Platform/PhysicalKeyboardEventDecoder.cs`
- Create: `src/GameMacro.App/Platform/PhysicalKeyboardMonitor.cs`
- Create: `tests/GameMacro.App.Tests/Platform/PhysicalKeyboardEventDecoderTests.cs`
- Modify: `src/GameMacro.App/Platform/NativeMethods.cs`

**Interfaces:**
- Produces: `PhysicalKeyboardEventArgs : EventArgs` with `ushort VirtualKey` and `bool IsDown`.
- Produces: `PhysicalKeyboardEventDecoder.TryDecode(int code, int message, uint virtualKey, uint flags, out PhysicalKeyboardEventArgs? value)`.
- Produces: disposable `PhysicalKeyboardMonitor` with `event EventHandler<PhysicalKeyboardEventArgs>? KeyChanged`, `bool IsRunning`, `bool Start()`, and `void Stop()`.

- [ ] **Step 1: Write failing decoder tests**

```csharp
[Fact]
public void Decoder_ignores_injected_input_and_accepts_physical_down_and_up()
{
    Assert.False(PhysicalKeyboardEventDecoder.TryDecode(0, 0x100, 0x31, 0x10, out _));
    Assert.True(PhysicalKeyboardEventDecoder.TryDecode(0, 0x100, 0x31, 0, out var down));
    Assert.Equal((0x31, true), (down!.VirtualKey, down.IsDown));
    Assert.True(PhysicalKeyboardEventDecoder.TryDecode(0, 0x101, 0x31, 0, out var up));
    Assert.Equal((0x31, false), (up!.VirtualKey, up.IsDown));
}
```

Also reject negative hook codes and unrelated Windows messages, and accept `WM_SYSKEYDOWN/WM_SYSKEYUP`.

- [ ] **Step 2: Run tests and verify RED**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter PhysicalKeyboardEventDecoderTests --no-restore`

Expected: compile failure because the decoder types do not exist.

- [ ] **Step 3: Implement decoder and monitor**

The decoder must check `code >= 0`, `(flags & NativeMethods.LlkhfInjected) == 0`, and the four keyboard messages. The monitor retains its delegate in a field, installs `WH_KEYBOARD_LL` with `SetWindowsHookEx`, reads `LowLevelKeyboardInput` through `Marshal.PtrToStructure`, raises decoded events, and always returns `CallNextHookEx`. `Stop` unhooks once; `Dispose` calls `Stop`.

- [ ] **Step 4: Run tests and verify GREEN**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter PhysicalKeyboardEventDecoderTests --no-restore`

Expected: all decoder tests pass.

### Task 4: Build and de-duplicate a single captured mapping

**Files:**
- Create: `src/GameMacro.App/Detection/SingleIconMappingBuilder.cs`
- Create: `tests/GameMacro.App.Tests/Detection/SingleIconMappingBuilderTests.cs`

**Interfaces:**
- Consumes: `CapturedRegion`, `PendingIconMapping`, `PixelIconTemplateBuilder`, and `IconVisualSignature.Distance`.
- Produces: `PendingIconMapping SingleIconMappingBuilder.Build(CapturedRegion captured)`.
- Produces: `bool SingleIconMappingBuilder.IsDuplicate(PendingIconMapping candidate, IEnumerable<PendingIconMapping> existing)`.

- [ ] **Step 1: Write failing mapping tests**

Create a valid BGRA icon fixture and assert `Build` preserves the preview/signature, stores a deserializable full pixel template, and leaves the action key unassigned. Build a second item with the same signature and assert `IsDuplicate` is true; use a clearly different signature and assert false.

```csharp
[Fact]
public void Build_creates_one_pending_mapping_with_full_pixel_template()
{
    var pixels = TestIconFrames.Ring(64, 64);
    var captured = new CapturedRegion(pixels, 64, 64, [0.1, 0.2], "png");

    var item = SingleIconMappingBuilder.Build(captured);

    Assert.Equal("png", item.PreviewPng);
    Assert.NotNull(PixelIconTemplate.Deserialize(item.PixelTemplateData));
    Assert.DoesNotContain(item.ActionKey, InputKeyOptions.All);
}
```

- [ ] **Step 2: Run tests and verify RED**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter SingleIconMappingBuilderTests --no-restore`

Expected: compile failure because `SingleIconMappingBuilder` does not exist.

- [ ] **Step 3: Implement builder and duplicate check**

Mirror the fields created by `BatchMappingBuilder.Build` for exactly one capture. Use the existing `.06` visual-signature duplicate limit so batch and single additions follow one policy.

- [ ] **Step 4: Run tests and verify GREEN**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter SingleIconMappingBuilderTests --no-restore`

Expected: all builder tests pass.

### Task 5: Add single-icon capture and interrupt-key configuration UI

**Files:**
- Create: `src/GameMacro.App/Platform/InterruptKeyBindingEditor.cs`
- Create: `tests/GameMacro.App.Tests/Platform/InterruptKeyBindingEditorTests.cs`
- Modify: `src/GameMacro.App/MainWindow.xaml`
- Modify: `src/GameMacro.App/MainWindow.xaml.cs`
- Modify: `tests/GameMacro.App.Tests/Services/JsonProfileStoreTests.cs`

**Interfaces:**
- Consumes: `SingleIconMappingBuilder`, `MacroProfile.InterruptKeys`, and the existing direct key-capture helpers.
- Produces: `InterruptKeyBindingEditor.TryAdd(ICollection<string> keys, string key, string toggleHotkey, out string? error)`.
- Adds handlers: `CaptureSingleIcon_Click`, `AddInterruptKey_Click`, and `DeleteInterruptKey_Click`.
- Adds fields: `_interruptKeys : ObservableCollection<string>` and `_awaitingInterruptKey : bool`.

- [ ] **Step 1: Add failing binding-editor tests**

Test that a supported key is appended, duplicates are idempotent, the toggle key is rejected, and an unsupported key is rejected. Also extend `JsonProfileStoreTests` to save/load a profile with `InterruptKeys = ["Q", "E"]` and to load legacy JSON without the property as an empty list.

- [ ] **Step 2: Run tests and verify RED**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter InterruptKeyBindingEditorTests --no-restore`

Expected: compile failure because `InterruptKeyBindingEditor` does not exist.

- [ ] **Step 3: Implement the binding editor and add UI controls**

Implement `TryAdd` using `InputKeyOptions.All`, ordinal-ignore-case duplicate comparison, and toggle-key conflict detection. Return `false` with a Chinese error for invalid/conflicting input; return `true` for a new key and for an already-present key without adding twice.

In the region row add an accent button labeled `框选单个技能`. In the bottom settings border add a compact `ItemsControl` backed by `_interruptKeys`, using a horizontal `WrapPanel`; each item shows the key and a small `×` button. Add an `添加打断键` button that enters direct key-capture mode.

- [ ] **Step 4: Implement single capture**

Follow the existing selector lifecycle: require the target window, hide the main window, wait 150 ms, select within client bounds, wait 100 ms, call `_capture.CaptureRegion(_profile, region)`, build one pending mapping, reject duplicates, append otherwise, then show/activate the main window. Do not assign any `Source*` property.

- [ ] **Step 5: Implement interrupt-key editing**

On profile selection, replace `_interruptKeys` from `profile.InterruptKeys`. `AddInterruptKey_Click` cancels mapping/toggle capture and enters interrupt capture. Extend `MainWindow_PreviewKeyDown`: Escape cancels all capture modes; a supported new key appends once; a key equal to `ToggleHotkey` is rejected with status text. `DeleteInterruptKey_Click` removes the clicked string. `ApplyProfileFields` writes `_interruptKeys.ToList()` to the profile.

- [ ] **Step 6: Run persistence and existing UI-adjacent tests**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter "InterruptKeyBindingEditorTests|JsonProfileStoreTests|BatchMappingBuilderTests|WpfKeyNameTests" --no-restore`

Expected: all selected tests pass.

### Task 6: Integrate physical interruption into the automation lifecycle

**Files:**
- Create: `src/GameMacro.App/Platform/ManualInterruptRouter.cs`
- Modify: `src/GameMacro.App/MainWindow.xaml.cs`
- Modify: `src/GameMacro.App/Platform/WindowsWindowService.cs`
- Create: `tests/GameMacro.App.Tests/Platform/ManualInterruptRouterTests.cs`

**Interfaces:**
- Consumes: `PhysicalKeyboardMonitor.KeyChanged`, `ManualInterruptGate`, `VirtualKeyParser.Parse`, and `MacroProfile.InterruptKeys`.
- Produces: `ManualInterruptRouter.Handle(PhysicalKeyboardEventArgs value, bool automationRunning, bool targetForeground, IReadOnlySet<ushort> configuredKeys)`.
- Produces: synchronous `bool WindowsWindowService.IsTargetForeground(MacroProfile profile)`; the existing async interface delegates to it.

- [ ] **Step 1: Add failing routing tests**

Test that key-down is ignored while stopped, outside the target foreground, or not configured; configured physical key-down in the foreground pauses the gate; and key-up for an already-held key is processed even after the target loses foreground.

```csharp
[Fact]
public void Tracked_key_up_is_processed_after_target_loses_foreground()
{
    var now = DateTimeOffset.UtcNow;
    var gate = new ManualInterruptGate(() => now);
    var router = new ManualInterruptRouter(gate);
    router.Handle(new PhysicalKeyboardEventArgs(0x31, true), true, true, new HashSet<ushort> { 0x31 });

    router.Handle(new PhysicalKeyboardEventArgs(0x31, false), true, false, new HashSet<ushort> { 0x31 });

    now += TimeSpan.FromSeconds(1);
    Assert.False(gate.IsPaused);
}
```

- [ ] **Step 2: Run test and verify RED**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter ManualInterruptRouterTests --no-restore`

Expected: compile failure because `ManualInterruptRouter` does not exist.

- [ ] **Step 3: Implement the router and add monitor/gate lifecycle**

The router sends valid foreground key-down events to the gate. It sends key-up only when `gate.IsHeld(value.VirtualKey)`, regardless of foreground/running state, so a tracked key cannot remain stuck.

Create one monitor and one gate for the window. Start the monitor during `MainWindow_Loaded`; if installation fails, retain a clear failure status and reject `ToggleMonitoring`. Subscribe once to `KeyChanged`. On close, unsubscribe/dispose. Reset the gate in `StopMonitoring` and before loading another profile.

- [ ] **Step 4: Route physical events**

For a physical key-down, require running automation, current target foreground, and membership in parsed current interrupt keys before calling `KeyDown`. For key-up, call `KeyUp` whenever `gate.IsHeld(virtualKey)` even if the game has since lost foreground. Do not suppress or consume the original keyboard event.

- [ ] **Step 5: Gate the automation tick**

At the start of `AutomationTickAsync`, after the reentrancy/profile checks and before window lookup/capture, return while `gate.IsPaused`. Set status text to `状态：手动优先，最后松键后 1 秒恢复`. Do not stop the timer, reset the recognizer, or enqueue any missed key afterward.

- [ ] **Step 6: Run platform and detection tests**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter "ManualInterruptGateTests|ManualInterruptRouterTests|PhysicalKeyboardEventDecoderTests|DynamicIconRecognizerTests" --no-restore`

Expected: all selected tests pass.

### Task 7: Documentation, complete verification, and installer rebuild

**Files:**
- Modify: `README.md`
- Generated: `artifacts/installer/GameMacro-Setup.exe`

**Interfaces:**
- Documents: single-icon append workflow, multiple manual interrupt keys, physical-only behavior, and one-second recovery.

- [ ] **Step 1: Update README**

Add `框选单个技能` as an alternative to batch source scanning. Explain that interrupt keys are configured per profile, pause only from real keyboard input, allow overlap with mapped skills, and resume one second after the final release.

- [ ] **Step 2: Run the complete solution tests**

Run: `dotnet test GameMacro.sln --no-restore`

Expected: zero failures in Core and App test projects.

- [ ] **Step 3: Build the installer**

Run: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/build-installer.ps1`

Expected: exit code 0 and a new `artifacts/installer/GameMacro-Setup.exe`.

- [ ] **Step 4: Safely remove installer staging and compute hash**

Resolve `artifacts/win-x64-installer-source`, verify its absolute path begins with the workspace root plus a directory separator, then remove it recursively. Compute SHA256 for the installer and report the file path, size, hash, test counts, and that no Git commit was created.
