# MyFastDownloader 🚀

A modern, high-performance download manager for Windows built with .NET 9.0 and WPF. Download files faster with multi-segment parallel downloads, pause/resume capability, and browser integration.

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)
![Platform](https://img.shields.io/badge/platform-Windows-0078D4)
![License](https://img.shields.io/badge/license-MIT-green)
![Status](https://img.shields.io/badge/status-active-success)

## ✨ Features

### 🎯 Core Features
- **⚡ Multi-Segment Downloads** - Download files up to 7x faster using 6-16 parallel connections
- **⏯️ Pause & Resume** - Pause downloads anytime and resume them later with metadata persistence
- **🌐 Browser Integration** - Add downloads directly from your browser via HTTP server (port 4153)
- **📊 Real-time Progress** - Track download progress with speed monitoring and progress bars
- **🎨 Modern Dark UI** - Beautiful, intuitive interface with Vietnamese language support
- **📁 Download Queue** - Automatic queue management with concurrent download control
- **🔄 Auto-Resume** - Automatic resume for interrupted downloads with HTTP Range support

### 🚀 Performance & Optimization
- **🎛️ Speed Limiting** - Control bandwidth with global speed limits (Token Bucket algorithm)
  - Global speed limiting with configurable rates (10 KB/s to 10 MB/s)
  - Per-download speed limit support (infrastructure ready)
  - Preset buttons for quick configuration (128KB/s, 256KB/s, 512KB/s, 1MB/s, 5MB/s, 10MB/s)
  - Dynamic updates without restart
- **⚙️ Settings Management** - Persistent configuration with settings dialog
  - Default download folder
  - Segment count configuration (6-16 segments)
  - Max concurrent downloads
  - Speed limit preferences
- **💾 Efficient Memory Usage** - Buffer management using ArrayPool for memory efficiency
- **🔁 Smart Retry Logic** - Exponential backoff with up to 5 retry attempts

### 🎨 User Experience
- **🌙 Dark Theme** - Modern, eye-friendly dark interface
- **🇻🇳 Vietnamese UI** - Full Vietnamese language support
- **📢 Toast Notifications** - Slide-in animations for status updates
- **🎭 Smooth Animations** - Polished animations throughout the interface
- **📊 Status Badges** - Color-coded status indicators for quick identification
- **🔍 Empty State UI** - Helpful guidance when no downloads are active

## 📋 Requirements

- **OS**: Windows 10/11 (64-bit)
- **Runtime**: .NET 9.0 (included in self-contained build)
- **RAM**: 100-200 MB typical usage
- **Disk**: 50 MB for application + space for downloads
- **Network**: Internet connection required

## 🚀 Installation

### Option 1: Download Release (Recommended)
1. Download the latest release from [Releases](../../releases)
2. Extract the ZIP file to your desired location
3. Run `MyFastDownloader.exe`

### Option 2: Build from Source
```bash
# Clone the repository
git clone https://github.com/lqviet45/MyFastDownloader.git
cd MyFastDownloader

# Build the project
dotnet build

# Run the application
dotnet run

# Or publish as self-contained executable
dotnet publish -c Release -r win-x64 --self-contained
```

## 📖 Quick Start

### Adding Downloads

#### Method 1: Direct URL Entry
1. Copy a download URL
2. Paste it into the URL field at the top
3. Click "**Thêm**" (Add)
4. Choose save location
5. Download starts automatically!

#### Method 2: Browser Integration
1. Click "**📋 Copy Bookmark**" in the footer
2. Drag the bookmarklet to your browser's bookmarks bar
3. On any webpage, click the bookmark to add the URL
4. Choose save location
5. Download starts in MyFastDownloader!

#### Method 3: Custom Protocol (Windows Registry)
1. Run `MyFastDownloader.reg` to register custom protocol
2. Links with `myfastdownloader://` will open in the app

### Managing Downloads

- **⏸️ Pause**: Click the pause button to temporarily stop a download
- **▶️ Resume**: Click play to resume a paused download
- **📂 Open Folder**: Click folder icon to open the download location
- **🗑️ Remove**: Remove completed or failed downloads from the list

### Configuring Speed Limits

1. Click the **⚙️ Settings** button (gear icon)
2. Enable "**Bật giới hạn tốc độ toàn cục**" (Enable global speed limit)
3. Set your desired speed limit:
   - Use the slider for quick adjustment
   - Type exact value in the text box
   - Click preset buttons (128KB/s, 256KB/s, etc.)
4. Click "**✓ Lưu**" (Save)
5. New downloads will use the configured limit
6. Pause and resume existing downloads to apply the new limit

## 🏗️ Architecture

### Project Structure
```
MyFastDownloader/
├── App.xaml / App.xaml.cs          # Application entry point with DI setup
├── Models/                          # Data models
│   ├── Core/                        # Core domain models
│   │   ├── DownloadTaskItem.cs      # Download task representation
│   │   ├── DownloadSegment.cs       # Individual download segment
│   │   └── DownloadMetadata.cs      # Metadata for persistence
│   ├── Enums/                       # Enumerations
│   │   ├── TaskStatus.cs            # Download status states
│   │   └── SpeedLimitMode.cs        # Speed limiting modes
│   └── Settings/                    # Settings models
│       └── AppSettings.cs           # Application configuration
├── Services/                        # Business logic services
│   ├── Core/                        # Core services
│   │   ├── DownloadManager.cs       # Download orchestration
│   │   └── SegmentedDownloader.cs   # Multi-segment download engine
│   ├── Network/                     # Network services
│   │   ├── LocalHttpServer.cs       # Browser integration server
│   │   └── SpeedThrottler.cs        # Bandwidth throttling
│   └── Storage/                     # Storage services
│       └── SettingsService.cs       # Settings persistence
├── ViewModels/                      # MVVM ViewModels
│   ├── Base/                        # Base classes
│   │   └── ViewModelBase.cs         # Base ViewModel with INotifyPropertyChanged
│   ├── MainViewModel.cs             # Main window ViewModel
│   └── SettingsViewModel.cs         # Settings window ViewModel
├── Views/                           # UI Views
│   ├── MainWindow.xaml              # Main application window
│   └── SettingsWindow.xaml          # Settings dialog
├── Converters/                      # Value converters for XAML
├── Helpers/                         # Utility helpers
└── Resources/                       # XAML resources (styles, colors)
```

### Key Technologies
- **.NET 9.0** - Modern .NET framework
- **WPF** - Windows Presentation Foundation for UI
- **MahApps.Metro** - Modern UI components library
- **CommunityToolkit.MVVM** - MVVM helpers and commands
- **Microsoft.Extensions.DependencyInjection** - Dependency injection
- **Serilog** - Structured logging

### Design Patterns
- **MVVM (Model-View-ViewModel)** - Clean separation of concerns
- **Dependency Injection** - Loose coupling and testability
- **Singleton** - Global speed throttler instance
- **Observer Pattern** - Event-driven download updates
- **Token Bucket Algorithm** - Smooth bandwidth throttling

## 🔧 Configuration

Settings are stored in: `%LocalAppData%/MyFastDownloader/settings.json`

Example configuration:
```json
{
  "DefaultDownloadFolder": "C:\\Users\\YourName\\Downloads",
  "AlwaysAskSaveLocation": false,
  "DefaultSegmentCount": 8,
  "MaxConcurrentDownloads": 3,
  "EnableSpeedLimit": true,
  "GlobalSpeedLimitBytesPerSec": 1048576
}
```

## 📚 Documentation

- [**User Guide**](docs/USER_GUIDE.md) - Comprehensive usage instructions
- [**Feature Roadmap**](docs/FEATURE_ROADMAP.md) - Planned features and timeline
- [**API Documentation**](docs/API.md) - Developer API reference
- [**IDM Feature Analysis**](docs/IDM_FEATURE_ANALYSIS.md) - Comparison with IDM
- [**Progress Tracker**](docs/PROGRESS_TRACKER.md) - Development progress
- [**Changelog**](Changelog.md) - Version history and changes

## 🛣️ Roadmap

### ✅ Version 1.0 (Released)
- Multi-segment download engine
- Pause/Resume functionality
- Browser integration
- Modern dark UI
- Speed limiting (global)

### 🚧 Version 1.1 (In Progress)
- [ ] HTTP Authentication (Basic, Digest, NTLM)
- [ ] Proxy support (HTTP/HTTPS/SOCKS)
- [ ] Enhanced error handling
- [ ] Per-download speed limits UI

### 📅 Version 1.2 (Planned)
- [ ] Download categories
- [ ] Scheduled downloads
- [ ] Batch downloads from files
- [ ] Download history tracking
- [ ] Enhanced browser integration

See [FEATURE_ROADMAP.md](docs/FEATURE_ROADMAP.md) for detailed plans.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

### Development Setup
1. Fork the repository
2. Clone your fork
3. Create a feature branch: `git checkout -b feature/amazing-feature`
4. Make your changes
5. Commit: `git commit -m 'Add amazing feature'`
6. Push: `git push origin feature/amazing-feature`
7. Open a Pull Request

### Coding Standards
- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Write clean, maintainable code
- Test your changes thoroughly

## 🐛 Troubleshooting

### Application won't start
- Ensure .NET 9.0 runtime is installed (or use self-contained build)
- Check Windows Event Viewer for error details
- Try running as administrator

### Downloads fail or are slow
- Check your internet connection
- Disable VPN/proxy temporarily
- Try reducing segment count in settings
- Check firewall settings

### Speed limiting not working
- Ensure speed limit is enabled in settings
- Verify speed limit value is > 0
- Pause and resume existing downloads to apply changes
- For accurate limiting, use speeds ≥ 100 KB/s

### Browser integration not working
- Check if port 4153 is available
- Allow MyFastDownloader through Windows Firewall
- Verify the bookmarklet was copied correctly
- Try using direct URL entry method instead

For more help, see the [User Guide](docs/USER_GUIDE.md) or open an [issue](../../issues).

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **MahApps.Metro** - For the beautiful modern UI components
- **CommunityToolkit.MVVM** - For MVVM helpers
- **Serilog** - For structured logging capabilities
- All contributors and users of MyFastDownloader

## 📞 Contact & Support

- **GitHub Issues**: [Report bugs or request features](../../issues)
- **Email**: lqviet455@gmail.com
- **Repository**: [github.com/lqviet45/MyFastDownloader](https://github.com/lqviet45/MyFastDownloader)

---

**Made with ❤️ by lqviet45**

*Download faster, work smarter!* 🚀
