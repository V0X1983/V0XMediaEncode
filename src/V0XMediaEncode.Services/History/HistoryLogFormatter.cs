using System.Globalization;
using System.Text;
using V0XMediaEncode.Core.Models;

namespace V0XMediaEncode.Services.History;

/// <summary>Pure formatting of history entries into a plain-text log, for the Historique page's export button.</summary>
public static class HistoryLogFormatter
{
    public static string FormatEntry(HistoryEntry entry)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Fichier source : {entry.SourcePath}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Fichier de sortie : {entry.OutputPath}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Preset : {entry.PresetName ?? "(aucun)"}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Statut : {entry.Status}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Démarré : {entry.StartedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Terminé : {entry.CompletedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Durée : {entry.Duration:hh\\:mm\\:ss}");

        if (!string.IsNullOrEmpty(entry.ErrorMessage))
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"Erreur : {entry.ErrorMessage}");
        }

        if (!string.IsNullOrEmpty(entry.LogTail))
        {
            sb.AppendLine();
            sb.AppendLine("--- Fin du journal ffmpeg ---");
            sb.AppendLine(entry.LogTail);
        }

        return sb.ToString();
    }

    public static string FormatAll(IEnumerable<HistoryEntry> entries)
    {
        var sb = new StringBuilder();
        foreach (var entry in entries.OrderByDescending(e => e.CompletedAt))
        {
            sb.AppendLine(new string('=', 60));
            sb.Append(FormatEntry(entry));
        }

        return sb.ToString();
    }
}
