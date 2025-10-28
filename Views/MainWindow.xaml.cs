using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MyFastDownloader.App.Models;
using MyFastDownloader.App.ViewModels;
using TaskStatus = MyFastDownloader.App.Models.TaskStatus;

namespace MyFastDownloader.App.Views;

/// <summary>
/// Enhanced MainWindow with modern UX/UI improvements
/// Features: Smooth animations, better feedback, improved interactions
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private const string DefaultDownloadFolder = "Downloads";

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        
        // Load window with fade-in animation
        Loaded += OnWindowLoaded;
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        // Animate window appearance
        this.Opacity = 0;
        var fadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromSeconds(0.4),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        this.BeginAnimation(OpacityProperty, fadeIn);
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        await AddDownloadWithAnimation();
    }

    private async Task AddDownloadWithAnimation()
    {
        // Disable button during processing
        AddButton.IsEnabled = false;
        
        try
        {
            await _viewModel.AddDownloadAsync();
            
            // Success feedback animation
            AnimateButton(AddButton, Colors.Green);
            ShowNotification("✓ Download đã được thêm!", NotificationType.Success);
        }
        catch (Exception ex)
        {
            // Error feedback animation
            AnimateButton(AddButton, Colors.Red);
            ShowNotification($"✗ Lỗi: {ex.Message}", NotificationType.Error);
        }
        finally
        {
            AddButton.IsEnabled = true;
        }
    }

    private void UrlTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _ = AddDownloadWithAnimation();
        }
    }

    private void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is DownloadTaskItem item)
        {
            if (item.Status == TaskStatus.Downloading)
            {
                _viewModel.PauseDownload(item);
                ShowNotification($"⏸ Đã tạm dừng: {item.FileName}", NotificationType.Info);
            }
            else if (item.Status == TaskStatus.Paused || item.Status == TaskStatus.Error)
            {
                _viewModel.StartDownload(item);
                ShowNotification($"▶ Đang tiếp tục: {item.FileName}", NotificationType.Info);
            }
            
            // Animate button click
            AnimateButtonClick(button);
        }
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is DownloadTaskItem item)
        {
            if (item.Status == TaskStatus.Completed && File.Exists(item.FilePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{item.FilePath}\"",
                        UseShellExecute = true
                    });
                    
                    AnimateButtonClick(button);
                    ShowNotification($"📂 Đã mở: {Path.GetDirectoryName(item.FilePath)}", NotificationType.Success);
                }
                catch (Exception ex)
                {
                    ShowNotification($"✗ Không thể mở file: {ex.Message}", NotificationType.Error);
                }
            }
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow
        {
            Owner = this
        };
        
        if (settingsWindow.ShowDialog() == true)
        {
            // Reload settings in ViewModel
            _viewModel.ReloadSettings();
            ShowNotification("✓ Cài đặt đã được lưu!", NotificationType.Success);
        }
    }

    private void CopyBookmarkButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var bookmarkCode = "javascript:(function(){var url=window.location.href;window.open('http://localhost:4153/add?url='+encodeURIComponent(url),'_blank');})();";
            Clipboard.SetText(bookmarkCode);
            
            if (sender is Button button)
            {
                AnimateButton(button, Colors.Green);
            }
            
            ShowNotification("📋 Đã sao chép bookmark! Kéo vào thanh bookmark của trình duyệt.", NotificationType.Success);
        }
        catch (Exception ex)
        {
            ShowNotification($"✗ Lỗi khi sao chép: {ex.Message}", NotificationType.Error);
        }
    }

    #region Animation Helpers

    private void AnimateButtonClick(Button button)
    {
        var scaleDown = new DoubleAnimation
        {
            From = 1,
            To = 0.95,
            Duration = TimeSpan.FromMilliseconds(100),
            AutoReverse = true,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };

        var scaleTransform = new ScaleTransform(1, 1);
        button.RenderTransform = scaleTransform;
        button.RenderTransformOrigin = new Point(0.5, 0.5);

        scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleDown);
        scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleDown);
    }

    private void AnimateButton(Button button, Color color)
    {
        var originalBrush = button.Background.Clone();
        var colorAnimation = new ColorAnimation
        {
            To = color,
            Duration = TimeSpan.FromMilliseconds(200),
            AutoReverse = true,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };

        button.Background = new SolidColorBrush(color);
        
        colorAnimation.Completed += (s, e) =>
        {
            button.Background = originalBrush;
        };

        button.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
    }

    #endregion

    #region Notification System

    private enum NotificationType
    {
        Success,
        Error,
        Info,
        Warning
    }

    private void ShowNotification(string message, NotificationType type)
    {
        Dispatcher.InvokeAsync(() =>
        {
            var notification = CreateNotification(message, type);
            var container = new Grid
            {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 80, 24, 0)
            };

            container.Children.Add(notification);
            
            // Add to main grid
            var mainGrid = (Grid)this.Content;
            mainGrid.Children.Add(container);
            Grid.SetRowSpan(container, 3);

            // Slide in animation
            var slideIn = new ThicknessAnimation
            {
                From = new Thickness(400, 80, -400, 0),
                To = new Thickness(0, 80, 24, 0),
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            container.BeginAnimation(MarginProperty, slideIn);
            container.BeginAnimation(OpacityProperty, fadeIn);

            // Auto-remove after 4 seconds
            Task.Delay(4000).ContinueWith(_ =>
            {
                Dispatcher.InvokeAsync(() =>
                {
                    var slideOut = new ThicknessAnimation
                    {
                        To = new Thickness(400, 80, -400, 0),
                        Duration = TimeSpan.FromMilliseconds(300),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                    };

                    var fadeOut = new DoubleAnimation
                    {
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(300)
                    };

                    fadeOut.Completed += (s, e) =>
                    {
                        mainGrid.Children.Remove(container);
                    };

                    container.BeginAnimation(MarginProperty, slideOut);
                    container.BeginAnimation(OpacityProperty, fadeOut);
                });
            });
        });
    }

    private Border CreateNotification(string message, NotificationType type)
    {
        var color = type switch
        {
            NotificationType.Success => new LinearGradientBrush(
                Color.FromRgb(16, 185, 129),
                Color.FromRgb(5, 150, 105),
                new Point(0, 0),
                new Point(1, 1)
            ),
            NotificationType.Error => new LinearGradientBrush(
                Color.FromRgb(239, 68, 68),
                Color.FromRgb(220, 38, 38),
                new Point(0, 0),
                new Point(1, 1)
            ),
            NotificationType.Warning => new LinearGradientBrush(
                Color.FromRgb(245, 158, 11),
                Color.FromRgb(217, 119, 6),
                new Point(0, 0),
                new Point(1, 1)
            ),
            _ => new LinearGradientBrush(
                Color.FromRgb(71, 85, 105),
                Color.FromRgb(51, 65, 85),
                new Point(0, 0),
                new Point(1, 1)
            )
        };

        var border = new Border
        {
            Background = color,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(20, 14, 20, 14),
            MaxWidth = 400,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 20,
                ShadowDepth = 4,
                Color = Colors.Black,
                Opacity = 0.4
            }
        };

        var textBlock = new TextBlock
        {
            Text = message,
            Foreground = Brushes.White,
            FontSize = 14,
            FontWeight = FontWeights.Medium,
            TextWrapping = TextWrapping.Wrap
        };

        border.Child = textBlock;
        return border;
    }

    #endregion
}