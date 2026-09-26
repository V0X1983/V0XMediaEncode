using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Serilog;
using V0XMediaEncode.App.Notifications;
using V0XMediaEncode.App.Update;
using V0XMediaEncode.App.ViewModels;
using V0XMediaEncode.Services.Ffmpeg;
using V0XMediaEncode.Services.History;
using V0XMediaEncode.Services.Presets;
using V0XMediaEncode.Services.Queue;
using V0XMediaEncode.Services.WatchFolders;

namespace V0XMediaEncode.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    private Window? _window;

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => LogCrash("AppDomain.UnhandledException", e.ExceptionObject as Exception);
        UnhandledException += (_, e) =>
        {
            LogCrash("Application.UnhandledException", e.Exception);
            e.Handled = true;
        };

        try
        {
            ConfigureLogging();
            InitializeComponent();
            Services = ConfigureServices();
        }
        catch (Exception ex)
        {
            LogCrash("App constructor", ex);
            throw;
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            _window = new MainWindow();
            _window.Activate();
        }
        catch (Exception ex)
        {
            LogCrash("OnLaunched", ex);
            throw;
        }
    }

    /// <summary>
    /// Last-resort logging for exceptions during startup, when Serilog itself might not be
    /// configured yet (or might be the thing that's broken) — writes straight to a file next to
    /// the regular logs instead of going through <see cref="Log.Logger"/>.
    /// </summary>
    private static void LogCrash(string phase, Exception? ex)
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "V0XMediaEncode",
                "logs",
                "crash.log");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(path, $"[{DateTime.Now:O}] {phase}: {ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Nothing more we can do if even crash logging fails.
        }
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton(Log.Logger);
        services.AddSingleton<IFFmpegLocator, FFmpegLocator>();
        services.AddSingleton<FFprobeService>();
        services.AddSingleton<FFmpegProcessService>();
        services.AddSingleton<FFmpegHardwareDetectionService>();
        services.AddSingleton<FFmpegThumbnailService>();
        services.AddSingleton<EncodeQueueOrchestrator>();
        services.AddSingleton<IPresetRepository>(_ => new SqlitePresetRepository());
        services.AddSingleton<IWatchFolderRepository>(_ => new SqliteWatchFolderRepository());
        services.AddSingleton<WatchFolderService>();
        services.AddSingleton<IHistoryRepository>(_ => new SqliteHistoryRepository());
        services.AddSingleton<ToastNotificationService>();
        services.AddSingleton<UpdateService>();

        services.AddSingleton<QueueViewModel>();
        services.AddSingleton<PresetsViewModel>();
        services.AddSingleton<WatchFoldersViewModel>();
        services.AddSingleton<HistoryViewModel>();
        services.AddSingleton<SettingsViewModel>();

        return services.BuildServiceProvider();
    }

    private static void ConfigureLogging()
    {
        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "V0XMediaEncode",
            "logs");
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(logDirectory, "v0x-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14)
            .CreateLogger();
    }
}
