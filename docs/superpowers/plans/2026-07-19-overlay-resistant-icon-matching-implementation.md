# Overlay-Resistant Icon Matching Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prevent the dynamic-slot black gradient from causing F3 and `~` to exchange match ranking while preserving unknown-icon rejection.

**Architecture:** Keep the existing 32×32 pixel-template pipeline and candidate confidence gates. Make the pixel distance ignore the unstable top-right overlay area and add a low-cost, brightness-independent gradient-orientation descriptor over the remaining stable pixels.

**Tech Stack:** .NET 8, C#, WPF, xUnit, Inno Setup 6

## Global Constraints

- Do not change profile JSON or require users to recapture templates.
- Do not change dynamic-region selection or key sending.
- Keep matching allocation-free inside the per-candidate pixel loops except for fixed eight-bin arrays.
- Preserve `MaximumDistance = .30` and `MinimumLead = .025` unless a failing regression test proves either must change.
- Do not create a Git commit; the user requested local-only development.

---

### Task 1: Reproduce the F3/`~` black-gradient confusion

**Files:**
- Modify: `tests/GameMacro.App.Tests/Detection/PixelIconTemplateMatcherTests.cs`
- Modify: `tests/GameMacro.App.Tests/Detection/PixelIconTemplateBuilderTests.cs`

**Interfaces:**
- Consumes: `PixelIconTemplateBuilder.Create(byte[] bgra, int width, int height)` and `PixelIconTemplateMatcher.Match(...)`.
- Produces: deterministic test frames `ParallelStreaks`, `CurvedSweep`, and `AddTopRightGradient` used by matcher regression tests.

- [ ] **Step 1: Add deterministic icon-frame builders**

Add helpers to `TestIconFrames` that draw multiple narrow parallel red streaks for F3, a broad curved red-orange sweep for `~`, and a black alpha-like gradient covering the upper-right portion of a cloned BGRA frame.

- [ ] **Step 2: Add the failing forward and reverse regression tests**

```csharp
[Fact]
public void Top_right_dynamic_gradient_does_not_confuse_parallel_streaks_with_curved_sweep()
{
    var f3 = Candidate("F3", TestIconFrames.ParallelStreaks(108, 108));
    var tilde = Candidate("~", TestIconFrames.CurvedSweep(108, 108, darkTopRight: true));
    var sample = PixelIconTemplateBuilder.Create(
        TestIconFrames.AddTopRightGradient(TestIconFrames.ParallelStreaks(76, 80), 76, 80), 76, 80);

    Assert.Equal("F3", PixelIconTemplateMatcher.Match(sample, [f3, tilde])?.Mapping.ActionKey);
}
```

Add the inverse test with a curved-sweep runtime sample and assert `~` remains selected.

- [ ] **Step 3: Run the focused tests and verify RED**

Run:

```powershell
dotnet test tests\GameMacro.App.Tests\GameMacro.App.Tests.csproj --filter FullyQualifiedName~PixelIconTemplateMatcherTests --no-restore
```

Expected: the F3 gradient regression fails because the current full-region distance either chooses `~` or rejects the ambiguous result; existing tests remain green.

### Task 2: Add stable-region and gradient-direction matching

**Files:**
- Modify: `src/GameMacro.App/Detection/PixelIconTemplateMatcher.cs`
- Test: `tests/GameMacro.App.Tests/Detection/PixelIconTemplateMatcherTests.cs`

**Interfaces:**
- Consumes: two current-version `PixelIconTemplate` values.
- Produces: the existing `double PixelIconTemplateMatcher.Distance(...)` API with improved discrimination and no schema change.

- [ ] **Step 1: Add the stable-region mask**

Add `IsStablePixel(int x, int y)` and exclude the upper-right overlay zone from both sides of each shifted comparison. Keep existing outer-edge and bottom-label exclusions.

- [ ] **Step 2: Add an eight-bin unsigned gradient-orientation distance**

For stable pixels, compute horizontal and vertical luminance gradients, fold angles into `[0, π)`, accumulate magnitude-weighted eight-bin histograms, normalize each histogram, and return half the L1 distance. Ignore gradients below a small noise floor.

- [ ] **Step 3: Blend orientation with the current best shifted distance**

Compute orientation once per template pair, then combine it with the best shifted structure/edge/color score. Keep orientation as a minority weight so existing color and unknown-icon behavior remains intact.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run the Task 1 command. Expected: all `PixelIconTemplateMatcherTests` pass, including forward/reverse gradient cases and unknown rejection.

- [ ] **Step 5: Run all detection tests**

```powershell
dotnet test tests\GameMacro.App.Tests\GameMacro.App.Tests.csproj --filter FullyQualifiedName~Detection --no-restore
```

Expected: all detection tests pass.

### Task 3: Verify and package the fixed build

**Files:**
- Verify: `GameMacro.sln`
- Generate: `artifacts/installer/GameMacro-Setup.exe`

**Interfaces:**
- Consumes: the completed matcher implementation.
- Produces: a tested Windows installer containing the fixed recognizer.

- [ ] **Step 1: Run the full test suite**

```powershell
dotnet test GameMacro.sln --no-restore
```

Expected: Core and App suites pass with zero failures.

- [ ] **Step 2: Build the installer**

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts\build-installer.ps1 -Version 1.0.0
```

Expected: Inno Setup reports `Successful compile` and writes `artifacts/installer/GameMacro-Setup.exe`.

- [ ] **Step 3: Remove only regenerated installer-source intermediates**

Verify the resolved target is inside the repository, then remove `artifacts/win-x64-installer-source`. Preserve the final installer, application icon, language file, source, tests, spec, and plan.

- [ ] **Step 4: Record final installer size and SHA-256**

```powershell
Get-Item artifacts\installer\GameMacro-Setup.exe
Get-FileHash -Algorithm SHA256 artifacts\installer\GameMacro-Setup.exe
```

Expected: a non-empty installer and a 64-character SHA-256 value.
