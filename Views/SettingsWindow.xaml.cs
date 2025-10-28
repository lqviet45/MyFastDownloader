using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using MyFastDownloader.App.Models;
using MyFastDownloader.App.Services;

namespace MyFastDownloader.App.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;
    private AppSettings _settings;

    public SettingsWindow()
    {
        InitializeComponent();
        
        _settingsService = App.GetSettingsService() ?? new SettingsService();
        _settings = new AppSettings();
        _ = LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        _settings = await _settingsService.LoadSettingsAsync();
        DataContext = _settings;
    }

    private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
    {
        // Use Windows Shell to select folder (no WinForms dependency needed)
        var selectedFolder = ShowFolderBrowserDialog(_settings.DefaultDownloadFolder);
        
        if (!string.IsNullOrEmpty(selectedFolder))
        {
            _settings.DefaultDownloadFolder = selectedFolder;
            DefaultFolderTextBox.Text = selectedFolder;
        }
    }

    private string? ShowFolderBrowserDialog(string initialFolder)
    {
        try
        {
            // Use Windows Shell COM interface for folder selection
            var dialog = (IFileDialog)new FileOpenDialog();
            
            // Set options to select folders
            dialog.GetOptions(out var options);
            options |= FOS.FOS_PICKFOLDERS | FOS.FOS_FORCEFILESYSTEM | FOS.FOS_PATHMUSTEXIST;
            dialog.SetOptions(options);
            
            // Set initial folder
            if (Directory.Exists(initialFolder))
            {
                IShellItem? initialFolderItem = null;
                SHCreateItemFromParsingName(initialFolder, IntPtr.Zero, typeof(IShellItem).GUID, out initialFolderItem);
                if (initialFolderItem != null)
                {
                    dialog.SetFolder(initialFolderItem);
                    Marshal.ReleaseComObject(initialFolderItem);
                }
            }
            
            // Show dialog
            var hwnd = new WindowInteropHelper(this).Handle;
            var result = dialog.Show(hwnd);
            
            if (result == 0) // S_OK
            {
                dialog.GetResult(out var resultItem);
                resultItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var path);
                Marshal.ReleaseComObject(resultItem);
                Marshal.ReleaseComObject(dialog);
                return path;
            }
            
            Marshal.ReleaseComObject(dialog);
        }
        catch
        {
            // Fallback to simple input dialog if COM fails
            return ShowSimpleFolderInput(initialFolder);
        }
        
        return null;
    }
    
    private string? ShowSimpleFolderInput(string currentFolder)
    {
        // Simple fallback: show input dialog for folder path
        var inputWindow = new Window
        {
            Title = "Chọn thư mục",
            Width = 500,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            Background = System.Windows.Media.Brushes.White
        };
        
        var grid = new System.Windows.Controls.Grid
        {
            Margin = new Thickness(20)
        };
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
        
        var label = new System.Windows.Controls.TextBlock 
        { 
            Text = "Nhập đường dẫn thư mục:",
            Margin = new Thickness(0, 0, 0, 10)
        };
        System.Windows.Controls.Grid.SetRow(label, 0);
        
        var textBox = new System.Windows.Controls.TextBox
        {
            Text = currentFolder,
            Padding = new Thickness(5),
            Margin = new Thickness(0, 0, 0, 15)
        };
        System.Windows.Controls.Grid.SetRow(textBox, 1);
        
        var buttonPanel = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        
        var okButton = new System.Windows.Controls.Button
        {
            Content = "OK",
            Width = 80,
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 0, 10, 0)
        };
        okButton.Click += (s, e) => { inputWindow.DialogResult = true; inputWindow.Close(); };
        
        var cancelButton = new System.Windows.Controls.Button
        {
            Content = "Hủy",
            Width = 80,
            Padding = new Thickness(10, 5, 10, 5)
        };
        cancelButton.Click += (s, e) => { inputWindow.DialogResult = false; inputWindow.Close(); };
        
        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        System.Windows.Controls.Grid.SetRow(buttonPanel, 2);
        
        grid.Children.Add(label);
        grid.Children.Add(textBox);
        grid.Children.Add(buttonPanel);
        
        inputWindow.Content = grid;
        
        if (inputWindow.ShowDialog() == true)
        {
            return textBox.Text;
        }
        
        return null;
    }

    private void ResetFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var defaultFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");
        
        _settings.DefaultDownloadFolder = defaultFolder;
        DefaultFolderTextBox.Text = defaultFolder;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Validate folder exists
            if (!Directory.Exists(_settings.DefaultDownloadFolder))
            {
                var result = MessageBox.Show(
                    "Thư mục không tồn tại. Bạn có muốn tạo thư mục này?",
                    "Xác nhận",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Directory.CreateDirectory(_settings.DefaultDownloadFolder);
                }
                else
                {
                    return;
                }
            }

            await _settingsService.SaveSettingsAsync(_settings);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi lưu cài đặt: {ex.Message}", "Lỗi", 
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    #region Windows Shell COM Interfaces for Folder Browser
    
    [ComImport]
    [Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
    [ClassInterface(ClassInterfaceType.None)]
    private class FileOpenDialog { }

    [ComImport]
    [Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileDialog
    {
        [PreserveSig] int Show(IntPtr hwndOwner);
        void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
        void SetFileTypeIndex(uint iFileType);
        void GetFileTypeIndex(out uint piFileType);
        void Advise(IntPtr pfde, out uint pdwCookie);
        void Unadvise(uint dwCookie);
        void SetOptions(FOS fos);
        void GetOptions(out FOS pfos);
        void SetDefaultFolder(IShellItem psi);
        void SetFolder(IShellItem psi);
        void GetFolder(out IShellItem ppsi);
        void GetCurrentSelection(out IShellItem ppsi);
        void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
        void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
        void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
        void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
        void GetResult(out IShellItem ppsi);
        void AddPlace(IShellItem psi, int fdap);
        void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
        void Close(int hr);
        void SetClientGuid(ref Guid guid);
        void ClearClientData();
        void SetFilter(IntPtr pFilter);
    }

    [ComImport]
    [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
        void GetParent(out IShellItem ppsi);
        void GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
        void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
        void Compare(IShellItem psi, uint hint, out int piOrder);
    }

    [Flags]
    private enum FOS : uint
    {
        FOS_PICKFOLDERS = 0x00000020,
        FOS_FORCEFILESYSTEM = 0x00000040,
        FOS_PATHMUSTEXIST = 0x00000800
    }

    private enum SIGDN : uint
    {
        SIGDN_FILESYSPATH = 0x80058000
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        IntPtr pbc,
        [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
        [MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] out IShellItem? ppv);

    #endregion
}