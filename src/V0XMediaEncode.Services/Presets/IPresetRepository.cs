using V0XMediaEncode.Core.Models;

namespace V0XMediaEncode.Services.Presets;

public interface IPresetRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EncodePreset>> GetAllAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(EncodePreset preset, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
