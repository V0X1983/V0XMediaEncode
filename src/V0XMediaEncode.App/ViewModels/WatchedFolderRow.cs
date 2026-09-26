using V0XMediaEncode.Core.Models;

namespace V0XMediaEncode.App.ViewModels;

/// <summary>
/// Display-only wrapper pairing a <see cref="WatchedFolder"/> with its resolved preset name, so the
/// watch-folders list can show the name via a plain property binding instead of a XAML function
/// binding that reaches back into the page/view-model.
/// </summary>
public sealed record WatchedFolderRow(WatchedFolder Folder, string PresetName)
{
    public string Path => Folder.Path;

    public bool IsEnabled => Folder.IsEnabled;

    public bool IncludeSubdirectories => Folder.IncludeSubdirectories;
}
