using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Serilog;
using V0XMediaEncode.Core.Models;

namespace V0XMediaEncode.App.Notifications;

/// <summary>
/// Thin wrapper around the Windows App SDK's <see cref="AppNotificationManager"/> for the one toast
/// this app raises: "a queued job finished". Kept in the App layer (not Services) since it's a WinUI/
/// packaged-app concern the encode engine itself has no business knowing about.
///
/// Registration can fail (observed as a COMException "No COM servers are registered for this app"
/// on a loose, unsigned dev-mode package registration) even though the rest of the app works fine —
/// notifications are strictly best-effort, so a failure here is swallowed rather than left to take
/// down the whole app.
/// </summary>
public sealed class ToastNotificationService
{
    private readonly ILogger _logger;
    private readonly bool _isAvailable;

    public ToastNotificationService(ILogger logger)
    {
        _logger = logger;
        try
        {
            AppNotificationManager.Default.Register();
            _isAvailable = true;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Impossible d'enregistrer les notifications Windows ; elles seront désactivées pour cette session.");
            _isAvailable = false;
        }
    }

    public void NotifyJobCompleted(EncodeJob job)
    {
        if (!_isAvailable)
        {
            return;
        }

        try
        {
            var builder = new AppNotificationBuilder()
                .AddText("V0X Media Encode")
                .AddText(job.FileName)
                .AddText(job.Status switch
                {
                    EncodeStatus.Completed => "Encodage terminé avec succès.",
                    EncodeStatus.Failed => $"Échec de l'encodage : {job.ErrorMessage ?? "erreur inconnue"}",
                    EncodeStatus.Cancelled => "Encodage annulé.",
                    _ => job.Status.ToString(),
                });

            AppNotificationManager.Default.Show(builder.BuildNotification());
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Échec de l'affichage de la notification pour {File}", job.FileName);
        }
    }
}
