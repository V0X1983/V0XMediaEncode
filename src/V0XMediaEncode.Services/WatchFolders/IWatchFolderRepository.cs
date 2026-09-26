using V0XMediaEncode.Core.Models;

namespace V0XMediaEncode.Services.WatchFolders;

public interface IWatchFolderRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WatchedFolder>> GetAllAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(WatchedFolder folder, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
