using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using V0XMediaEncode.Core.Models;
using V0XMediaEncode.Services.Presets;

namespace V0XMediaEncode.App.ViewModels;

/// <summary>Backs the "Presets" page: list + new/duplicate/delete/save for the tabbed preset editor.</summary>
public sealed partial class PresetsViewModel : ObservableObject
{
    private readonly IPresetRepository _presetRepository;

    public ObservableCollection<EncodePreset> Presets { get; } = [];

    [ObservableProperty]
    private EncodePreset? _selectedPreset;

    public PresetsViewModel(IPresetRepository presetRepository)
    {
        _presetRepository = presetRepository;
    }

    public async Task InitializeAsync()
    {
        await _presetRepository.InitializeAsync().ConfigureAwait(true);
        await ReloadAsync().ConfigureAwait(true);
    }

    private async Task ReloadAsync()
    {
        var presets = await _presetRepository.GetAllAsync().ConfigureAwait(true);
        var selectedId = SelectedPreset?.Id;

        Presets.Clear();
        foreach (var preset in presets)
        {
            Presets.Add(preset);
        }

        SelectedPreset = selectedId is { } id ? Presets.FirstOrDefault(p => p.Id == id) : null;
    }

    [RelayCommand]
    private async Task NewPresetAsync()
    {
        var preset = new EncodePreset { Name = "Nouveau preset" };
        await _presetRepository.SaveAsync(preset).ConfigureAwait(true);
        await ReloadAsync().ConfigureAwait(true);
        SelectedPreset = Presets.FirstOrDefault(p => p.Id == preset.Id);
    }

    [RelayCommand]
    private async Task DuplicatePresetAsync(EncodePreset? source)
    {
        if (source is null)
        {
            return;
        }

        var copy = new EncodePreset
        {
            Name = $"{source.Name} (copie)",
            Container = source.Container,
            VideoCodec = source.VideoCodec,
            HardwareEncoder = source.HardwareEncoder,
            RateControlMode = source.RateControlMode,
            VideoBitrateKbps = source.VideoBitrateKbps,
            MaxVideoBitrateKbps = source.MaxVideoBitrateKbps,
            CrfValue = source.CrfValue,
            Width = source.Width,
            Height = source.Height,
            FrameRate = source.FrameRate,
            AudioCodec = source.AudioCodec,
            AudioBitrateKbps = source.AudioBitrateKbps,
            AudioSampleRateHz = source.AudioSampleRateHz,
            AudioChannels = source.AudioChannels,
        };

        await _presetRepository.SaveAsync(copy).ConfigureAwait(true);
        await ReloadAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task DeletePresetAsync(EncodePreset? preset)
    {
        if (preset is null || preset.IsBuiltIn)
        {
            return;
        }

        await _presetRepository.DeleteAsync(preset.Id).ConfigureAwait(true);
        await ReloadAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task SavePresetAsync(EncodePreset? preset)
    {
        if (preset is null)
        {
            return;
        }

        await _presetRepository.SaveAsync(preset).ConfigureAwait(true);
        await ReloadAsync().ConfigureAwait(true);
    }
}
