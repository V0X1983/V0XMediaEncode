using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using V0XMediaEncode.App.ViewModels;
using V0XMediaEncode.Core.Models;

namespace V0XMediaEncode.App.Views;

public sealed partial class PresetsPage : Page
{
    public PresetsViewModel ViewModel { get; }

    public IReadOnlyList<ContainerFormat> ContainerOptions { get; } = Enum.GetValues<ContainerFormat>();

    public IReadOnlyList<VideoCodec> VideoCodecOptions { get; } = Enum.GetValues<VideoCodec>();

    public IReadOnlyList<HardwareEncoderKind> HardwareEncoderOptions { get; } = Enum.GetValues<HardwareEncoderKind>();

    public IReadOnlyList<RateControlMode> RateControlOptions { get; } = Enum.GetValues<RateControlMode>();

    public IReadOnlyList<AudioCodec> AudioCodecOptions { get; } = Enum.GetValues<AudioCodec>();

    public PresetsPage()
    {
        ViewModel = App.Services.GetRequiredService<PresetsViewModel>();
        InitializeComponent();
        Loaded += PresetsPage_Loaded;
    }

    private async void PresetsPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
    }
}
