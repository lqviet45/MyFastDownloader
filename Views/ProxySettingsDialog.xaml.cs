using System;
using System.Windows;
using System.Windows.Controls;
using MyFastDownloader.App.Models.Enums;
using MyFastDownloader.App.Models.Proxy;
using MyFastDownloader.App.Services.Proxy;

namespace MyFastDownloader.App.Views;

/// <summary>
/// Interaction logic for ProxySettingsDialog.xaml
/// </summary>
public partial class ProxySettingsDialog : Window
{
    private readonly ProxyManager _proxyManager;
    private ProxyConfig? _editingConfig;
    
    public ProxyConfig? ResultConfig { get; private set; }
    
    /// <summary>
    /// Constructor for adding new proxy config
    /// </summary>
    public ProxySettingsDialog(ProxyManager proxyManager)
    {
        InitializeComponent();
        _proxyManager = proxyManager;
        
        InitializeProxyTypes();
        TitleTextBlock.Text = "Add New Proxy";
    }
    
    /// <summary>
    /// Constructor for editing existing proxy config
    /// </summary>
    public ProxySettingsDialog(ProxyManager proxyManager, ProxyConfig config) 
        : this(proxyManager)
    {
        _editingConfig = config;
        TitleTextBlock.Text = "Edit Proxy Configuration";
        
        LoadConfigData(config);
    }
    
    /// <summary>
    /// Initializes proxy type dropdown
    /// </summary>
    private void InitializeProxyTypes()
    {
        ProxyTypeComboBox.Items.Add(new ComboBoxItem { Content = "No Proxy (Direct)", Tag = ProxyType.None });
        ProxyTypeComboBox.Items.Add(new ComboBoxItem { Content = "System Proxy (Auto)", Tag = ProxyType.System });
        ProxyTypeComboBox.Items.Add(new ComboBoxItem { Content = "HTTP Proxy", Tag = ProxyType.Http });
        ProxyTypeComboBox.Items.Add(new ComboBoxItem { Content = "HTTPS Proxy", Tag = ProxyType.Https });
        ProxyTypeComboBox.Items.Add(new ComboBoxItem { Content = "SOCKS4", Tag = ProxyType.Socks4 });
        ProxyTypeComboBox.Items.Add(new ComboBoxItem { Content = "SOCKS5", Tag = ProxyType.Socks5 });
        
        ProxyTypeComboBox.SelectedIndex = 0;
    }
    
    /// <summary>
    /// Loads config data into form
    /// </summary>
    private void LoadConfigData(ProxyConfig config)
    {
        NameTextBox.Text = config.Name;
        
        // Set proxy type
        for (int i = 0; i < ProxyTypeComboBox.Items.Count; i++)
        {
            var item = (ComboBoxItem)ProxyTypeComboBox.Items[i];
            if ((ProxyType)item.Tag == config.Type)
            {
                ProxyTypeComboBox.SelectedIndex = i;
                break;
            }
        }
        
        HostTextBox.Text = config.Host;
        PortTextBox.Text = config.Port.ToString();
        RequiresAuthCheckBox.IsChecked = config.RequiresAuth;
        UsernameTextBox.Text = config.Username;
        BypassListTextBox.Text = config.BypassList;
        BypassLocalCheckBox.IsChecked = config.BypassLocalAddresses;
        NotesTextBox.Text = config.Notes ?? "";
        IsActiveCheckBox.IsChecked = config.IsActive;
        
        // Decrypt password
        try
        {
            if (!string.IsNullOrEmpty(config.EncryptedPassword))
            {
                var password = _proxyManager.DecryptPassword(config.EncryptedPassword);
                PasswordBox.Password = password;
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"⚠ Could not decrypt password: {ex.Message}";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
        }
    }
    
    /// <summary>
    /// Handles proxy type selection change
    /// </summary>
    private void ProxyTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProxyTypeComboBox.SelectedItem is ComboBoxItem item)
        {
            var type = (ProxyType)item.Tag;
            
            // Show/hide proxy details based on type
            ProxyDetailsPanel.Visibility = (type != ProxyType.None && type != ProxyType.System)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }
    
    /// <summary>
    /// Handles authentication checkbox change
    /// </summary>
    private void RequiresAuthCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        AuthDetailsPanel.Visibility = RequiresAuthCheckBox.IsChecked == true
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
    
    /// <summary>
    /// Validates form data
    /// </summary>
    private (bool IsValid, string Message) ValidateForm()
    {
        var type = GetSelectedProxyType();
        
        // No validation needed for None/System
        if (type == ProxyType.None || type == ProxyType.System)
        {
            return (true, "");
        }
        
        // Host is required
        if (string.IsNullOrWhiteSpace(HostTextBox.Text))
        {
            return (false, "⚠ Proxy host is required");
        }
        
        // Port validation
        if (!int.TryParse(PortTextBox.Text, out int port) || port <= 0 || port > 65535)
        {
            return (false, "⚠ Port must be between 1 and 65535");
        }
        
        // Authentication validation
        if (RequiresAuthCheckBox.IsChecked == true)
        {
            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                return (false, "⚠ Username is required for authentication");
            }
            
            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                return (false, "⚠ Password is required for authentication");
            }
        }
        
        return (true, "");
    }
    
    /// <summary>
    /// Gets selected proxy type
    /// </summary>
    private ProxyType GetSelectedProxyType()
    {
        if (ProxyTypeComboBox.SelectedItem is ComboBoxItem item)
        {
            return (ProxyType)item.Tag;
        }
        return ProxyType.None;
    }
    
    /// <summary>
    /// Handles save button click
    /// </summary>
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate form
        var (isValid, message) = ValidateForm();
        if (!isValid)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
            return;
        }
        
        try
        {
            var type = GetSelectedProxyType();
            
            // Create or update config
            var config = _editingConfig ?? new ProxyConfig();
            
            config.Name = string.IsNullOrWhiteSpace(NameTextBox.Text) 
                ? "Proxy Configuration" 
                : NameTextBox.Text.Trim();
            config.Type = type;
            config.IsActive = IsActiveCheckBox.IsChecked ?? false;
            config.Notes = NotesTextBox.Text.Trim();
            
            if (type != ProxyType.None && type != ProxyType.System)
            {
                config.Host = HostTextBox.Text.Trim();
                
                if (int.TryParse(PortTextBox.Text, out int port))
                {
                    config.Port = port;
                }
                
                config.RequiresAuth = RequiresAuthCheckBox.IsChecked ?? false;
                
                if (config.RequiresAuth)
                {
                    config.Username = UsernameTextBox.Text.Trim();
                    config.EncryptedPassword = _proxyManager.EncryptPassword(PasswordBox.Password);
                }
                
                config.BypassList = BypassListTextBox.Text.Trim();
                config.BypassLocalAddresses = BypassLocalCheckBox.IsChecked ?? true;
            }
            
            // Validate before saving
            var (configValid, validationMsg) = config.Validate();
            if (!configValid)
            {
                StatusTextBlock.Text = $"⚠ {validationMsg}";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
                return;
            }
            
            // Set result and close
            ResultConfig = config;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"✗ Error: {ex.Message}";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            
            MessageBox.Show($"Failed to save proxy config: {ex.Message}",
                          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Handles cancel button click
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
    
    /// <summary>
    /// Handles test button click
    /// </summary>
    private async void TestButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate form first
        var (isValid, message) = ValidateForm();
        if (!isValid)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
            return;
        }
        
        // Test URL is required
        if (string.IsNullOrWhiteSpace(TestUrlTextBox.Text))
        {
            StatusTextBlock.Text = "⚠ Please enter a test URL";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
            return;
        }
        
        try
        {
            StatusTextBlock.Text = "🔍 Testing connection...";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.LightBlue;
            
            var type = GetSelectedProxyType();
            
            // Create temporary config for testing
            var testConfig = new ProxyConfig
            {
                Type = type,
                Host = HostTextBox.Text.Trim(),
                RequiresAuth = RequiresAuthCheckBox.IsChecked ?? false,
                Username = UsernameTextBox.Text.Trim(),
                BypassLocalAddresses = BypassLocalCheckBox.IsChecked ?? true
            };
            
            if (int.TryParse(PortTextBox.Text, out int port))
            {
                testConfig.Port = port;
            }
            
            if (testConfig.RequiresAuth && !string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                testConfig.EncryptedPassword = _proxyManager.EncryptPassword(PasswordBox.Password);
            }
            
            var (success, testMessage) = await _proxyManager.TestProxyAsync(
                testConfig,
                TestUrlTextBox.Text.Trim());
            
            StatusTextBlock.Text = testMessage;
            StatusTextBlock.Foreground = success
                ? System.Windows.Media.Brushes.LightGreen
                : System.Windows.Media.Brushes.Orange;
            
            if (success)
            {
                MessageBox.Show(testMessage, "Test Successful",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"✗ Error: {ex.Message}";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            
            MessageBox.Show($"Test failed: {ex.Message}",
                          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
