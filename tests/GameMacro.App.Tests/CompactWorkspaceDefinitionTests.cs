namespace GameMacro.App.Tests.Ui;

public sealed class CompactWorkspaceDefinitionTests
{
    [Fact]
    public void Main_window_uses_permanent_library_and_compact_mapping_slots()
    {
        var root = FindRepositoryRoot();
        var mainWindow = File.ReadAllText(Path.Combine(root, "src", "GameMacro.App", "MainWindow.xaml"));

        Assert.Contains("Title=\"按键助手 v1.0.4\"", mainWindow);
        Assert.Contains("Text=\"v1.0.4\"", mainWindow);
        Assert.Contains("x:Name=\"LibraryPanel\"", mainWindow);
        Assert.DoesNotContain("ToggleSkillLibrary_Click", mainWindow);
        Assert.DoesNotContain("Content=\"技能库\"", mainWindow);
        Assert.Contains("Width=\"60\" Height=\"78\"", mainWindow);
        Assert.Contains("Width=\"48\" Height=\"48\"", mainWindow);
        Assert.Contains("Visibility=\"Collapsed\"", mainWindow);
    }

    [Fact]
    public void Skill_library_wraps_slots_tightly_and_scopes_hover_to_one_slot()
    {
        var root = FindRepositoryRoot();
        var library = File.ReadAllText(Path.Combine(root, "src", "GameMacro.App", "SkillLibraryPanel.xaml"));

        Assert.Contains("<ItemsPanelTemplate><WrapPanel Orientation=\"Horizontal\"/></ItemsPanelTemplate>", library);
        Assert.Contains("<Trigger SourceName=\"TemplateSlot\" Property=\"IsMouseOver\" Value=\"True\">", library);
        Assert.DoesNotContain("AncestorType=ContentPresenter", library);
        Assert.Contains("Width=\"52\" Height=\"66\"", library);
        Assert.Contains("Width=\"44\" Height=\"44\"", library);
        Assert.DoesNotContain("Close_Click", library);
        Assert.DoesNotContain("Text=\"已添加\"", library);
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
