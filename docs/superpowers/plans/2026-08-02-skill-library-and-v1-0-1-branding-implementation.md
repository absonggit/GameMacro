# Skill Library and v1.0.1 Branding Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将程序统一为“按键助手 v1.0.1”，把内嵌在方案中的图标模板迁移到全局技能库，并提供按职业分类、拖拽加入方案的技能库侧栏。

**Architecture:** 使用 `%LOCALAPPDATA%\GameMacro\SkillLibrary.json` 保存职业分类和视觉模板；方案的 `IconKeyMapping` 只持有模板 ID、按键和启用状态。应用启动时通过幂等迁移器把旧方案模板合并进技能库，运行时由解析器将方案引用与模板组合为识别器现有输入，避免重写识别算法。

**Tech Stack:** .NET 8、C#、WPF、System.Text.Json、xUnit、Inno Setup 6、Windows SendInput/GDI 截图现有实现。

## Global Constraints

- 主窗口显示“按键助手 v1.0.1”，快捷方式显示“按键助手”。
- 程序、文件和安装器版本统一为 `1.0.1`；四段程序集/文件版本为 `1.0.1.0`。
- 内部程序集名、命名空间、安装目录、配置目录和安装包文件名保持现状。
- 技能模板不保存名称，只按用户管理的职业分类分组；必须存在默认“未分类”。
- 同一模板 ID 在同一方案中只引用一次；不同模板可以绑定同一按键。
- 已被任一方案引用的模板不得删除。
- 单方案导入导出必须携带其引用的模板和职业分类。
- 不引入数据库或第三方 UI/图像依赖。
- 用户要求本地开发且不提交，因此本计划不执行 Git commit。

---

### Task 1: 统一程序品牌与版本

**Files:**
- Modify: `src/GameMacro.App/GameMacro.App.csproj`
- Modify: `src/GameMacro.App/MainWindow.xaml`
- Modify: `installer/GameMacro.iss`
- Modify: `scripts/build-installer.ps1`
- Modify: `README.md`
- Modify: `tests/GameMacro.App.Tests/Packaging/InstallerDefinitionTests.cs`

**Interfaces:**
- Produces: 程序元数据版本 `1.0.1`、窗口标题“按键助手 v1.0.1”、安装产品名和快捷方式“按键助手”。

- [ ] **Step 1: 先修改打包测试，声明新的品牌与版本要求**

```csharp
Assert.Contains("#define MyAppVersion \"1.0.1\"", installer);
Assert.Contains("#define MyAppName \"按键助手\"", installer);
Assert.Contains(@"{autoprograms}\{#MyAppName}", installer);
Assert.Contains(@"{autodesktop}\{#MyAppName}", installer);
Assert.Contains("<Product>按键助手</Product>", project);
Assert.Contains("<Version>1.0.1</Version>", project);
Assert.Contains("<FileVersion>1.0.1.0</FileVersion>", project);
```

- [ ] **Step 2: 运行测试并确认因旧名称/旧版本失败**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter InstallerDefinitionTests --no-restore`

Expected: FAIL，输出指出仍为旧产品名或 `1.0.0`。

- [ ] **Step 3: 设置项目、窗口、安装器和脚本版本**

在 `GameMacro.App.csproj` 的 `PropertyGroup` 增加：

```xml
<Product>按键助手</Product>
<AssemblyTitle>按键助手</AssemblyTitle>
<Version>1.0.1</Version>
<AssemblyVersion>1.0.1.0</AssemblyVersion>
<FileVersion>1.0.1.0</FileVersion>
<InformationalVersion>1.0.1</InformationalVersion>
```

将主窗口标题改为 `按键助手 v1.0.1`；将 Inno Setup 的 `MyAppName`、快捷方式和启动说明改为宏 `{#MyAppName}`；把安装器和构建脚本默认版本改为 `1.0.1`。README 标题和分发说明同步改名，但保留 `GameMacro-Setup.exe` 与 `%LOCALAPPDATA%\GameMacro`。

- [ ] **Step 4: 运行打包定义测试**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter InstallerDefinitionTests --no-restore`

Expected: PASS。

### Task 2: 建立技能库模型、目录操作与可靠存储

**Files:**
- Create: `src/GameMacro.Core/Models/SkillCategory.cs`
- Create: `src/GameMacro.Core/Models/SkillTemplate.cs`
- Create: `src/GameMacro.Core/Models/SkillLibrary.cs`
- Create: `src/GameMacro.App/Services/SkillLibraryCatalog.cs`
- Create: `src/GameMacro.App/Services/JsonSkillLibraryStore.cs`
- Create: `tests/GameMacro.App.Tests/Services/SkillLibraryCatalogTests.cs`
- Create: `tests/GameMacro.App.Tests/Services/JsonSkillLibraryStoreTests.cs`

**Interfaces:**
- Produces: `SkillLibraryCatalog.EnsureUncategorized`, `FindDuplicate`, `AddTemplates`, `CanDeleteTemplate`, `DeleteTemplate`。
- Produces: `JsonSkillLibraryStore.LoadAsync` 与 `SaveAsync`。
- Consumes: 现有图标签名距离规则 `IconVisualSignature.Distance`。

- [ ] **Step 1: 写模型与目录行为的失败测试**

```csharp
[Fact]
public void AddTemplates_deduplicates_identical_visuals_but_keeps_distinct_variants()
{
    var library = new SkillLibrary();
    var catalog = new SkillLibraryCatalog(library);
    var category = catalog.EnsureUncategorized();
    var first = TestTemplate(category.Id, [0.1, 0.2]);
    var duplicate = TestTemplate(category.Id, [0.1, 0.2]);
    var variant = TestTemplate(category.Id, [0.8, 0.7]);

    var result = catalog.AddTemplates(category.Id, [first, duplicate, variant]);

    Assert.Equal(2, result.Added.Count);
    Assert.Equal(2, library.Templates.Count);
}

[Fact]
public void Referenced_template_cannot_be_deleted()
{
    var result = catalog.CanDeleteTemplate(template.Id, [profile]);
    Assert.False(result.Allowed);
    Assert.Contains(profile.Name, result.ReferencingProfiles);
}
```

- [ ] **Step 2: 运行目录测试并确认缺少类型而失败**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter SkillLibraryCatalogTests --no-restore`

Expected: FAIL，编译器报告 `SkillLibrary`/`SkillLibraryCatalog` 不存在。

- [ ] **Step 3: 实现最小模型与目录服务**

```csharp
public sealed class SkillCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "未分类";
}

public sealed class SkillTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public double[] Signature { get; set; } = [];
    public string PreviewPng { get; set; } = string.Empty;
    public double MatchThreshold { get; set; } = .18;
    public byte[] PixelTemplateData { get; set; } = [];
}

public sealed class SkillLibrary
{
    public int Version { get; set; } = 1;
    public List<SkillCategory> Categories { get; set; } = [];
    public List<SkillTemplate> Templates { get; set; } = [];
}
```

`FindDuplicate` 使用与现有单图标去重一致的签名距离阈值 `.06`。`DeleteTemplate` 必须先调用引用检查；分类仅在没有模板时允许删除。

- [ ] **Step 4: 写存储失败测试**

覆盖首次加载创建默认分类、保存后重载、第二次保存产生 `.bak`、主 JSON 损坏时复制 `.corrupt` 并从 `.bak` 恢复、主文件和备份都损坏时抛出可读异常。

- [ ] **Step 5: 运行存储测试并确认失败**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter JsonSkillLibraryStoreTests --no-restore`

Expected: FAIL，`JsonSkillLibraryStore` 尚不存在。

- [ ] **Step 6: 实现可靠 JSON 存储**

```csharp
public sealed class JsonSkillLibraryStore(string path)
{
    public Task<SkillLibrary> LoadAsync(CancellationToken cancellationToken);
    public Task SaveAsync(SkillLibrary library, CancellationToken cancellationToken);
}
```

保存到 `path + ".tmp"`，已有主文件先复制为 `path + ".bak"`，再 `File.Move(temp, path, true)`。反序列化主文件失败时复制为 `path + ".corrupt"` 并读取备份；无可用备份时抛 `InvalidDataException`，不得覆盖损坏文件。

- [ ] **Step 7: 运行技能库服务测试**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter "SkillLibraryCatalogTests|JsonSkillLibraryStoreTests" --no-restore`

Expected: PASS。

### Task 3: 将方案映射改为模板引用并实现旧方案迁移

**Files:**
- Modify: `src/GameMacro.Core/Models/MacroProfile.cs`
- Modify: `src/GameMacro.Core/Models/IconKeyMapping.cs`
- Create: `src/GameMacro.App/Services/SkillMappingResolver.cs`
- Create: `src/GameMacro.App/Services/LegacySkillLibraryMigrator.cs`
- Modify: `src/GameMacro.App/Services/JsonProfileStore.cs`
- Create: `tests/GameMacro.App.Tests/Services/SkillMappingResolverTests.cs`
- Create: `tests/GameMacro.App.Tests/Services/LegacySkillLibraryMigratorTests.cs`
- Modify: `tests/GameMacro.App.Tests/Services/JsonProfileStoreTests.cs`

**Interfaces:**
- Adds: `IconKeyMapping.SkillTemplateId : Guid`。
- Produces: `SkillMappingResolver.Resolve(MacroProfile, SkillLibrary) -> SkillMappingResolution`。
- Produces: `LegacySkillLibraryMigrator.Migrate(MacroProfile, SkillLibrary) -> MigrationResult`。

- [ ] **Step 1: 写解析和迁移失败测试**

```csharp
[Fact]
public void Resolve_combines_template_pixels_with_profile_key()
{
    var result = SkillMappingResolver.Resolve(profile, library);
    var resolved = Assert.Single(result.Mappings);
    Assert.Equal("F3", resolved.ActionKey);
    Assert.Equal(template.PixelTemplateData, resolved.PixelTemplateData);
}

[Fact]
public void Migrate_reuses_duplicate_template_and_preserves_mapping_key()
{
    var result = LegacySkillLibraryMigrator.Migrate(profileWithEmbeddedTemplate, library);
    Assert.True(result.Changed);
    Assert.Equal(existingTemplate.Id, profileWithEmbeddedTemplate.IconMappings[0].SkillTemplateId);
    Assert.Equal("F3", profileWithEmbeddedTemplate.IconMappings[0].ActionKey);
    Assert.Single(library.Templates);
}

[Fact]
public void Migration_is_idempotent()
{
    LegacySkillLibraryMigrator.Migrate(profile, library);
    var second = LegacySkillLibraryMigrator.Migrate(profile, library);
    Assert.False(second.Changed);
    Assert.Single(library.Templates);
}
```

- [ ] **Step 2: 运行测试并确认因引用/迁移 API 缺失而失败**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter "SkillMappingResolverTests|LegacySkillLibraryMigratorTests" --no-restore`

Expected: FAIL。

- [ ] **Step 3: 增加模板 ID 并实现解析器**

`IconKeyMapping` 增加 `SkillTemplateId`。保留旧模板字段用于读取 v2 JSON 和构造运行时解析结果；迁移成功后把旧字段清空。解析器为每个有效引用创建仅供运行时使用的 `IconKeyMapping` 副本，复制模板的签名、预览、阈值和像素数据，同时保留方案按键与启用状态。返回 `MissingTemplateIds`，供启动校验和 UI 显示。

- [ ] **Step 4: 实现幂等迁移器**

```csharp
public sealed record MigrationResult(bool Changed, int AddedTemplates, int ReusedTemplates);

public static MigrationResult Migrate(MacroProfile profile, SkillLibrary library)
```

对 `SkillTemplateId == Guid.Empty` 且旧模板已校准的映射进行迁移；先按 `.06` 签名距离查重，找不到才添加到“未分类”。设置模板 ID 后清空内嵌模板数据。将 `MacroProfile.Version` 升到 `3`。

- [ ] **Step 5: 调整方案存储测试，确保 v2 JSON 仍可读取且保存会产生备份**

旧方案加载本身不隐式访问技能库；应用启动编排负责迁移。`JsonProfileStore` 保留 v1 到 v2 的已有升级，并允许 v2 进入迁移器。

- [ ] **Step 6: 运行迁移与存储测试**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter "SkillMappingResolverTests|LegacySkillLibraryMigratorTests|JsonProfileStoreTests" --no-restore`

Expected: PASS。

### Task 4: 让单方案导入导出携带技能模板

**Files:**
- Create: `src/GameMacro.App/Services/ProfileExportPackage.cs`
- Create: `src/GameMacro.App/Services/SkillLibraryPackageMerger.cs`
- Modify: `src/GameMacro.App/Services/ProfileTransfer.cs`
- Modify: `tests/GameMacro.App.Tests/Services/ProfileTransferTests.cs`
- Create: `tests/GameMacro.App.Tests/Services/SkillLibraryPackageMergerTests.cs`

**Interfaces:**
- Produces: `ProfileTransfer.Serialize(MacroProfile, SkillLibrary) -> string`。
- Produces: `ProfileTransfer.ImportAsCopy(string, SkillLibrary) -> ProfileImportResult`。
- Produces: `SkillLibraryPackageMerger.Merge(ProfileExportPackage, SkillLibrary)`，返回重写模板 ID 后的方案。

- [ ] **Step 1: 写打包、去重和 ID 冲突的失败测试**

```csharp
[Fact]
public void Export_contains_only_templates_used_by_profile()
{
    var json = ProfileTransfer.Serialize(profile, library);
    var package = JsonSerializer.Deserialize<ProfileExportPackage>(json, options)!;
    Assert.Single(package.Templates);
    Assert.Equal(usedTemplate.Id, package.Templates[0].Id);
}

[Fact]
public void Import_rewrites_reference_when_same_id_has_different_content()
{
    var result = ProfileTransfer.ImportAsCopy(jsonWithConflict, localLibrary);
    Assert.NotEqual(conflictingId, result.Profile.IconMappings[0].SkillTemplateId);
    Assert.Equal(2, localLibrary.Templates.Count);
}
```

还要测试：按签名识别到本机重复模板时复用本机 ID；旧版仅含 `MacroProfile` 的导入 JSON 仍可导入并交给迁移器处理。

- [ ] **Step 2: 运行导入导出测试并确认旧 API 不满足要求**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter "ProfileTransferTests|SkillLibraryPackageMergerTests" --no-restore`

Expected: FAIL。

- [ ] **Step 3: 实现导出包与合并器**

```csharp
public sealed class ProfileExportPackage
{
    public int FormatVersion { get; set; } = 2;
    public MacroProfile Profile { get; set; } = new();
    public List<SkillCategory> Categories { get; set; } = [];
    public List<SkillTemplate> Templates { get; set; } = [];
}

public sealed record ProfileImportResult(MacroProfile Profile, bool LibraryChanged);
```

合并顺序为：同 ID 同内容复用；同 ID 不同内容生成 ID；不同 ID 但视觉距离不超过 `.06` 时复用本机模板；否则新增。每次 ID 替换都同步重写导入方案引用。分类 ID 冲突时按名称复用或生成新 ID。

- [ ] **Step 4: 运行导入导出测试**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter "ProfileTransferTests|SkillLibraryPackageMergerTests" --no-restore`

Expected: PASS。

### Task 5: 实现方案添加逻辑、拖放契约与删除保护

**Files:**
- Create: `src/GameMacro.App/Services/ProfileSkillEditor.cs`
- Create: `src/GameMacro.App/ViewModels/SkillTemplateCard.cs`
- Create: `src/GameMacro.App/SkillTemplateDragData.cs`
- Create: `tests/GameMacro.App.Tests/Services/ProfileSkillEditorTests.cs`

**Interfaces:**
- Produces: `ProfileSkillEditor.AddTemplate`, `RemoveMapping`, `AssignKey`。
- Produces: 拖放格式常量 `SkillTemplateDragData.Format` 和 `TemplateId`。

- [ ] **Step 1: 写方案编辑失败测试**

```csharp
[Fact]
public void Different_templates_may_share_one_action_key()
{
    var first = editor.AddTemplate(profile, firstTemplate.Id);
    var second = editor.AddTemplate(profile, secondTemplate.Id);
    editor.AssignKey(first.Id, "4");
    editor.AssignKey(second.Id, "4");
    Assert.All(profile.IconMappings, mapping => Assert.Equal("4", mapping.ActionKey));
}

[Fact]
public void Adding_same_template_twice_returns_existing_mapping()
{
    var first = editor.AddTemplate(profile, template.Id);
    var second = editor.AddTemplate(profile, template.Id);
    Assert.Equal(first.Id, second.Id);
    Assert.Single(profile.IconMappings);
}
```

- [ ] **Step 2: 运行测试并确认失败**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter ProfileSkillEditorTests --no-restore`

Expected: FAIL。

- [ ] **Step 3: 实现纯方案编辑服务和拖放载荷**

新增映射默认 `Enabled = true`、`ActionKey = "点击设置"`，只填模板 ID。完全重复拖入返回现有映射 ID，供 UI 定位高亮；删除映射只修改方案列表。按键校验继续复用 `InputKeyOptions`/`VirtualKeyParser`。

- [ ] **Step 4: 运行方案编辑测试**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter ProfileSkillEditorTests --no-restore`

Expected: PASS。

### Task 6: 接入主窗口技能库侧栏、扫描与运行时解析

**Files:**
- Create: `src/GameMacro.App/SkillLibraryPanel.xaml`
- Create: `src/GameMacro.App/SkillLibraryPanel.xaml.cs`
- Modify: `src/GameMacro.App/MainWindow.xaml`
- Modify: `src/GameMacro.App/MainWindow.xaml.cs`
- Modify: `src/GameMacro.App/ViewModels/PendingIconMapping.cs`
- Create: `src/GameMacro.App/Detection/SkillTemplateFactory.cs`
- Create: `src/GameMacro.App/Services/SkillLibraryStartup.cs`
- Modify: `src/GameMacro.Core/Models/ProfileInputValidator.cs`
- Modify: `tests/GameMacro.Core.Tests/Models/DynamicIconProfileTests.cs`
- Create: `tests/GameMacro.App.Tests/Detection/SkillTemplateFactoryTests.cs`
- Create: `tests/GameMacro.App.Tests/Services/SkillLibraryStartupTests.cs`

**Interfaces:**
- Consumes: Task 2 的技能库存储/目录服务、Task 3 的解析器/迁移器、Task 5 的方案编辑器。
- Produces: `SkillTemplateFactory.FromCapturedRegion` 和 `FromSegmentedIcons`。
- Produces: 右侧可收起技能库、职业管理、批量/单图标入库、拖入/双击加入方案、运行时模板解析。

- [ ] **Step 1: 写启动编排和缺失模板校验失败测试**

抽出可单测的 `SkillLibraryStartup.LoadAndMigrateAsync(store, profiles, profileStore)`：先加载技能库，再迁移所有方案；技能库先保存，随后保存发生变化的方案。测试首次启动、多个旧方案模板去重、第二次运行零变化。为 `ProfileInputValidator` 增加 `Validate(MacroProfile profile, IReadOnlyCollection<Guid> missingTemplateIds)` 重载，并测试缺失模板阻止启动。

- [ ] **Step 2: 运行测试并确认失败**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter "SkillLibraryStartupTests|DynamicIconProfileTests" --no-restore`

Expected: FAIL。

- [ ] **Step 3: 实现启动编排并在 MainWindow 加载技能库**

构造技能库路径：

```csharp
var appData = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "GameMacro");
_store = new JsonProfileStore(Path.Combine(appData, "Profiles"));
_skillLibraryStore = new JsonSkillLibraryStore(Path.Combine(appData, "SkillLibrary.json"));
```

加载顺序为技能库、方案、迁移与保存，最后绑定界面。加载失败时显示错误并禁止启动识别，不静默创建空库覆盖数据。

- [ ] **Step 4: 用现有截图结果构建技能模板**

`SkillTemplateFactory.FromCapturedRegion(CapturedRegion, Guid categoryId)` 复用 `IconVisualSignature`、`PixelIconTemplateBuilder` 和 `.18` 默认阈值生成单模板；`FromSegmentedIcons(IReadOnlyList<SegmentedIcon>, Guid categoryId)` 批量生成模板。两者不设置名称和按键。先写 `SkillTemplateFactoryTests`，确认类别 ID、预览、签名和像素模板完整，再实现并运行：

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter SkillTemplateFactoryTests --no-restore`

Expected: PASS。

- [ ] **Step 5: 建立可收起技能库侧栏**

`SkillLibraryPanel` 顶部包含职业选择与“新建/重命名/删除”，主体使用 `ItemsControl + WrapPanel` 自适应显示图标，底部包含“扫描技能来源”和“框选单个技能”。图标处理：

```csharp
private void Icon_MouseMove(object sender, MouseEventArgs e)
{
    if (e.LeftButton != MouseButtonState.Pressed ||
        sender is not FrameworkElement { DataContext: SkillTemplateCard card }) return;
    DragDrop.DoDragDrop(this,
        new DataObject(SkillTemplateDragData.Format, card.TemplateId),
        DragDropEffects.Copy);
}
```

双击发布 `TemplateAddRequested`。侧栏不直接修改方案，由 MainWindow 调用 `ProfileSkillEditor`。

- [ ] **Step 6: 调整主窗口映射区与拖放处理**

主映射区设置 `AllowDrop="True"`，在 `Drop` 中读取模板 ID 并添加。模板卡片通过库查找预览；缺失模板显示“模板缺失”。移除按钮只删方案引用。原区域设置仅保留动态监控框选与“技能库”开关，扫描入口由侧栏触发并将扫描结果写入当前职业。

- [ ] **Step 7: 接入导入导出和运行时解析**

导出调用 `ProfileTransfer.Serialize(_profile, _skillLibrary)`。导入先合并并保存技能库，再保存新方案。启动和每次识别前使用 `SkillMappingResolver.Resolve`；缺失模板时拒绝启动，识别器收到解析后的完整模板列表。保存方案只保存引用映射。

- [ ] **Step 8: 运行 UI 周边、启动与识别回归测试**

Run: `dotnet test tests/GameMacro.App.Tests/GameMacro.App.Tests.csproj --filter "SkillLibraryStartupTests|ProfileSkillEditorTests|ProfileTransferTests|DynamicIconRecognizerTests|JsonProfileStoreTests" --no-restore`

Expected: PASS。

### Task 7: 文档、全量验证与 v1.0.1 安装包

**Files:**
- Modify: `README.md`
- Verify: `GameMacro.sln`
- Generate: `artifacts/installer/GameMacro-Setup.exe`

**Interfaces:**
- Consumes: 全部前置任务。
- Produces: 可分发的“按键助手”v1.0.1 安装包。

- [ ] **Step 1: 更新 README 使用流程**

说明职业分类、技能库扫描、拖拽/双击加入方案、多个视觉模板绑定同一按键、删除保护、旧方案自动迁移、方案导入导出携带模板，以及配置文件路径。保留内部文件名和安装路径说明。

- [ ] **Step 2: 运行完整测试套件**

Run: `dotnet test GameMacro.sln --no-restore`

Expected: Core 与 App 测试全部 PASS，失败数为 0。

- [ ] **Step 3: 运行 Release 编译**

Run: `dotnet build GameMacro.sln -c Release --no-restore`

Expected: exit code 0，错误 0。

- [ ] **Step 4: 生成 v1.0.1 安装包**

Run: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-installer.ps1 -Version 1.0.1`

Expected: Inno Setup 输出 `Successful compile`，生成 `artifacts\installer\GameMacro-Setup.exe`。

- [ ] **Step 5: 安全清理发布暂存目录并校验安装包**

解析 `artifacts\win-x64-installer-source` 的绝对路径，确认它位于工作区根目录内后递归删除。随后运行：

```powershell
Get-Item artifacts\installer\GameMacro-Setup.exe
Get-FileHash -Algorithm SHA256 artifacts\installer\GameMacro-Setup.exe
```

Expected: 安装包存在，记录文件大小、修改时间和 SHA-256。
