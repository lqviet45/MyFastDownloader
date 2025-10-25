using System.Windows;
using MyFastDownloader.App.Services;
using MyFastDownloader.App.ViewModels;
using MyFastDownloader.App.Views;

namespace MyFastDownloader.App;

public partial class App : Application
{
    private LocalHttpServer? _httpServer;
    private static DownloadManager? _downloadManager;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Initialize download manager (singleton for the app)
        _downloadManager = new DownloadManager();
        
        // START HTTP SERVER FOR BROWSER INTEGRATION
        _httpServer = new LocalHttpServer(4153);
        _httpServer.OnAddUrl = async (url) =>
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow?.DataContext is MainViewModel vm)
                {
                    vm.DownloadUrl = url;
                    await vm.AddDownloadAsync();
                }
            });
        };
        _httpServer.Start();
        
        // Handle command line arguments (custom protocol URIs)
        if (e.Args.Length > 0 && e.Args[0].StartsWith("myfastdownloader://"))
        {
            var url = e.Args[0].Replace("myfastdownloader://", "");
            Dispatcher.InvokeAsync(async () =>
            {
                await System.Threading.Tasks.Task.Delay(500);
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow?.DataContext is MainViewModel vm)
                {
                    vm.DownloadUrl = url;
                    await vm.AddDownloadAsync();
                }
            });
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _httpServer?.Dispose();
        base.OnExit(e);
    }
    
    public static DownloadManager? GetDownloadManager() => _downloadManager;
}