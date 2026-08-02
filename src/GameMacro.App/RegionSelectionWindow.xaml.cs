using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using GameMacro.App.Detection;

namespace GameMacro.App;

public partial class RegionSelectionWindow : Window
{
    private Point _start;
    private bool _dragging;
    public NormalizedRegion? SelectedRegion { get; private set; }

    public RegionSelectionWindow(int screenX, int screenY, int pixelWidth, int pixelHeight, uint dpi)
    {
        InitializeComponent();
        var scale = 96d / Math.Max(96, dpi);
        Left = screenX * scale;
        Top = screenY * scale;
        Width = pixelWidth * scale;
        Height = pixelHeight * scale;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _start = e.GetPosition(SelectionCanvas);
        _dragging = true;
        SelectionRectangle.Visibility = Visibility.Visible;
        CaptureMouse();
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        UpdateRectangle(e.GetPosition(SelectionCanvas));
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;
        ReleaseMouseCapture();
        var end = e.GetPosition(SelectionCanvas);
        var x = Math.Max(0, Math.Min(_start.X, end.X));
        var y = Math.Max(0, Math.Min(_start.Y, end.Y));
        var width = Math.Min(ActualWidth, Math.Max(_start.X, end.X)) - x;
        var height = Math.Min(ActualHeight, Math.Max(_start.Y, end.Y)) - y;
        if (width < 6 || height < 6) return;
        SelectedRegion = new(x / ActualWidth, y / ActualHeight, width / ActualWidth, height / ActualHeight);
        DialogResult = true;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) DialogResult = false;
    }

    private void UpdateRectangle(Point end)
    {
        var x = Math.Min(_start.X, end.X);
        var y = Math.Min(_start.Y, end.Y);
        Canvas.SetLeft(SelectionRectangle, x);
        Canvas.SetTop(SelectionRectangle, y);
        SelectionRectangle.Width = Math.Abs(end.X - _start.X);
        SelectionRectangle.Height = Math.Abs(end.Y - _start.Y);
    }
}
