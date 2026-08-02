using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using GameMacro.App.Detection;
using GameMacro.App.Overlay;
using GameMacro.App.Platform;
using GameMacro.App.Services;
using GameMacro.App.ViewModels;
using GameMacro.Core.Models;

namespace GameMacro.App;

public partial class MainWindow : Window
{
    private const int ToggleHotkeyId = 0x4101;
    private const int WmHotkey = 0x0312;
    private readonly ObservableCollection<MacroProfile> _profiles = [];
    private readonly ObservableCollection<PendingIconMapping> _pendingMappings = [];
    private readonly ObservableCollection<string> _interruptKeys = [];
    private readonly WindowsWindowService _windows = new();
    private readonly SendInputService _input = new();
    private readonly DynamicIconRecognizer _recognizer = new();
    private readonly ManualInterruptGate _manualInterruptGate = new();
    private readonly ManualInterruptRouter _manualInterruptRouter;
    private readonly PhysicalKeyboardMonitor _physicalKeyboardMonitor = new();
    private readonly DispatcherTimer _automationTimer = new();
    private readonly DispatcherTimer _overlayTimer = new() { Interval = TimeSpan.FromMilliseconds(150) };
    private readonly WindowsSkillCaptureService _capture;
    private readonly JsonProfileStore _store;
    private readonly JsonSkillLibraryStore _skillLibraryStore;
    private SkillLibrary _skillLibrary = new();
    private IReadOnlyList<IconKeyMapping> _resolvedMappings = [];
    private bool _skillLibraryAvailable;
    private MacroProfile? _profile;
    private HwndSource? _source;
    private bool _tickInProgress;
    private PendingIconMapping? _awaitingKey;
    private bool _awaitingToggleHotkey;
    private bool _awaitingInterruptKey;
    private GameOverlayWindow? _overlayWindow;
    private bool _overlayTickInProgress;

    public IReadOnlyList<string> AvailableKeys => InputKeyOptions.All;

    public MainWindow()
    {
        InitializeComponent();
        MappingsList.ItemsSource = _pendingMappings;
        InterruptKeysList.ItemsSource = _interruptKeys;
        _manualInterruptRouter = new(_manualInterruptGate);
        _physicalKeyboardMonitor.KeyChanged += PhysicalKeyboardMonitor_KeyChanged;
        _capture = new(_windows);
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GameMacro");
        _store = new(Path.Combine(appDataPath, "Profiles"));
        _skillLibraryStore = new(Path.Combine(appDataPath, "SkillLibrary.json"));
        ProfilesList.ItemsSource = _profiles;
        LibraryPanel.CloseRequested += (_, _) => LibraryPanel.Visibility = Visibility.Collapsed;
        LibraryPanel.BatchScanRequested += (_, _) => ScanSource_Click(LibraryPanel, new RoutedEventArgs());
        LibraryPanel.SingleCaptureRequested += (_, _) => CaptureSingleIcon_Click(LibraryPanel, new RoutedEventArgs());
        LibraryPanel.LibraryChanged += LibraryPanel_LibraryChanged;
        LibraryPanel.TemplateAddRequested += LibraryPanel_TemplateAddRequested;
        LibraryPanel.TemplateDeleteRequested += LibraryPanel_TemplateDeleteRequested;
        _automationTimer.Tick += async (_, _) => await AutomationTickAsync();
        _overlayTimer.Tick += async (_, _) => await OverlayTickAsync();
        PreviewKeyDown += MainWindow_PreviewKeyDown;
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_physicalKeyboardMonitor.Start())
            StatusText.Text = "状态：物理键盘监听启动失败，无法使用优先打断键";
        var loadedProfiles = (await _store.LoadAllAsync(CancellationToken.None)).ToList();
        try
        {
            var startup = await SkillLibraryStartup.LoadAndMigrateAsync(
                _skillLibraryStore,
                loadedProfiles,
                _store,
                CancellationToken.None);
            _skillLibrary = startup.Library;
            _skillLibraryAvailable = true;
        }
        catch (Exception exception)
        {
            _skillLibrary = new SkillLibrary();
            new SkillLibraryCatalog(_skillLibrary).EnsureUncategorized();
            LibraryPanel.IsEnabled = false;
            MessageBox.Show(this,
                $"技能库加载失败，为防止覆盖原数据，本次不能编辑技能库或启动识别。\n\n{exception.Message}",
                "技能库不可用",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        foreach (var profile in loadedProfiles) _profiles.Add(profile);
        if (_profiles.Count == 0) _profiles.Add(CreateDefaultProfile());
        _overlayWindow = new GameOverlayWindow();
        _overlayWindow.ProfileSelectionRequested += OverlayWindow_ProfileSelectionRequested;
        _overlayWindow.ToggleRequested += OverlayWindow_ToggleRequested;
        _overlayWindow.OverlayMoved += OverlayWindow_OverlayMoved;
        RefreshWindows();
        ProfilesList.SelectedIndex = 0;
        var handle = new WindowInteropHelper(this).Handle;
        _source = HwndSource.FromHwnd(handle);
        _source.AddHook(WindowProc);
        RegisterProfileHotkey();
        _overlayTimer.Start();
    }

    private static MacroProfile CreateDefaultProfile() => new()
    {
        Version = 3,
        Name = "按键方案",
        ToggleHotkey = "F8",
        ScanIntervalMs = 20
    };

    private void ProfilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_automationTimer.IsEnabled) StopMonitoring();
        _manualInterruptGate.Reset();
        _awaitingKey = null;
        _awaitingToggleHotkey = false;
        _awaitingInterruptKey = false;
        _profile = ProfilesList.SelectedItem as MacroProfile;
        if (_profile is null) return;
        _recognizer.Reset();
        _resolvedMappings = [];
        ProfileNameBox.Text = _profile.Name;
        ScanIntervalBox.Text = _profile.ScanIntervalMs.ToString();
        ShowOverlayCheckBox.IsChecked = _profile.ShowGameOverlay;
        ReplacePending(BatchMappingBuilder.FromMappings(_profile.IconMappings, _skillLibrary));
        ReplaceInterruptKeys(_profile.InterruptKeys);
        SelectConfiguredWindow();
        RefreshPreviews();
        RefreshLibraryPanel();
        UpdateHotkeyLabels();
        UpdateOverlayState();
        if (IsLoaded) RegisterProfileHotkey();
    }

    private async void ScanSource_Click(object sender, RoutedEventArgs e)
    {
        if (_profile is null || !EnsureSkillLibraryAvailable()) return;
        try
        {
            var handle = RequireTargetWindow();
            var bounds = _capture.GetClientScreenBounds(_profile);
            Hide();
            await Task.Delay(150);
            var selector = new RegionSelectionWindow(bounds.X, bounds.Y, bounds.Width, bounds.Height, NativeMethods.GetDpiForWindow(handle));
            if (selector.ShowDialog() != true || selector.SelectedRegion is not { } region) return;
            await Task.Delay(100);
            var captured = _capture.CaptureRegion(_profile, region);
            var result = SkillIconSegmenter.Segment(captured.Pixels, captured.Width, captured.Height);
            if (result.Icons.Count == 0)
            {
                MessageBox.Show(this, "没有检测到有效技能图标。请框得更贴近技能图标，或使用技能库中的“框选单个技能”。",
                    "扫描结果为空", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var categoryId = RequireLibraryCategory();
            var candidates = SkillTemplateFactory.FromDetectedIcons(result.Icons, categoryId);
            var added = new SkillLibraryCatalog(_skillLibrary).AddTemplates(categoryId, candidates);
            await _skillLibraryStore.SaveAsync(_skillLibrary, CancellationToken.None);
            RefreshLibraryPanel();
            SourceStatusText.Text = $"技能库新增 {added.Added.Count} 个，复用重复模板 {added.Reused.Count} 个；过滤空槽 {result.EmptyFilteredCount} 个。";
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "扫描技能失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            Show();
            Activate();
            RefreshPreviews();
        }
    }

    private async void CaptureSingleIcon_Click(object sender, RoutedEventArgs e)
    {
        if (_profile is null || !EnsureSkillLibraryAvailable()) return;
        try
        {
            var handle = RequireTargetWindow();
            var bounds = _capture.GetClientScreenBounds(_profile);
            Hide();
            await Task.Delay(150);
            var selector = new RegionSelectionWindow(
                bounds.X,
                bounds.Y,
                bounds.Width,
                bounds.Height,
                NativeMethods.GetDpiForWindow(handle));
            if (selector.ShowDialog() != true || selector.SelectedRegion is not { } region) return;
            await Task.Delay(100);
            var categoryId = RequireLibraryCategory();
            var template = SkillTemplateFactory.FromCapturedRegion(
                _capture.CaptureRegion(_profile, region),
                categoryId);
            var result = new SkillLibraryCatalog(_skillLibrary).AddTemplates(categoryId, [template]);
            await _skillLibraryStore.SaveAsync(_skillLibrary, CancellationToken.None);
            RefreshLibraryPanel();
            SourceStatusText.Text = result.Added.Count == 1
                ? "已把单个技能图标加入技能库。"
                : "该技能图标已存在，已复用技能库中的模板。";
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "框选单个技能失败",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            Show();
            Activate();
            RefreshPreviews();
        }
    }

    private void DeletePendingMapping_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is not Button { DataContext: PendingIconMapping mapping }) return;
        _pendingMappings.Remove(mapping);
        PendingStatusText.Text = _pendingMappings.Count == 0 ? "尚无待绑定图标" : $"待绑定 {_pendingMappings.Count} 个技能";
        RefreshLibraryPanel();
    }

    private void ToggleSkillLibrary_Click(object sender, RoutedEventArgs e)
    {
        LibraryPanel.Visibility = LibraryPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
        RefreshLibraryPanel();
    }

    private void MappingsList_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(SkillTemplateDragData.Format)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void MappingsList_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(SkillTemplateDragData.Format) is Guid templateId)
            AddTemplateToCurrentProfile(templateId);
        e.Handled = true;
    }

    private void LibraryPanel_TemplateAddRequested(object? sender, SkillTemplateEventArgs e)
        => AddTemplateToCurrentProfile(e.TemplateId);

    private async void LibraryPanel_TemplateDeleteRequested(object? sender, SkillTemplateEventArgs e)
    {
        if (!EnsureSkillLibraryAvailable()) return;
        try
        {
            if (_pendingMappings.Any(item => item.SkillTemplateId == e.TemplateId))
                throw new InvalidOperationException("当前方案正在使用该技能模板，请先从方案映射中删除。 ");
            new SkillLibraryCatalog(_skillLibrary).DeleteTemplate(e.TemplateId, _profiles);
            await _skillLibraryStore.SaveAsync(_skillLibrary, CancellationToken.None);
            RefreshLibraryPanel();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "无法删除技能模板", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void LibraryPanel_LibraryChanged(object? sender, EventArgs e)
    {
        if (!EnsureSkillLibraryAvailable()) return;
        try
        {
            await _skillLibraryStore.SaveAsync(_skillLibrary, CancellationToken.None);
            RefreshLibraryPanel();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "无法保存技能库", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void AddTemplateToCurrentProfile(Guid templateId)
    {
        if (_profile is null || !EnsureSkillLibraryAvailable()) return;
        if (_pendingMappings.Any(item => item.SkillTemplateId == templateId))
        {
            PendingStatusText.Text = "当前方案已经添加了这个模板；不同图标模板仍可绑定相同按键。";
            return;
        }
        var template = _skillLibrary.Templates.FirstOrDefault(item => item.Id == templateId);
        if (template is null)
        {
            PendingStatusText.Text = "技能模板不存在，请刷新技能库。";
            return;
        }
        _pendingMappings.Add(new PendingIconMapping
        {
            SkillTemplateId = template.Id,
            PreviewPng = template.PreviewPng,
            Signature = template.Signature.ToArray(),
            MatchThreshold = template.MatchThreshold,
            PixelTemplateData = template.PixelTemplateData.ToArray()
        });
        PendingStatusText.Text = $"已添加技能模板，当前 {_pendingMappings.Count} 个；请点击卡片设置按键。";
        RefreshLibraryPanel();
    }

    private void RefreshLibraryPanel()
        => LibraryPanel.SetState(
            _skillLibrary,
            _pendingMappings.Where(item => item.SkillTemplateId != Guid.Empty)
                .Select(item => item.SkillTemplateId));

    private bool EnsureSkillLibraryAvailable()
    {
        if (_skillLibraryAvailable) return true;
        MessageBox.Show(this, "技能库当前不可用。请先修复或恢复技能库文件后重新启动程序。",
            "技能库不可用", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    private Guid RequireLibraryCategory()
    {
        var categoryId = LibraryPanel.SelectedCategoryId;
        if (categoryId != Guid.Empty) return categoryId;
        return new SkillLibraryCatalog(_skillLibrary).EnsureUncategorized().Id;
    }

    private void AssignKey_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PendingIconMapping mapping }) return;
        _awaitingToggleHotkey = false;
        _awaitingInterruptKey = false;
        RegisterProfileHotkey();
        _awaitingKey = mapping;
        PendingStatusText.Text = "请直接按下要绑定的键（F1-F12、数字、字母或 ~）";
        Activate();
        Focus();
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_awaitingKey is null && !_awaitingToggleHotkey && !_awaitingInterruptKey) return;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            _awaitingKey = null;
            _awaitingToggleHotkey = false;
            _awaitingInterruptKey = false;
            PendingStatusText.Text = "已取消按键设置";
            UpdateHotkeyLabels();
            RegisterProfileHotkey();
            e.Handled = true;
            return;
        }
        var name = WpfKeyName.FromKey(key);
        if (name is null) return;
        if (_awaitingToggleHotkey && _profile is not null)
        {
            _profile.ToggleHotkey = name;
            _awaitingToggleHotkey = false;
            UpdateHotkeyLabels();
            UpdateOverlayState();
            RegisterProfileHotkey();
            StatusText.Text = $"状态：启停热键已设为 {name}，保存方案后生效";
            e.Handled = true;
            return;
        }
        if (_awaitingInterruptKey && _profile is not null)
        {
            if (!InterruptKeyBindingEditor.TryAdd(_interruptKeys, name, _profile.ToggleHotkey, out var error))
            {
                StatusText.Text = $"状态：{error} 请按其他按键，Esc 取消";
                e.Handled = true;
                return;
            }
            _awaitingInterruptKey = false;
            RegisterProfileHotkey();
            StatusText.Text = $"状态：已添加优先打断键 {name}，保存方案后生效";
            e.Handled = true;
            return;
        }
        if (_awaitingKey is null) return;
        _awaitingKey.ActionKey = name;
        _awaitingKey = null;
        MappingsList.Items.Refresh();
        PendingStatusText.Text = $"已绑定 {name}，可继续点击其他卡片设置";
        e.Handled = true;
    }

    private async void SaveAllMappings_Click(object sender, RoutedEventArgs e)
    {
        if (_profile is null) return;
        try
        {
            var mappings = BatchMappingBuilder.Save(_pendingMappings);
            ApplyProfileFields();
            var oldMappings = _profile.IconMappings;
            _profile.IconMappings = mappings;
            _recognizer.Reset();
            _resolvedMappings = [];
            var resolution = SkillMappingResolver.Resolve(_profile, _skillLibrary);
            var errors = ProfileInputValidator.Validate(_profile, resolution.MissingTemplateIds);
            if (errors.Count > 0)
            {
                _profile.IconMappings = oldMappings;
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
            }
            await _store.SaveAsync(_profile, CancellationToken.None);
            _resolvedMappings = resolution.Mappings;
            RegisterProfileHotkey();
            RefreshLibraryPanel();
            PendingStatusText.Text = $"已保存 {_profile.IconMappings.Count} 个图标按键映射";
            StatusText.Text = "状态：全部映射已保存";
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "无法保存映射", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ReplacePending(IEnumerable<PendingIconMapping> items)
    {
        _pendingMappings.Clear();
        foreach (var item in items) _pendingMappings.Add(item);
        PendingStatusText.Text = _pendingMappings.Count == 0 ? "尚无待绑定图标" : $"当前 {_pendingMappings.Count} 个图标映射";
        RefreshLibraryPanel();
    }

    private void ReplaceInterruptKeys(IEnumerable<string>? keys)
    {
        _interruptKeys.Clear();
        foreach (var key in keys ?? [])
        {
            if (!_interruptKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
                _interruptKeys.Add(key);
        }
    }

    private async void SelectRegion_Click(object sender, RoutedEventArgs e)
    {
        if (_profile is null) return;
        try
        {
            var handle = RequireTargetWindow();
            var bounds = _capture.GetClientScreenBounds(_profile);
            Hide();
            await Task.Delay(150);
            var selector = new RegionSelectionWindow(bounds.X, bounds.Y, bounds.Width, bounds.Height, NativeMethods.GetDpiForWindow(handle));
            if (selector.ShowDialog() == true && selector.SelectedRegion is { } region)
            {
                _profile.DetectionX = region.X;
                _profile.DetectionY = region.Y;
                _profile.DetectionWidth = region.Width;
                _profile.DetectionHeight = region.Height;
                await Task.Delay(100);
                _profile.DetectionPreviewPng = _capture.CaptureRegion(_profile).PreviewPng;
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "框选失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            Show();
            Activate();
            RefreshPreviews();
        }
    }

    private nint RequireTargetWindow()
    {
        if (_profile is null) throw new InvalidOperationException("没有选中的方案。");
        var handle = _windows.FindWindow(_profile);
        return handle == 0 ? throw new InvalidOperationException("请先选择有效的游戏窗口。") : handle;
    }

    private void RefreshPreviews()
    {
        if (_profile is null) return;
        RegionPreview.Source = PngPreviewCodec.Decode(_profile.DetectionPreviewPng);
        RegionStatusText.Text = _profile.HasDetectionRegion
            ? $"已框选：X {_profile.DetectionX:0.000}，Y {_profile.DetectionY:0.000}，宽 {_profile.DetectionWidth:0.000}，高 {_profile.DetectionHeight:0.000}"
            : "尚未框选动态技能窗口";
        SourceStatusText.Text = _skillLibraryAvailable
            ? $"技能库：{_skillLibrary.Categories.Count} 个职业分类，{_skillLibrary.Templates.Count} 个图标模板。"
            : "技能库不可用，识别已禁用。";
    }

    private void RefreshWindows_Click(object sender, RoutedEventArgs e) => RefreshWindows();

    private void RefreshWindows()
    {
        WindowCombo.ItemsSource = _windows.ListWindows();
        SelectConfiguredWindow();
    }

    private void SelectConfiguredWindow()
    {
        if (_profile is null || WindowCombo.ItemsSource is null) return;
        WindowCombo.SelectedItem = WindowCombo.Items.Cast<WindowInfo>().FirstOrDefault(item =>
            (!string.IsNullOrWhiteSpace(_profile.TargetProcessName)
                && string.Equals(item.ProcessName, _profile.TargetProcessName, StringComparison.OrdinalIgnoreCase))
            || string.Equals(item.Title.Trim(), _profile.TargetWindowTitle.Trim(), StringComparison.CurrentCulture));
    }

    private void WindowCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_profile is not null && WindowCombo.SelectedItem is WindowInfo window)
        {
            _profile.TargetWindowTitle = window.Title;
            _profile.TargetProcessName = window.ProcessName;
        }
    }

    private async void SaveProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_profile is null) return;
        try
        {
            if (!_physicalKeyboardMonitor.IsRunning)
                throw new InvalidOperationException("物理键盘监听未启动，无法保证优先打断键生效。");
            ApplyProfileFields();
            _profile.IconMappings = BatchMappingBuilder.Save(_pendingMappings);
            var resolution = SkillMappingResolver.Resolve(_profile, _skillLibrary);
            var errors = ProfileInputValidator.Validate(_profile, resolution.MissingTemplateIds);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
            await _store.SaveAsync(_profile, CancellationToken.None);
            _resolvedMappings = resolution.Mappings;
            RegisterProfileHotkey();
            ProfilesList.Items.Refresh();
            UpdateHotkeyLabels();
            StatusText.Text = "状态：方案已保存";
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "无法保存", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void ExportProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_profile is null) return;
        try
        {
            ApplyProfileFields();
            _profile.IconMappings = BatchMappingBuilder.Save(_pendingMappings);
            var resolution = SkillMappingResolver.Resolve(_profile, _skillLibrary);
            var errors = ProfileInputValidator.Validate(_profile, resolution.MissingTemplateIds);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
            var dialog = new SaveFileDialog
            {
                Title = "导出当前方案",
                Filter = "方案配置 (*.json)|*.json|所有文件 (*.*)|*.*",
                DefaultExt = ".json",
                AddExtension = true,
                FileName = SafeFileName(_profile.Name) + ".json"
            };
            if (dialog.ShowDialog(this) != true) return;
            await File.WriteAllTextAsync(dialog.FileName, ProfileTransfer.Serialize(_profile, _skillLibrary));
            StatusText.Text = $"状态：方案已导出到 {dialog.FileName}";
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "无法导出方案", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void ImportProfile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = "导入方案",
                Filter = "方案配置 (*.json)|*.json|所有文件 (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };
            if (dialog.ShowDialog(this) != true) return;
            if (!EnsureSkillLibraryAvailable()) return;
            var import = ProfileTransfer.ImportAsCopy(await File.ReadAllTextAsync(dialog.FileName), _skillLibrary);
            var imported = import.Profile;
            var resolution = SkillMappingResolver.Resolve(imported, _skillLibrary);
            var errors = ProfileInputValidator.Validate(imported, resolution.MissingTemplateIds);
            if (errors.Count > 0) throw new InvalidDataException(string.Join(Environment.NewLine, errors));
            if (import.LibraryChanged)
                await _skillLibraryStore.SaveAsync(_skillLibrary, CancellationToken.None);
            await _store.SaveAsync(imported, CancellationToken.None);
            _profiles.Add(imported);
            ProfilesList.SelectedItem = imported;
            RefreshLibraryPanel();
            StatusText.Text = $"状态：已导入方案 {imported.Name}";
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "无法导入方案", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static string SafeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(name.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "方案" : safe;
    }

    private void ApplyProfileFields()
    {
        if (_profile is null) return;
        _profile.Name = string.IsNullOrWhiteSpace(ProfileNameBox.Text) ? "按键方案" : ProfileNameBox.Text.Trim();
        if (WindowCombo.SelectedItem is WindowInfo window)
        {
            _profile.TargetWindowTitle = window.Title;
            _profile.TargetProcessName = window.ProcessName;
        }
        _profile.ShowGameOverlay = ShowOverlayCheckBox.IsChecked == true;
        _profile.InterruptKeys = _interruptKeys.ToList();
        if (!int.TryParse(ScanIntervalBox.Text, out var interval)) throw new InvalidOperationException("扫描间隔必须是整数。");
        _profile.ScanIntervalMs = interval;
    }

    private void NewProfile_Click(object sender, RoutedEventArgs e)
    {
        var profile = CreateDefaultProfile();
        profile.Id = Guid.NewGuid();
        profile.Name = "新方案";
        _profiles.Add(profile);
        ProfilesList.SelectedItem = profile;
    }

    private async void DeleteProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_profile is null) return;
        if (MessageBox.Show(this, $"确定删除方案“{_profile.Name}”？", "删除方案", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        StopMonitoring();
        await _store.DeleteAsync(_profile.Id, CancellationToken.None);
        _profiles.Remove(_profile);
        if (_profiles.Count == 0) _profiles.Add(CreateDefaultProfile());
        ProfilesList.SelectedIndex = 0;
    }

    private void RegisterProfileHotkey()
    {
        if (_profile is null) return;
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == 0) return;
        NativeMethods.UnregisterHotKey(handle, ToggleHotkeyId);
        var key = VirtualKeyParser.Parse(_profile.ToggleHotkey);
        if (key is null || !NativeMethods.RegisterHotKey(handle, ToggleHotkeyId, 0, key.Value))
        {
            StatusText.Text = "状态：启动热键注册失败";
            return;
        }
    }

    private void UpdateHotkeyLabels()
    {
        if (_profile is null) return;
        ToggleHotkeyButton.Content = _awaitingToggleHotkey
            ? "请按下按键…"
            : $"按键 {_profile.ToggleHotkey}";
    }

    private void ToggleHotkeyCapture_Click(object sender, RoutedEventArgs e)
    {
        if (_profile is null) return;
        _awaitingKey = null;
        _awaitingInterruptKey = false;
        _awaitingToggleHotkey = true;
        var handle = new WindowInteropHelper(this).Handle;
        if (handle != 0) NativeMethods.UnregisterHotKey(handle, ToggleHotkeyId);
        UpdateHotkeyLabels();
        StatusText.Text = "状态：请直接按下新的启动/停止热键，Esc 取消";
        Activate();
        Focus();
    }

    private void AddInterruptKey_Click(object sender, RoutedEventArgs e)
    {
        if (_profile is null) return;
        _awaitingKey = null;
        _awaitingToggleHotkey = false;
        _awaitingInterruptKey = true;
        var handle = new WindowInteropHelper(this).Handle;
        if (handle != 0) NativeMethods.UnregisterHotKey(handle, ToggleHotkeyId);
        UpdateHotkeyLabels();
        StatusText.Text = "状态：请直接按下新的优先打断键，Esc 取消";
        Activate();
        Focus();
    }

    private void DeleteInterruptKey_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is not Button { DataContext: string key }) return;
        _interruptKeys.Remove(key);
        StatusText.Text = $"状态：已删除优先打断键 {key}，保存方案后生效";
    }

    private void PhysicalKeyboardMonitor_KeyChanged(object? sender, PhysicalKeyboardEventArgs e)
    {
        if (_profile is null) return;
        var configuredKeys = _profile.InterruptKeys
            .Select(VirtualKeyParser.Parse)
            .Where(key => key.HasValue)
            .Select(key => key!.Value)
            .ToHashSet();
        var targetForeground = e.IsDown
            && _automationTimer.IsEnabled
            && configuredKeys.Contains(e.VirtualKey)
            && _windows.IsTargetForeground(_profile);
        _manualInterruptRouter.Handle(
            e,
            _automationTimer.IsEnabled,
            targetForeground,
            configuredKeys);
    }

    private void ToggleMonitoring()
    {
        if (_automationTimer.IsEnabled)
        {
            StopMonitoring();
            return;
        }
        if (_profile is null || string.IsNullOrWhiteSpace(_profile.TargetWindowTitle))
        {
            MessageBox.Show(this, "请先选择目标游戏窗口。", "无法启动", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            if (!EnsureSkillLibraryAvailable()) return;
            ApplyProfileFields();
            _profile.IconMappings = BatchMappingBuilder.Save(_pendingMappings);
            var resolution = SkillMappingResolver.Resolve(_profile, _skillLibrary);
            var errors = ProfileInputValidator.Validate(_profile, resolution.MissingTemplateIds);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
            _resolvedMappings = resolution.Mappings;
            RegisterProfileHotkey();
            _automationTimer.Interval = TimeSpan.FromMilliseconds(_profile.ScanIntervalMs);
            _recognizer.Reset();
            _manualInterruptGate.Reset();
            _automationTimer.Start();
            UpdateHotkeyLabels();
            UpdateOverlayState();
            StatusText.Text = "状态：识别运行中，请切回游戏";
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "无法启动", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void StopMonitoring()
    {
        _automationTimer.Stop();
        _recognizer.Reset();
        _resolvedMappings = [];
        _manualInterruptGate.Reset();
        UpdateHotkeyLabels();
        UpdateOverlayState();
        StatusText.Text = "状态：已停止";
    }

    private async Task AutomationTickAsync()
    {
        if (_tickInProgress || _profile is null) return;
        if (_manualInterruptGate.IsPaused)
        {
            StatusText.Text = "状态：手动优先，最后松键后 1 秒恢复";
            return;
        }
        _tickInProgress = true;
        try
        {
            var targetHandle = _windows.FindWindow(_profile);
            if (targetHandle == 0)
            {
                StatusText.Text = "状态：目标游戏进程未找到，请重新选择目标窗口";
                return;
            }
            if (!await _windows.IsTargetForegroundAsync(_profile, CancellationToken.None))
            {
                StatusText.Text = "状态：目标游戏不在前台";
                return;
            }
            var sample = _capture.CaptureRegionTemplate(_profile);
            var match = _recognizer.Match(sample, _resolvedMappings);
            if (match is null)
            {
                StatusText.Text = "状态：未识别到已绑定图标";
                return;
            }
            if (_manualInterruptGate.IsPaused)
            {
                StatusText.Text = "状态：手动优先，最后松键后 1 秒恢复";
                return;
            }
            await _input.EnqueueAsync(match.Mapping.ActionKey, CancellationToken.None);
            StatusText.Text = $"状态：匹配 {match.Mapping.ActionKey}，像素距离 {match.Distance:0.000}，持续发送中";
        }
        catch (Exception exception)
        {
            StopMonitoring();
            StatusText.Text = $"状态：识别已停止 - {exception.Message}";
        }
        finally
        {
            _tickInProgress = false;
        }
    }

    private void ShowOverlayCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_profile is null) return;
        _profile.ShowGameOverlay = ShowOverlayCheckBox.IsChecked == true;
        if (!_profile.ShowGameOverlay) _overlayWindow?.Hide();
        UpdateOverlayState();
    }

    private void OverlayWindow_ProfileSelectionRequested(object? sender, MacroProfile profile)
    {
        if (_automationTimer.IsEnabled || !_profiles.Contains(profile))
        {
            UpdateOverlayState();
            return;
        }

        ProfilesList.SelectedItem = profile;
        _windows.ActivateTarget(profile);
    }

    private void OverlayWindow_ToggleRequested(object? sender, EventArgs e)
    {
        ToggleMonitoring();
        if (_profile is not null) _windows.ActivateTarget(_profile);
    }

    private async void OverlayWindow_OverlayMoved(object? sender, EventArgs e)
    {
        if (_profile is null || _overlayWindow is null || _automationTimer.IsEnabled) return;
        try
        {
            var client = GetOverlayClientBounds(_profile);
            var normalized = OverlayPlacement.ToNormalized(
                _overlayWindow.Left,
                _overlayWindow.Top,
                client,
                OverlayWidth,
                OverlayHeight);
            _profile.OverlayLeft = normalized.Left;
            _profile.OverlayTop = normalized.Top;
            PositionOverlay(_profile);
            await _store.SaveAsync(_profile, CancellationToken.None);
        }
        catch (Exception exception)
        {
            StatusText.Text = $"状态：浮窗位置保存失败 - {exception.Message}";
        }
    }

    private async Task OverlayTickAsync()
    {
        if (_overlayTickInProgress || _overlayWindow is null) return;
        _overlayTickInProgress = true;
        try
        {
            if (_profile is null || !_profile.ShowGameOverlay)
            {
                _overlayWindow.Hide();
                return;
            }

            var targetForeground = await _windows.IsTargetForegroundAsync(_profile, CancellationToken.None);
            if (!targetForeground && !_overlayWindow.IsActive)
            {
                _overlayWindow.Hide();
                return;
            }

            UpdateOverlayState();
            if (!_overlayWindow.IsActive) PositionOverlay(_profile);
            if (!_overlayWindow.IsVisible) _overlayWindow.Show();
        }
        catch
        {
            _overlayWindow.Hide();
        }
        finally
        {
            _overlayTickInProgress = false;
        }
    }

    private void UpdateOverlayState()
    {
        if (_overlayWindow is null || _profile is null) return;
        var profiles = OverlayProfilePolicy.ProfilesForTarget(_profiles, _profile);
        _overlayWindow.UpdateState(profiles, _profile, _automationTimer.IsEnabled);
    }

    private void PositionOverlay(MacroProfile profile)
    {
        if (_overlayWindow is null) return;
        var client = GetOverlayClientBounds(profile);
        var point = OverlayPlacement.ToScreen(
            profile.OverlayLeft,
            profile.OverlayTop,
            client,
            OverlayWidth,
            OverlayHeight);
        _overlayWindow.Left = point.X;
        _overlayWindow.Top = point.Y;
    }

    private OverlayBounds GetOverlayClientBounds(MacroProfile profile)
    {
        var handle = _windows.FindWindow(profile);
        if (handle == 0) throw new InvalidOperationException("目标游戏窗口不可用。");
        var bounds = _capture.GetClientScreenBounds(profile);
        var dpi = NativeMethods.GetDpiForWindow(handle);
        var scale = dpi == 0 ? 1d : 96d / dpi;
        return new OverlayBounds(
            bounds.X * scale,
            bounds.Y * scale,
            bounds.Width * scale,
            bounds.Height * scale);
    }

    private double OverlayWidth => _overlayWindow?.ActualWidth > 0
        ? _overlayWindow.ActualWidth
        : _overlayWindow?.Width ?? OverlayPresentation.Width;

    private double OverlayHeight => _overlayWindow?.ActualHeight > 0
        ? _overlayWindow.ActualHeight
        : _overlayWindow?.Height ?? OverlayPresentation.Height;

    private nint WindowProc(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message != WmHotkey) return 0;
        handled = true;
        if (_awaitingToggleHotkey || _awaitingInterruptKey || _awaitingKey is not null) return 0;
        if (wParam == ToggleHotkeyId) ToggleMonitoring();
        return 0;
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        _overlayTimer.Stop();
        StopMonitoring();
        var handle = new WindowInteropHelper(this).Handle;
        NativeMethods.UnregisterHotKey(handle, ToggleHotkeyId);
        _source?.RemoveHook(WindowProc);
        _physicalKeyboardMonitor.KeyChanged -= PhysicalKeyboardMonitor_KeyChanged;
        _physicalKeyboardMonitor.Dispose();
        _overlayWindow?.Close();
    }
}
