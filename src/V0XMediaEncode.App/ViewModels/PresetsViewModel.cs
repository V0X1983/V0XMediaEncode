using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using V0XMediaEncode.Core.Models;
using V0XMediaEncode.Services.Ffmpeg;
using V0XMediaEncode.Services.Presets;

namespace V0XMediaEncode.App.ViewModels;

/// <summary>Backs the "Presets" page: list + new/duplicate/delete/save for the tabbed preset editor.</summary>
public sealed partial class PresetsViewModel : ObservableObject
{
    private readonly IPresetRepository _presetRepository;
    private readonly FFmpegHardwareDetectionService _hardwareDetectionService;

    public ObservableCollection<EncodePreset> Presets { get; } = [];

    /// <summary>
    /// Only the hardware encoder vendors this machine's ffmpeg build actually reported in
    /// `-encoders` (see <see cref="FFmpegHardwareDetectionService"/>), plus "None" (software) which
    /// is always available. Populated once in <see cref="InitializeAsync"/> so PresetsPage never
    /// offers e.g. NVENC on a machine without an NVIDIA GPU.
    /// </summary>
    public ObservableCollection<HardwareEncoderKind> AvailableHardwareEncoders { get; } = [HardwareEncoderKind.None];

    [ObservableProperty]
    private string _hardwareStatusText = "Détection de l'accélération matérielle...";

    [ObservableProperty]
    private EncodePreset? _selectedPreset;

    private HardwareEncoderKind _bestDetectedHardwareEncoder = HardwareEncoderKind.None;

    public PresetsViewModel(IPresetRepository presetRepository, FFmpegHardwareDetectionService hardwareDetectionService)
    {
        _presetRepository = presetRepository;
        _hardwareDetectionService = hardwareDetectionService;
    }

    public async Task InitializeAsync()
    {
        await _presetRepository.InitializeAsync().ConfigureAwait(true);
        await ReloadAsync().ConfigureAwait(true);
        await DetectHardwareAsync().ConfigureAwait(true);
    }

    private async Task DetectHardwareAsync()
    {
        var capabilities = await _hardwareDetectionService.DetectAsync().ConfigureAwait(true);

        var detected = new List<string>();
        if (capabilities.HasNvencH264 || capabilities.HasNvencHevc)
        {
            AvailableHardwareEncoders.Add(HardwareEncoderKind.Nvenc);
            detected.Add("NVIDIA (NVENC)");
            _bestDetectedHardwareEncoder = HardwareEncoderKind.Nvenc;
        }
        if (capabilities.HasQsvH264 || capabilities.HasQsvHevc)
        {
            AvailableHardwareEncoders.Add(HardwareEncoderKind.Qsv);
            detected.Add("Intel (Quick Sync)");
            if (_bestDetectedHardwareEncoder == HardwareEncoderKind.None)
            {
                _bestDetectedHardwareEncoder = HardwareEncoderKind.Qsv;
            }
        }
        if (capabilities.HasAmfH264 || capabilities.HasAmfHevc)
        {
            AvailableHardwareEncoders.Add(HardwareEncoderKind.Amf);
            detected.Add("AMD (AMF)");
            if (_bestDetectedHardwareEncoder == HardwareEncoderKind.None)
            {
                _bestDetectedHardwareEncoder = HardwareEncoderKind.Amf;
            }
        }

        HardwareStatusText = detected.Count > 0
            ? $"Accélération détectée : {string.Join(", ", detected)}."
            : "Aucune accélération matérielle détectée sur cette machine — encodage logiciel uniquement.";

        await ApplyDetectedHardwareToBuiltInPresetsAsync(capabilities).ConfigureAwait(true);
    }

    /// <summary>
    /// Auto-selects the detected hardware encoder on the built-in presets it's actually safe for.
    /// Deliberately excludes CRF presets (H.265 4K HDR/1080p Compact, MKV archive): ffmpeg's `-crf`
    /// flag is an x264/x265-only option and NVENC/QSV/AMF would reject it (they use `-cq` instead,
    /// which <see cref="FFmpegArgumentBuilder"/> doesn't emit) - only touches VBR/CBR presets, whose
    /// `-b:v`/`-maxrate` bitrate flags are generic across every encoder. Only upgrades a preset that
    /// is still on "None" (software), so a user who deliberately picked something else - including
    /// explicitly setting it back to "None" - isn't overridden on the next launch.
    /// </summary>
    private async Task ApplyDetectedHardwareToBuiltInPresetsAsync(HardwareEncoderCapabilities capabilities)
    {
        var anyChanged = false;

        foreach (var preset in Presets)
        {
            if (!preset.IsBuiltIn || preset.HardwareEncoder != HardwareEncoderKind.None || preset.RateControlMode == RateControlMode.Crf)
            {
                continue;
            }

            var best = preset.VideoCodec switch
            {
                VideoCodec.H264 when capabilities.HasNvencH264 => HardwareEncoderKind.Nvenc,
                VideoCodec.H264 when capabilities.HasQsvH264 => HardwareEncoderKind.Qsv,
                VideoCodec.H264 when capabilities.HasAmfH264 => HardwareEncoderKind.Amf,
                VideoCodec.H265 when capabilities.HasNvencHevc => HardwareEncoderKind.Nvenc,
                VideoCodec.H265 when capabilities.HasQsvHevc => HardwareEncoderKind.Qsv,
                VideoCodec.H265 when capabilities.HasAmfHevc => HardwareEncoderKind.Amf,
                _ => (HardwareEncoderKind?)null,
            };

            if (best is { } kind)
            {
                preset.HardwareEncoder = kind;
                await _presetRepository.SaveAsync(preset).ConfigureAwait(true);
                anyChanged = true;
            }
        }

        if (anyChanged)
        {
            await ReloadAsync().ConfigureAwait(true);
        }
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
        // Defaults to Vbr/H264 (see EncodePreset), so the detected hardware encoder is always a
        // safe choice here - unlike ApplyDetectedHardwareToBuiltInPresetsAsync, there's no risk of
        // it being a CRF preset since none exists yet.
        var preset = new EncodePreset { Name = "Nouveau preset", HardwareEncoder = _bestDetectedHardwareEncoder };
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
