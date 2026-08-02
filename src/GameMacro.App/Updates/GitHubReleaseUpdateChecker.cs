using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace GameMacro.App.Updates;

public sealed class GitHubReleaseUpdateChecker(HttpClient httpClient)
{
    private static readonly Uri LatestReleaseApi = new(
        "https://api.github.com/repos/absonggit/GameMacro/releases/latest");

    public async Task<AppUpdateInfo?> CheckAsync(
        Version currentVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(currentVersion);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseApi);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("KeyAssistant", currentVersion.ToString(3)));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return Parse(document.RootElement, currentVersion);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private static AppUpdateInfo? Parse(JsonElement release, Version currentVersion)
    {
        if (GetBoolean(release, "draft") || GetBoolean(release, "prerelease")) return null;
        if (!release.TryGetProperty("tag_name", out var tagElement)) return null;

        var tag = tagElement.GetString()?.Trim();
        if (string.IsNullOrEmpty(tag)) return null;
        if (tag[0] is 'v' or 'V') tag = tag[1..];
        if (!Version.TryParse(tag, out var latestVersion) || latestVersion <= currentVersion) return null;

        if (!release.TryGetProperty("html_url", out var pageElement)
            || !Uri.TryCreate(pageElement.GetString(), UriKind.Absolute, out var releasePage))
            return null;

        Uri? installerDownload = null;
        if (release.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
        {
            foreach (var asset in assets.EnumerateArray())
            {
                if (!asset.TryGetProperty("name", out var name)
                    || !string.Equals(name.GetString(), "GameMacro-Setup.exe", StringComparison.OrdinalIgnoreCase)
                    || !asset.TryGetProperty("browser_download_url", out var download)
                    || !Uri.TryCreate(download.GetString(), UriKind.Absolute, out installerDownload))
                    continue;
                break;
            }
        }

        return new AppUpdateInfo(latestVersion, releasePage, installerDownload);
    }

    private static bool GetBoolean(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value)
           && value.ValueKind is JsonValueKind.True;
}
