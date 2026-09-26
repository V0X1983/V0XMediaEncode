using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Velopack;

namespace V0XMediaEncode.App;

public static class Program
{
    /// <summary>
    /// Explicit entry point (see DISABLE_XAML_GENERATED_MAIN in the csproj). Needed only so
    /// <see cref="VelopackApp.Run"/> executes before <see cref="Application.Start"/> — Velopack
    /// intercepts its own install/update/uninstall hook arguments here and exits the process
    /// without ever creating a window. It is a no-op when the app was launched normally (including
    /// under MSIX, which never passes those arguments).
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        global::WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            System.Threading.SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
    }
}
