using System.Configuration;
using System.Data;
using System.Windows;

namespace MyFastDownloader.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // e.Args có thể chứa URI giao thức tùy chỉnh
        // Bạn có thể forward vào MainViewModel nếu muốn auto-add
    }
}