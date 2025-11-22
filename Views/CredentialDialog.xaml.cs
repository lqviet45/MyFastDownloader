using System;
using System.Windows;
using System.Windows.Controls;
using MyFastDownloader.App.Models.Auth;
using MyFastDownloader.App.Models.Enums;
using MyFastDownloader.App.Services.Auth;

namespace MyFastDownloader.App.Views;

/// <summary>
/// Interaction logic for CredentialDialog.xaml
/// </summary>
public partial class CredentialDialog : Window
{
    private readonly CredentialManager _credentialManager;
    private Credential? _editingCredential;
    
    public Credential? ResultCredential { get; private set; }
    
    /// <summary>
    /// Constructor for adding new credential
    /// </summary>
    public CredentialDialog(CredentialManager credentialManager)
    {
        InitializeComponent();
        _credentialManager = credentialManager;
        
        // Set default selection
        AuthModeComboBox.SelectedIndex = 0; // Basic
        TitleTextBlock.Text = "Thêm Credential Mới";
    }
    
    /// <summary>
    /// Constructor for editing existing credential
    /// </summary>
    public CredentialDialog(CredentialManager credentialManager, Credential credential) 
        : this(credentialManager)
    {
        _editingCredential = credential;
        TitleTextBlock.Text = "Chỉnh Sửa Credential";
        
        LoadCredentialData(credential);
    }
    
    /// <summary>
    /// Loads credential data into form
    /// </summary>
    private void LoadCredentialData(Credential credential)
    {
        NameTextBox.Text = credential.Name;
        DomainTextBox.Text = credential.Domain;
        UsernameTextBox.Text = credential.Username;
        NotesTextBox.Text = credential.Notes ?? "";
        IsActiveCheckBox.IsChecked = credential.IsActive;
        
        // Set authentication mode
        AuthModeComboBox.SelectedIndex = credential.Mode switch
        {
            AuthenticationMode.Basic => 0,
            AuthenticationMode.Digest => 1,
            AuthenticationMode.NTLM => 2,
            AuthenticationMode.Bearer => 3,
            _ => 0
        };
        
        // Decrypt and load password/token
        try
        {
            if (credential.Mode == AuthenticationMode.Bearer && !string.IsNullOrEmpty(credential.EncryptedToken))
            {
                var token = _credentialManager.DecryptPassword(credential.EncryptedToken);
                BearerTokenTextBox.Text = token;
            }
            else if (!string.IsNullOrEmpty(credential.EncryptedPassword))
            {
                var password = _credentialManager.DecryptPassword(credential.EncryptedPassword);
                PasswordBox.Password = password;
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"⚠ Không thể giải mã password: {ex.Message}";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
        }
    }
    
    /// <summary>
    /// Handles authentication mode change
    /// </summary>
    private void AuthModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AuthModeComboBox.SelectedItem is ComboBoxItem item)
        {
            var mode = item.Tag.ToString();
            
            // Show/hide appropriate panels
            if (mode == "Bearer")
            {
                UsernamePasswordPanel.Visibility = Visibility.Collapsed;
                BearerTokenPanel.Visibility = Visibility.Visible;
            }
            else
            {
                UsernamePasswordPanel.Visibility = Visibility.Visible;
                BearerTokenPanel.Visibility = Visibility.Collapsed;
            }
        }
    }
    
    /// <summary>
    /// Validates form data
    /// </summary>
    private (bool IsValid, string Message) ValidateForm()
    {
        // Domain is required
        if (string.IsNullOrWhiteSpace(DomainTextBox.Text))
        {
            return (false, "⚠ Vui lòng nhập Domain/URL");
        }
        
        var mode = GetSelectedAuthMode();
        
        if (mode == AuthenticationMode.Bearer)
        {
            // Bearer token is required
            if (string.IsNullOrWhiteSpace(BearerTokenTextBox.Text))
            {
                return (false, "⚠ Vui lòng nhập Bearer Token");
            }
        }
        else
        {
            // Username and password are required
            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                return (false, "⚠ Vui lòng nhập Username");
            }
            
            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                return (false, "⚠ Vui lòng nhập Password");
            }
        }
        
        return (true, "");
    }
    
    /// <summary>
    /// Gets selected authentication mode
    /// </summary>
    private AuthenticationMode GetSelectedAuthMode()
    {
        if (AuthModeComboBox.SelectedItem is ComboBoxItem item)
        {
            return item.Tag.ToString() switch
            {
                "Basic" => AuthenticationMode.Basic,
                "Digest" => AuthenticationMode.Digest,
                "NTLM" => AuthenticationMode.NTLM,
                "Bearer" => AuthenticationMode.Bearer,
                _ => AuthenticationMode.Basic
            };
        }
        
        return AuthenticationMode.Basic;
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
            var mode = GetSelectedAuthMode();
            
            // Create or update credential
            var credential = _editingCredential ?? new Credential();
            
            credential.Name = NameTextBox.Text.Trim();
            credential.Domain = DomainTextBox.Text.Trim();
            credential.Mode = mode;
            credential.IsActive = IsActiveCheckBox.IsChecked ?? true;
            credential.Notes = NotesTextBox.Text.Trim();
            
            if (mode == AuthenticationMode.Bearer)
            {
                // Encrypt bearer token
                credential.EncryptedToken = _credentialManager.EncryptPassword(BearerTokenTextBox.Text.Trim());
                credential.Username = "Bearer"; // Placeholder
            }
            else
            {
                // Encrypt username and password
                credential.Username = UsernameTextBox.Text.Trim();
                credential.EncryptedPassword = _credentialManager.EncryptPassword(PasswordBox.Password);
            }
            
            // Set result and close
            ResultCredential = credential;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"✗ Lỗi: {ex.Message}";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            
            MessageBox.Show($"Không thể lưu credential: {ex.Message}", 
                          "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
        
        // Test URL is required for testing
        if (string.IsNullOrWhiteSpace(TestUrlTextBox.Text))
        {
            StatusTextBlock.Text = "⚠ Vui lòng nhập Test URL";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
            return;
        }
        
        try
        {
            StatusTextBlock.Text = "🔍 Đang kiểm tra kết nối...";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.LightBlue;
            
            var mode = GetSelectedAuthMode();
            
            // Create temporary credential for testing
            var testCredential = new Credential
            {
                Domain = DomainTextBox.Text.Trim(),
                Username = UsernameTextBox.Text.Trim(),
                Mode = mode
            };
            
            if (mode == AuthenticationMode.Bearer)
            {
                testCredential.EncryptedToken = _credentialManager.EncryptPassword(BearerTokenTextBox.Text.Trim());
            }
            else
            {
                testCredential.EncryptedPassword = _credentialManager.EncryptPassword(PasswordBox.Password);
            }
            
            var (success, testMessage) = await _credentialManager.TestCredentialAsync(
                testCredential, 
                TestUrlTextBox.Text.Trim());
            
            StatusTextBlock.Text = testMessage;
            StatusTextBlock.Foreground = success 
                ? System.Windows.Media.Brushes.LightGreen 
                : System.Windows.Media.Brushes.Orange;
            
            if (success)
            {
                MessageBox.Show(testMessage, "Thành công", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"✗ Lỗi: {ex.Message}";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            
            MessageBox.Show($"Lỗi khi kiểm tra: {ex.Message}", 
                          "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
