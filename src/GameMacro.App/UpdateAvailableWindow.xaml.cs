using System.Windows;
using GameMacro.App.Updates;

namespace GameMacro.App;

public partial class UpdateAvailableWindow : Window
{
    public UpdateAvailableWindow(AppUpdateInfo update)
    {
        InitializeComponent();
        VersionText.Text = $"当前有 v{update.Version.ToString(3)} 可用";
    }

    private void Later_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void Download_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
