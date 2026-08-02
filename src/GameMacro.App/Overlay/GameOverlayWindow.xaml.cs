using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GameMacro.Core.Models;

namespace GameMacro.App.Overlay;

public partial class GameOverlayWindow : Window
{
    private bool _updating;
    private bool _running;

    public event EventHandler<MacroProfile>? ProfileSelectionRequested;
    public event EventHandler? ToggleRequested;
    public event EventHandler? OverlayMoved;

    public GameOverlayWindow()
    {
        InitializeComponent();
    }

    public void UpdateState(
        IEnumerable<MacroProfile> profiles,
        MacroProfile current,
        bool running)
    {
        _updating = true;
        try
        {
            var available = profiles.ToList();
            ProfileSelector.ItemsSource = available;
            ProfileSelector.SelectedItem = available.FirstOrDefault(profile => profile.Id == current.Id);
            _running = running;
            ProfileSelector.IsEnabled = OverlayProfilePolicy.CanSwitch(running);
            ToggleButton.Content = OverlayPresentation.ToggleLabel(running, current.ToggleHotkey);
            ToggleButton.Background = new SolidColorBrush(
                running ? Color.FromRgb(174, 66, 66) : Color.FromRgb(76, 141, 255));
        }
        finally
        {
            _updating = false;
        }
    }

    private void ProfileSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || _running || ProfileSelector.SelectedItem is not MacroProfile profile) return;
        ProfileSelectionRequested?.Invoke(this, profile);
    }

    private void ToggleButton_Click(object sender, RoutedEventArgs e)
        => ToggleRequested?.Invoke(this, EventArgs.Empty);

    private void DragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_running || e.ChangedButton != MouseButton.Left) return;
        if (e.OriginalSource is DependencyObject source
            && FindAncestor<Button>(source) is not null) return;
        if (e.OriginalSource is DependencyObject comboSource
            && FindAncestor<ComboBox>(comboSource) is not null) return;

        try
        {
            DragMove();
            OverlayMoved?.Invoke(this, EventArgs.Empty);
        }
        catch (InvalidOperationException)
        {
            // DragMove can be interrupted if the mouse button is released immediately.
        }
    }

    private static T? FindAncestor<T>(DependencyObject? source) where T : DependencyObject
    {
        while (source is not null)
        {
            if (source is T match) return match;
            source = VisualTreeHelper.GetParent(source);
        }
        return null;
    }
}
