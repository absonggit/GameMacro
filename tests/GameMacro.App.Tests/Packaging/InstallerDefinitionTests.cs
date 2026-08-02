namespace GameMacro.App.Tests.Packaging;

public sealed class InstallerDefinitionTests
{
    [Fact]
    public void Installer_is_per_user_preserves_profiles_and_builds_single_setup_exe()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(root, "installer", "GameMacro.iss");

        Assert.True(File.Exists(path), $"Installer definition not found: {path}");
        var script = File.ReadAllText(path);

        Assert.Contains("PrivilegesRequired=lowest", script);
        Assert.Contains(@"DefaultDirName={localappdata}\Programs\GameMacro", script);
        Assert.Contains("OutputBaseFilename=GameMacro-Setup", script);
        Assert.Contains("Uninstallable=yes", script);
        Assert.Contains("#define MyAppVersion \"1.0.1\"", script);
        Assert.Contains("#define MyAppName \"按键助手\"", script);
        Assert.Contains(@"{autoprograms}\{#MyAppName}", script);
        Assert.Contains(@"{autodesktop}\{#MyAppName}", script);
        Assert.DoesNotContain(@"GameMacro\Profiles", script);
    }

    [Fact]
    public void Installer_and_application_use_embedded_icon_and_simplified_chinese_messages()
    {
        var root = FindRepositoryRoot();
        var installerPath = Path.Combine(root, "installer", "GameMacro.iss");
        var projectPath = Path.Combine(root, "src", "GameMacro.App", "GameMacro.App.csproj");
        var iconPath = Path.Combine(root, "src", "GameMacro.App", "Assets", "AppIcon.ico");
        var languagePath = Path.Combine(root, "installer", "Languages", "ChineseSimplified.isl");

        var installer = File.ReadAllText(installerPath);
        var project = File.ReadAllText(projectPath);

        Assert.True(File.Exists(iconPath), $"Application icon not found: {iconPath}");
        Assert.True(File.Exists(languagePath), $"Simplified Chinese messages not found: {languagePath}");
        Assert.Contains("<ApplicationIcon>Assets\\AppIcon.ico</ApplicationIcon>", project);
        Assert.Contains("SetupIconFile=..\\src\\GameMacro.App\\Assets\\AppIcon.ico", installer);
        Assert.Contains("Name: \"chinesesimp\"; MessagesFile: \"Languages\\ChineseSimplified.isl\"", installer);
        Assert.Contains("IconFilename: \"{app}\\{#MyAppExeName}\"; IconIndex: 0", installer);
        Assert.Contains("<Product>按键助手</Product>", project);
        Assert.Contains("<AssemblyTitle>按键助手</AssemblyTitle>", project);
        Assert.Contains("<Version>1.0.1</Version>", project);
        Assert.Contains("<AssemblyVersion>1.0.1.0</AssemblyVersion>", project);
        Assert.Contains("<FileVersion>1.0.1.0</FileVersion>", project);
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
