namespace GameMacro.App.Updates;

public sealed record AppUpdateInfo(
    Version Version,
    Uri ReleasePage,
    Uri? InstallerDownload);
