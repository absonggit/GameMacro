using System.Net;
using System.Text;
using GameMacro.App.Updates;

namespace GameMacro.App.Tests.Updates;

public sealed class GitHubReleaseUpdateCheckerTests
{
    [Fact]
    public async Task Returns_newer_stable_release_and_prefers_setup_asset()
    {
        using var client = CreateClient(HttpStatusCode.OK, """
        {
          "tag_name": "v1.0.3",
          "html_url": "https://github.com/absonggit/GameMacro/releases/tag/v1.0.3",
          "draft": false,
          "prerelease": false,
          "assets": [
            { "name": "notes.txt", "browser_download_url": "https://example.test/notes.txt" },
            { "name": "GameMacro-Setup.exe", "browser_download_url": "https://example.test/GameMacro-Setup.exe" }
          ]
        }
        """);
        var checker = new GitHubReleaseUpdateChecker(client);

        var update = await checker.CheckAsync(new Version(1, 0, 2), CancellationToken.None);

        Assert.NotNull(update);
        Assert.Equal(new Version(1, 0, 3), update.Version);
        Assert.Equal("https://github.com/absonggit/GameMacro/releases/tag/v1.0.3", update.ReleasePage.ToString());
        Assert.Equal("https://example.test/GameMacro-Setup.exe", update.InstallerDownload?.ToString());
    }

    [Theory]
    [InlineData("v1.0.2")]
    [InlineData("v1.0.1")]
    public async Task Returns_null_when_latest_release_is_not_newer(string tag)
    {
        using var client = CreateClient(HttpStatusCode.OK, $$"""
        {
          "tag_name": "{{tag}}",
          "html_url": "https://example.test/release",
          "draft": false,
          "prerelease": false,
          "assets": []
        }
        """);
        var checker = new GitHubReleaseUpdateChecker(client);

        var update = await checker.CheckAsync(new Version(1, 0, 2), CancellationToken.None);

        Assert.Null(update);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Ignores_draft_and_prerelease_versions(bool draft, bool prerelease)
    {
        using var client = CreateClient(HttpStatusCode.OK, $$"""
        {
          "tag_name": "v9.0.0",
          "html_url": "https://example.test/release",
          "draft": {{draft.ToString().ToLowerInvariant()}},
          "prerelease": {{prerelease.ToString().ToLowerInvariant()}},
          "assets": []
        }
        """);
        var checker = new GitHubReleaseUpdateChecker(client);

        var update = await checker.CheckAsync(new Version(1, 0, 2), CancellationToken.None);

        Assert.Null(update);
    }

    [Fact]
    public async Task Returns_null_when_the_update_service_is_unavailable()
    {
        using var client = CreateClient(HttpStatusCode.ServiceUnavailable, "unavailable");
        var checker = new GitHubReleaseUpdateChecker(client);

        var update = await checker.CheckAsync(new Version(1, 0, 2), CancellationToken.None);

        Assert.Null(update);
    }

    private static HttpClient CreateClient(HttpStatusCode statusCode, string content)
        => new(new StubHandler(statusCode, content))
        {
            BaseAddress = new Uri("https://api.github.com/")
        };

    private sealed class StubHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json"),
                RequestMessage = request
            });
    }
}
