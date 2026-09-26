using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using V0XMediaEncode.App.Update;

namespace V0XMediaEncode.App.ViewModels;

/// <summary>Backs the "Paramètres" page: app version plus the Velopack update check (Phase 9).</summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly UpdateService _updateService;
    private readonly ILogger _logger;

    private UpdateCheckResult? _lastCheck;

    public SettingsViewModel(UpdateService updateService, ILogger logger)
    {
        _updateService = updateService;
        _logger = logger;
    }

    public string AppVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?";

    [ObservableProperty]
    private string _updateStatusText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCheckForUpdates))]
    private bool _isCheckingForUpdates;

    [ObservableProperty]
    private bool _canApplyUpdate;

    public bool CanCheckForUpdates => !IsCheckingForUpdates;

    public void Initialize()
    {
        UpdateStatusText = _updateService.IsInstalled
            ? "Prêt à vérifier les mises à jour."
            : "Mises à jour automatiques indisponibles (build non installée via Velopack, ex. MSIX ou débogage).";
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        IsCheckingForUpdates = true;
        CanApplyUpdate = false;
        UpdateStatusText = "Recherche de mises à jour...";

        try
        {
            _lastCheck = await _updateService.CheckForUpdatesAsync();
            UpdateStatusText = _lastCheck.Status switch
            {
                UpdateCheckStatus.NotInstalled => "Mises à jour automatiques indisponibles pour cette build.",
                UpdateCheckStatus.UpToDate => "Vous utilisez déjà la dernière version.",
                UpdateCheckStatus.UpdateAvailable => $"Nouvelle version disponible : {_lastCheck.UpdateInfo!.TargetFullRelease.Version}",
                UpdateCheckStatus.Failed => $"Échec de la vérification : {_lastCheck.ErrorMessage}",
                _ => UpdateStatusText,
            };
            CanApplyUpdate = _lastCheck.Status == UpdateCheckStatus.UpdateAvailable;
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    [RelayCommand]
    private async Task DownloadAndApplyUpdateAsync()
    {
        if (_lastCheck?.UpdateInfo is not { } updateInfo)
        {
            return;
        }

        IsCheckingForUpdates = true;
        UpdateStatusText = "Téléchargement de la mise à jour...";

        try
        {
            await _updateService.DownloadAndApplyAsync(updateInfo);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Échec de l'application de la mise à jour");
            UpdateStatusText = $"Échec de la mise à jour : {ex.Message}";
            IsCheckingForUpdates = false;
        }
    }
}
