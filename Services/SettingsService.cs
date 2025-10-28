using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MyFastDownloader.App.Models;

namespace MyFastDownloader.App.Services;

/// <summary>
/// Service for managing application settings persistence
/// </summary>
public class SettingsService
{
    private readonly string _settingsFilePath;
    private AppSettings? _currentSettings;
    
    public SettingsService()
    {
        var appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MyFastDownloader");
        
        Directory.CreateDirectory(appDataFolder);
        _settingsFilePath = Path.Combine(appDataFolder, "settings.json");
    }
    
    /// <summary>
    /// Loads settings from disk or creates default settings
    /// </summary>
    public async Task<AppSettings> LoadSettingsAsync()
    {
        if (_currentSettings != null)
            return _currentSettings;
        
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                _currentSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            else
            {
                _currentSettings = new AppSettings();
                await SaveSettingsAsync(_currentSettings);
            }
        }
        catch
        {
            _currentSettings = new AppSettings();
        }
        
        return _currentSettings;
    }
    
    /// <summary>
    /// Saves settings to disk
    /// </summary>
    public async Task SaveSettingsAsync(AppSettings settings)
    {
        _currentSettings = settings;
        
        try
        {
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true 
            };
            var json = JsonSerializer.Serialize(settings, options);
            await File.WriteAllTextAsync(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            // Log error but don't throw - settings aren't critical
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Gets the current settings
    /// </summary>
    public AppSettings GetSettings()
    {
        return _currentSettings ?? new AppSettings();
    }
    
    /// <summary>
    /// Updates a specific setting and saves
    /// </summary>
    public async Task UpdateSettingAsync(Action<AppSettings> updateAction)
    {
        var settings = await LoadSettingsAsync();
        updateAction(settings);
        await SaveSettingsAsync(settings);
    }
}