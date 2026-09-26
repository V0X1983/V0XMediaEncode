using V0XMediaEncode.Core.Models;

namespace V0XMediaEncode.Services.History;

public interface IHistoryRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HistoryEntry>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(HistoryEntry entry, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
