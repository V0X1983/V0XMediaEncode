using V0XMediaEncode.Core.Models;

namespace V0XMediaEncode.Services.WatchFolders;

public sealed class FileDetectedEventArgs(WatchedFolder folder, string filePath) : EventArgs
{
    public WatchedFolder Folder { get; } = folder;

    public string FilePath { get; } = filePath;
}
