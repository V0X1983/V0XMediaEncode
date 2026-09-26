namespace V0XMediaEncode.Services.WatchFolders;

/// <summary>
/// Pure decision of whether a path that just showed up in a watched folder is a real source file
/// worth queuing, as opposed to a hidden file, an editor swap file, or another app's in-progress
/// download/copy. No I/O here (it never touches the file itself) so it is directly unit-testable;
/// actually waiting for the file to finish being written is <see cref="WatchFolderService"/>'s job.
/// </summary>
public static class WatchFolderFileFilter
{
    private static readonly string[] IgnoredExtensions = [".tmp", ".part", ".crdownload", ".download", ".partial"];

    public static bool IsCandidateFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        if (string.IsNullOrEmpty(fileName))
        {
            return false;
        }

        // Hidden/system dotfiles, Office/editor lock files (~$report.docx), and macOS resource forks (._foo).
        if (fileName.StartsWith('.') || fileName.StartsWith("~$"))
        {
            return false;
        }

        var extension = Path.GetExtension(filePath);
        if (Array.Exists(IgnoredExtensions, ignored => string.Equals(ignored, extension, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return true;
    }
}
