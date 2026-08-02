namespace GameMacro.App.Tests.Ui;

public sealed class DarkTitleBarDefinitionTests
{
    [Fact]
    public void Main_window_applies_native_dark_title_bar_after_handle_creation()
    {
        var root = FindRepositoryRoot();
        var mainWindow = File.ReadAllText(Path.Combine(root, "src", "GameMacro.App", "MainWindow.xaml.cs"));
        var appearancePath = Path.Combine(root, "src", "GameMacro.App", "Platform", "WindowAppearanceService.cs");

        Assert.Contains("SourceInitialized += MainWindow_SourceInitialized;", mainWindow);
        Assert.True(File.Exists(appearancePath), "WindowAppearanceService.cs should define the native title-bar integration.");
        var appearance = File.ReadAllText(appearancePath);
        Assert.Contains("WindowAppearanceService.ApplyDarkTitleBar(this);", mainWindow);
        Assert.Contains("DwmSetWindowAttribute", appearance);
        Assert.Contains("DwmwaUseImmersiveDarkMode = 20", appearance);
        Assert.Contains("DwmwaCaptionColor = 35", appearance);
        Assert.Contains("DwmwaTextColor = 36", appearance);
        Assert.Contains("DwmwaBorderColor = 34", appearance);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "GameMacro.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate GameMacro.sln.");
    }
}
