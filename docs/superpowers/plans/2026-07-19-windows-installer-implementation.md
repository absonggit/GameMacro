# Windows Installer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 生成可安装、可卸载的单文件 `GameMacro-Setup.exe`。

**Architecture:** PowerShell 脚本负责 .NET 自包含发布并定位 Inno Setup；`.iss` 文件定义按用户安装、快捷方式和卸载行为。

**Tech Stack:** .NET 8、PowerShell、Inno Setup、xUnit

## Global Constraints

- 不提交 Git。
- 不打包 `%LOCALAPPDATA%\GameMacro\Profiles`。
- 安装不要求管理员权限。
- 输出文件名固定为 `GameMacro-Setup.exe`。

---

### Task 1: 安装定义与结构测试

**Files:**
- Create: `tests/GameMacro.App.Tests/Packaging/InstallerDefinitionTests.cs`
- Create: `installer/GameMacro.iss`

- [ ] 写失败测试，验证安装定义不存在。
- [ ] 测试按用户目录、无管理员权限、快捷方式、卸载和输出文件名。
- [ ] 创建 Inno Setup 定义并让测试通过。

### Task 2: 一键构建脚本

**Files:**
- Create: `scripts/build-installer.ps1`
- Modify: `README.md`

- [ ] 编写先发布再调用 `ISCC.exe` 的构建脚本。
- [ ] 支持 PATH、Program Files 和本地用户安装位置中的 Inno Setup。
- [ ] 更新分发和安装包构建说明。

### Task 3: 验证与生成

- [ ] 运行完整解决方案测试。
- [ ] 检测或安装 Inno Setup。
- [ ] 执行构建脚本。
- [ ] 确认 `artifacts\installer\GameMacro-Setup.exe` 存在。
