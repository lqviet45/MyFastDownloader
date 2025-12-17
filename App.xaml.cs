using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MyFastDownloader.App.Services.Auth;
using MyFastDownloader.App.Services.Core;
using MyFastDownloader.App.Services.Network;
using MyFastDownloader.App.Services.Proxy;
using MyFastDownloader.App.Services.Storage;
using MyFastDownloader.App.ViewModels;
using MyFastDownloader.App.Views;

namespace MyFastDownloader.App;

public partial class App : Application
{
    private LocalHttpServer? _httpServer;
    private IServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Force initialization of critical singletons
        _ = _serviceProvider.GetRequiredService<SettingsService>();
        _ = _serviceProvider.GetRequiredService<DownloadManager>();
        
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
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<SettingsService>();
        services.AddSingleton<DownloadManager>();
        services.AddSingleton<CredentialManager>();
        services.AddSingleton<ProxyManager>();
        
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
    }

    public static T GetRequiredService<T>() where T : notnull =>
        ((App)Current)._serviceProvider!.GetRequiredService<T>();
}