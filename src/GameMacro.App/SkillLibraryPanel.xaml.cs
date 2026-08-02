using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GameMacro.App.Services;
using GameMacro.App.ViewModels;
using GameMacro.Core.Models;

namespace GameMacro.App;

public sealed class SkillTemplateEventArgs(Guid templateId) : EventArgs
{
    public Guid TemplateId { get; } = templateId;
}

public partial class SkillLibraryPanel : UserControl
{
    private readonly ObservableCollection<SkillTemplateCard> _cards = [];
    private SkillLibrary? _library;
    private HashSet<Guid> _addedTemplateIds = [];

    public event EventHandler? BatchScanRequested;
    public event EventHandler? SingleCaptureRequested;
    public event EventHandler? LibraryChanged;
    public event EventHandler<SkillTemplateEventArgs>? TemplateAddRequested;
    public event EventHandler<SkillTemplateEventArgs>? TemplateDeleteRequested;

    public Guid SelectedCategoryId
        => CategoryCombo.SelectedItem is SkillCategory category ? category.Id : Guid.Empty;

    public SkillLibraryPanel()
    {
        InitializeComponent();
        TemplatesList.ItemsSource = _cards;
    }

    public void SetState(SkillLibrary library, IEnumerable<Guid> addedTemplateIds)
    {
        _library = library;
        _addedTemplateIds = addedTemplateIds.ToHashSet();
        var previous = SelectedCategoryId;
        CategoryCombo.ItemsSource = library.Categories;
        CategoryCombo.SelectedItem = library.Categories.FirstOrDefault(item => item.Id == previous)
            ?? library.Categories.FirstOrDefault(item => item.Name == SkillLibraryCatalog.UncategorizedName)
            ?? library.Categories.FirstOrDefault();
        RefreshCards();
    }

    private void RefreshCards()
    {
        _cards.Clear();
        if (_library is null || SelectedCategoryId == Guid.Empty) return;
        foreach (var template in _library.Templates.Where(item => item.CategoryId == SelectedCategoryId))
            _cards.Add(new SkillTemplateCard
            {
                TemplateId = template.Id,
                PreviewPng = template.PreviewPng,
                IsAdded = _addedTemplateIds.Contains(template.Id)
            });
    }

    private void CategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshCards();
    private void BatchScan_Click(object sender, RoutedEventArgs e) => BatchScanRequested?.Invoke(this, EventArgs.Empty);
    private void SingleCapture_Click(object sender, RoutedEventArgs e) => SingleCaptureRequested?.Invoke(this, EventArgs.Empty);

    private void Template_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed ||
            sender is not FrameworkElement { DataContext: SkillTemplateCard card }) return;
        DragDrop.DoDragDrop(this,
            new DataObject(SkillTemplateDragData.Format, card.TemplateId),
            DragDropEffects.Copy);
    }

    private void Template_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && sender is FrameworkElement { DataContext: SkillTemplateCard card })
        {
            TemplateAddRequested?.Invoke(this, new SkillTemplateEventArgs(card.TemplateId));
            e.Handled = true;
        }
    }

    private void DeleteTemplate_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is Button { DataContext: SkillTemplateCard card })
            TemplateDeleteRequested?.Invoke(this, new SkillTemplateEventArgs(card.TemplateId));
    }

    private void NewCategory_Click(object sender, RoutedEventArgs e)
    {
        if (_library is null) return;
        var dialog = new CategoryNameDialog { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var category = new SkillLibraryCatalog(_library).CreateCategory(dialog.CategoryName);
            CategoryCombo.Items.Refresh();
            CategoryCombo.SelectedItem = category;
            LibraryChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception)
        {
            MessageBox.Show(Window.GetWindow(this), exception.Message, "无法新建职业", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RenameCategory_Click(object sender, RoutedEventArgs e)
    {
        if (_library is null || CategoryCombo.SelectedItem is not SkillCategory category) return;
        var dialog = new CategoryNameDialog(category.Name) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true) return;
        try
        {
            new SkillLibraryCatalog(_library).RenameCategory(category.Id, dialog.CategoryName);
            CategoryCombo.Items.Refresh();
            LibraryChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception)
        {
            MessageBox.Show(Window.GetWindow(this), exception.Message, "无法重命名职业", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void DeleteCategory_Click(object sender, RoutedEventArgs e)
    {
        if (_library is null || CategoryCombo.SelectedItem is not SkillCategory category) return;
        try
        {
            new SkillLibraryCatalog(_library).DeleteCategory(category.Id);
            CategoryCombo.Items.Refresh();
            CategoryCombo.SelectedIndex = 0;
            LibraryChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception)
        {
            MessageBox.Show(Window.GetWindow(this), exception.Message, "无法删除职业", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
