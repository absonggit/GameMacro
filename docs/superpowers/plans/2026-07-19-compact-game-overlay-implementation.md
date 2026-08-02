# Compact Game Overlay Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将游戏浮窗缩小为 `250×40`，增加独立拖拽柄，并在启停按钮上显示当前方案热键。

**Architecture:** 使用纯 `OverlayPresentation` 类型集中定义紧凑尺寸和按钮文字，WPF 浮窗消费该展示模型；现有识别、方案和窗口定位逻辑保持不变。

**Tech Stack:** .NET 8、WPF、xUnit

## Global Constraints

- 不提交 Git。
- 浮窗尺寸固定为 `250×40`，方案选单宽 `96px`，拖拽柄宽 `18px`。
- 启停按钮显示当前方案的 `ToggleHotkey`。

---

### Task 1: 紧凑浮窗展示模型和界面

**Files:**
- Create: `src/GameMacro.App/Overlay/OverlayPresentation.cs`
- Create: `tests/GameMacro.App.Tests/Overlay/OverlayPresentationTests.cs`
- Modify: `src/GameMacro.App/Overlay/GameOverlayWindow.xaml`
- Modify: `src/GameMacro.App/Overlay/GameOverlayWindow.xaml.cs`
- Modify: `src/GameMacro.App/MainWindow.xaml.cs`

**Interfaces:**
- Produces: `OverlayPresentation.Width`, `Height`, `ToggleLabel(bool, string)`。
- Consumes: `MacroProfile.ToggleHotkey`。

- [ ] **Step 1: Write the failing test**

验证尺寸为 `270×44`，并验证停止和运行状态分别生成“启动 F5”和“停止 F5”。

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --no-restore --filter FullyQualifiedName~OverlayPresentationTests`

Expected: FAIL because `OverlayPresentation` does not exist.

- [ ] **Step 3: Write minimal implementation**

创建展示常量和文字方法；将浮窗 XAML 改为紧凑布局，并把当前方案热键传入 `UpdateState`。

- [ ] **Step 4: Run focused and full tests**

Run: `dotnet test GameMacro.sln --no-restore`

Expected: all tests pass.

- [ ] **Step 5: Publish locally**

Run: `dotnet publish src/GameMacro.App/GameMacro.App.csproj -c Release -r win-x64 --self-contained true --no-restore -p:PublishSingleFile=false -o artifacts/win-x64-game-overlay`

Expected: `GameMacro.App.exe` exists in the output directory.
