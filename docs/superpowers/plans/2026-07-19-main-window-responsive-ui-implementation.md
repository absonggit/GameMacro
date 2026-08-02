# Main Window Responsive UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 优化主窗口的方案、热键和映射卡片布局，并将浮窗缩为无状态栏的 `200×40`。

**Architecture:** 抽取通用 WPF 按键名称转换器供技能映射和启停热键共享。主窗口使用禁用横向滚动的 WrapPanel 自动换行，浮窗继续由现有展示模型提供尺寸和按钮文字。

**Tech Stack:** .NET 8、WPF、xUnit

## Global Constraints

- 不提交 Git。
- 不改变图标识别和按键发送逻辑。
- 主窗口不提供启动按钮，保留全局热键与浮窗控制。

---

### Task 1: 通用点击按键绑定

**Files:**
- Create: `src/GameMacro.App/Platform/WpfKeyName.cs`
- Create: `tests/GameMacro.App.Tests/Platform/WpfKeyNameTests.cs`
- Modify: `src/GameMacro.App/MainWindow.xaml.cs`
- Modify: `src/GameMacro.App/MainWindow.xaml`

**Interfaces:**
- Produces: `WpfKeyName.FromKey(Key key) -> string?`。
- Consumes: `MacroProfile.ToggleHotkey`。

- [ ] 写失败测试，覆盖 F5、数字、字母、波浪键和不支持键。
- [ ] 运行专项测试，确认因 `WpfKeyName` 不存在而失败。
- [ ] 实现转换器，并让技能映射与启停热键共用点击后捕获逻辑。
- [ ] 运行专项测试确认通过。

### Task 2: 响应式主窗口与紧凑浮窗

**Files:**
- Modify: `src/GameMacro.App/MainWindow.xaml`
- Modify: `src/GameMacro.App/Overlay/OverlayPresentation.cs`
- Modify: `src/GameMacro.App/Overlay/GameOverlayWindow.xaml`
- Modify: `src/GameMacro.App/Overlay/GameOverlayWindow.xaml.cs`
- Modify: `tests/GameMacro.App.Tests/Overlay/OverlayPresentationTests.cs`

- [ ] 先将尺寸测试改为 `200×40` 并确认失败。
- [ ] 禁止映射列表横向滚动并启用按宽度自动换行。
- [ ] 强化方案项和浮窗开关样式，删除标题提示和主启动按钮。
- [ ] 删除浮窗状态列并缩为 `200×40`。
- [ ] 运行专项测试和 WPF 编译。

### Task 3: 单方案导入导出

**Files:**
- Create: `src/GameMacro.App/Services/ProfileTransfer.cs`
- Create: `tests/GameMacro.App.Tests/Services/ProfileTransferTests.cs`
- Modify: `src/GameMacro.App/MainWindow.xaml`
- Modify: `src/GameMacro.App/MainWindow.xaml.cs`

- [ ] 写失败测试，验证导入生成新 ID 和名称，并保留像素模板及方案设置。
- [ ] 实现格式化 JSON 导出与复制导入。
- [ ] 使用 Windows 打开/保存对话框接入顶部按钮。
- [ ] 运行专项测试和 WPF 编译。

### Task 4: 回归与发布

**Files:**
- Modify: `README.md`

- [ ] 更新主窗口、浮窗和导入导出操作说明。
- [ ] 运行 `dotnet test GameMacro.sln --no-restore`。
- [ ] 发布到 `artifacts/win-x64-game-overlay` 并确认 EXE 更新时间。
