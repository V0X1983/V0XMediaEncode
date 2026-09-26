using Serilog;
using Velopack;
using Velopack.Sources;

namespace V0XMediaEncode.App.Update;

public enum UpdateCheckStatus
{
    NotInstalled,
    UpToDate,
    UpdateAvailable,
    Failed,
}

public sealed record UpdateCheckResult(UpdateCheckStatus Status, UpdateInfo? UpdateInfo = null, string? ErrorMessage = null)
{
    public static UpdateCheckResult NotInstalled() => new(UpdateCheckStatus.NotInstalled);
    public static UpdateCheckResult UpToDate() => new(UpdateCheckStatus.UpToDate);
    public static UpdateCheckResult UpdateAvailable(UpdateInfo info) => new(UpdateCheckStatus.UpdateAvailable, info);
    public static UpdateCheckResult Failed(string message) => new(UpdateCheckStatus.Failed, ErrorMessage: message);
}

/// <summary>
/// Wraps Velopack's <see cref="UpdateManager"/> for the "unpackaged, direct-download" distribution
/// channel (see scripts/Publish-Velopack.ps1). MSIX builds are never Velopack-installed, so
/// <see cref="UpdateManager.IsInstalled"/> is false there and <see cref="CheckForUpdatesAsync"/>
/// short-circuits to <see cref="UpdateCheckStatus.NotInstalled"/> instead of calling the network.
/// </summary>
public sealed class UpdateService
{
    // Velopack reads release assets (the .nupkg/.exe produced by `vpk pack`, see
    // scripts/Publish-Velopack.ps1) from here. Update-checking is inert until a release with those
    // assets is published on this repo.
    private const string GithubRepoUrl = "https://github.com/V0X1983/V0XMediaEncode";

    private readonly UpdateManager _updateManager;
    private readonly ILogger _logger;

    public UpdateService(ILogger logger)
    {
        _logger = logger;
        _updateManager = new UpdateManager(new GithubSource(GithubRepoUrl, accessToken: null, prerelease: false));
    }

    public bool IsInstalled => _updateManager.IsInstalled;

    public string? CurrentVersion => _updateManager.CurrentVersion?.ToString();

    public async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        if (!_updateManager.IsInstalled)
        {
            return UpdateCheckResult.NotInstalled();
        }

        try
        {
            var updateInfo = await _updateManager.CheckForUpdatesAsync().ConfigureAwait(true);
            return updateInfo is null
                ? UpdateCheckResult.UpToDate()
                : UpdateCheckResult.UpdateAvailable(updateInfo);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Échec de la vérification des mises à jour");
            return UpdateCheckResult.Failed(ex.Message);
        }
    }

    /// <summary>Downloads the update then restarts the app into it. Does not return on success.</summary>
    public async Task DownloadAndApplyAsync(UpdateInfo updateInfo, Action<int>? progress = null)
    {
        await _updateManager.DownloadUpdatesAsync(updateInfo, progress).ConfigureAwait(true);
        _updateManager.ApplyUpdatesAndRestart(updateInfo);
    }
}
