using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyFastDownloader.App.ViewModels.Base;

/// <summary>
/// Base class for all ViewModels implementing INotifyPropertyChanged
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises PropertyChanged event for the specified property
    /// </summary>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets property value and raises PropertyChanged if value changed
    /// </summary>
    /// <returns>True if value was changed and event was raised</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Sets property value and raises PropertyChanged for multiple properties
    /// </summary>
    protected bool SetProperty<T>(ref T field, T value, params string[] additionalPropertyNames)
    {
        if (Equals(field, value))
            return false;

        field = value;
        
        // Raise for the main property (uses caller member name)
        OnPropertyChanged();
        
        // Raise for additional properties
        foreach (var propertyName in additionalPropertyNames)
        {
            OnPropertyChanged(propertyName);
        }
        
        return true;
    }
}