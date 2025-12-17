# MyFastDownloader 🚀

A modern, high-performance download manager for Windows built with .NET 9.0 and WPF. Download files faster with multi-segment parallel downloads, pause/resume capability, browser integration, and advanced networking features.

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)
![Platform](https://img.shields.io/badge/platform-Windows-0078D4)
![License](https://img.shields.io/badge/license-MIT-green)
![Status](https://img.shields.io/badge/status-v1.1--beta-blue)
![Progress](https://img.shields.io/badge/progress-85%25-brightgreen)

## ✨ Features

### 🎯 Core Features
- **⚡ Multi-Segment Downloads** - Download files up to 7x faster using 8-16 parallel connections
- **⏯️ Pause & Resume** - Pause downloads anytime and resume them later with metadata persistence
- **🌐 Browser Integration** - Add downloads directly from your browser via HTTP server (port 4153)
- **📊 Real-time Progress** - Track download progress with speed monitoring and progress bars
- **🎨 Modern Dark UI** - Beautiful, intuitive interface with Vietnamese language support
- **📁 Download Queue** - Automatic queue management with concurrent download control
- **🔄 Auto-Resume** - Automatic resume for interrupted downloads with HTTP Range support
- **🗑️ Download Management** - Delete, pause, and remove downloads easily

### 🚀 Performance & Optimization (v1.1)
- **🎛️ Speed Limiting** - Control bandwidth with global speed limits
  - Global speed limiting with configurable rates (10 KB/s to 10 MB/s)
  - Token Bucket algorithm for smooth throttling
  - Preset buttons for quick configuration (128KB/s, 256KB/s, 512KB/s, 1MB/s, 5MB/s, 10MB/s)
  - Dynamic updates without restart
  - Per-download speed limit infrastructure ready
  
- **🔐 HTTP Authentication** - Full authentication support
  - Basic, Digest, NTLM, and Bearer token authentication
  - Windows DPAPI encryption for secure credential storage
  - Auto-detection by domain with wildcard support (*.company.com)
  - Test connection before saving
  - Usage statistics tracking
  - Complete credential management UI
  
- **🌐 Proxy Support** - Corporate network ready
  - HTTP, HTTPS, SOCKS4, and SOCKS5 proxy support
  - System proxy auto-detection
  - Proxy authentication with credentials
  - Bypass rules for internal sites (wildcards supported)
  - Multiple proxy configurations (only one active at a time)
  - Encrypted proxy credential storage
  - Test proxy connection feature
  - Usage statistics and last used tracking

- **⚙️ Settings Management** - Comprehensive configuration
  - Persistent settings with JSON storage
  - Default download folder customization
  - Segment count configuration (1-16)
  - Max concurrent downloads (1-10)
  - Always ask for save location option
  
- **💾 Efficient Memory Usage** - Buffer management using ArrayPool
- **🔁 Smart Retry Logic** - Exponential backoff with up to 5 retry attempts

### 🎨 User Experience
- **🌙 Dark Theme** - Modern, eye-friendly dark interface
- **🇻🇳 Vietnamese UI** - Full Vietnamese language support
- **📢 Toast Notifications** - Slide-in animations for status updates
- **🎭 Smooth Animations** - Polished animations throughout the interface
- **📊 Status Badges** - Color-coded status indicators
  - 🟢 Green - Downloading
  - 🟡 Orange - Paused
  - ✅ Bright Green - Completed
  - 🔴 Red - Error
  - ⚫ Gray - Queued/Canceled
- **🔍 Empty State UI** - Helpful guidance when no downloads are active
- **🗑️ Delete Downloads** - Remove downloads from list with smart file handling

## 📋 Requirements

- **OS**: Windows 10/11 (64-bit)
- **Runtime**: .NET 9.0 (included in self-contained build)
- **RAM**: 100-200 MB typical usage
- **Disk**: 50 MB for application + space for downloads
- **Network**: Internet connection required
- **Optional**: Windows Firewall access for browser integration (port 4153)

## 🚀 Installation

### Option 1: Download Release (Recommended)
1. Download the latest release from [Releases](../../releases)
2. Extract the ZIP file to your desired location
3. Run `MyFastDownloader.exe`

### Option 2: Build from Source
```bash
# Clone the repository
git clone https://github.com/yourusername/MyFastDownloader.git
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
3. Click "**Thêm**" (Add) or press **Enter**
4. Choose save location (or use default folder)
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
- **🗑️ Delete**: Click trash icon to remove from list
  - Completed: Removes from list only (keeps file)
  - Paused/Error: Removes from list and deletes incomplete file
  - Downloading: Stops download, removes, and deletes incomplete file

### Configuring Settings

#### Speed Limiting
1. Click **⚙️ Settings** button
2. Enable "**Bật giới hạn tốc độ toàn cục**"
3. Set speed limit:
   - Use slider for adjustment
   - Type exact value in KB/s
   - Click preset buttons for quick selection
4. Click "**✓ Lưu**" (Save)
5. New downloads use configured limit automatically

#### HTTP Authentication
1. Click **⚙️ Settings** → **🔐 Quản Lý Xác Thực**
2. Click "**➕ Thêm Mới**"
3. Enter:
   - Domain (e.g., download.company.com or *.company.com)
   - Username & Password
   - Select authentication type (Basic/Digest/NTLM/Bearer)
4. Click "**🔍 Test**" to verify connection
5. Click "**✓ Lưu**"
6. Authentication applies automatically when downloading from matching domains

#### Proxy Configuration
1. Click **⚙️ Settings** → **🌐 Quản Lý Proxy**
2. Click "**➕ Thêm Mới**"
3. Configure:
   - Proxy Type (HTTP/HTTPS/SOCKS4/SOCKS5/System)
   - Host and Port
   - Authentication (optional)
   - Bypass List (e.g., *.local;192.168.*;*.company.com)
4. Click "**🔍 Test**" to verify proxy
5. Click "**✓ Lưu**"
6. Toggle "**▶**" to activate proxy (only one can be active)

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
│   │   ├── SpeedLimitMode.cs        # Speed limiting modes
│   │   ├── AuthenticationMode.cs    # Authentication types
│   │   └── ProxyType.cs             # Proxy types
│   ├── Settings/                    # Settings models
│   │   └── AppSettings.cs           # Application configuration
│   ├── Auth/                        # Authentication models
│   │   └── Credential.cs            # Credential storage model
│   └── Proxy/                       # Proxy models
│       └── ProxyConfig.cs           # Proxy configuration model
├── Services/                        # Business logic services
│   ├── Core/                        # Core services
│   │   ├── DownloadManager.cs       # Download orchestration
│   │   └── SegmentedDownloader.cs   # Multi-segment download engine
│   ├── Network/                     # Network services
│   │   ├── LocalHttpServer.cs       # Browser integration server
│   │   └── SpeedThrottler.cs        # Bandwidth throttling
│   ├── Storage/                     # Storage services
│   │   └── SettingsService.cs       # Settings persistence
│   ├── Auth/                        # Authentication services
│   │   └── CredentialManager.cs     # Credential management
│   └── Proxy/                       # Proxy services
│       └── ProxyManager.cs          # Proxy configuration management
├── ViewModels/                      # MVVM ViewModels
│   ├── Base/                        # Base classes
│   │   └── ViewModelBase.cs         # Base ViewModel
│   ├── MainViewModel.cs             # Main window ViewModel
│   ├── SettingsViewModel.cs         # Settings window ViewModel
│   └── ProxyViewModel.cs            # Proxy manager ViewModel
├── Views/                           # UI Views
│   ├── MainWindow.xaml              # Main application window
│   ├── SettingsWindow.xaml          # Settings dialog
│   ├── CredentialDialog.xaml        # Add/Edit credential dialog
│   ├── CredentialManagerWindow.xaml # Credential management window
│   ├── ProxySettingsDialog.xaml     # Add/Edit proxy dialog
│   └── ProxyManagerWindow.xaml      # Proxy management window
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
- **Singleton** - HttpClient, settings, managers
- **Observer Pattern** - Event-driven download updates
- **Token Bucket Algorithm** - Smooth bandwidth throttling
- **Strategy Pattern** - Different authentication and proxy types

### Security Features
- **Windows DPAPI Encryption** - Secure credential and proxy storage
- **Custom Entropy** - Additional security layer
- **User-Scoped Encryption** - Credentials only accessible by current user
- **Memory Protection** - Sensitive data cleared after use
- **No Plain-Text Storage** - All sensitive data encrypted at rest

## 🔧 Configuration

Settings are stored in: `%LocalAppData%/MyFastDownloader/`

### Files:
- **settings.json** - Application settings
- **credentials.dat** - Encrypted HTTP authentication credentials
- **proxy_configs.dat** - Encrypted proxy configurations

### Example settings.json:
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
- Vietnamese language support

### 🚀 Version 1.1 (Current - 85% Complete)
- ✅ **Speed Limiting** - Global bandwidth control (100%)
- ✅ **HTTP Authentication** - Basic, Digest, NTLM, Bearer (100%)
- ✅ **Proxy Support** - HTTP, HTTPS, SOCKS4/5 (100%)
- ✅ **Settings Dialog** - Comprehensive configuration (100%)
- ✅ **Delete Downloads** - Remove from list (100%)
- 🔄 **Enhanced Error Handling** - Better error messages (In Progress)

### 📅 Version 1.2 (Planned - Q1 2025)
- [ ] Download categories
- [ ] Scheduled downloads
- [ ] Batch downloads from files
- [ ] Download history tracking
- [ ] Enhanced browser integration (Chrome/Firefox extensions)
- [ ] Per-download speed limits UI
- [ ] Context menu (right-click) options

### 🔮 Version 1.3 (Planned - Q2 2025)
- [ ] File type associations
- [ ] Advanced UI features (graphs, statistics)
- [ ] Mirror support
- [ ] Download acceleration improvements

See [FEATURE_ROADMAP.md](docs/FEATURE_ROADMAP.md) for detailed plans.

## 🎯 Feature Comparison

| Feature | MyFastDownloader | IDM | Free Download Manager |
|---------|------------------|-----|----------------------|
| Multi-segment | ✅ 8 segments | ✅ 32 segments | ✅ 10 segments |
| Speed Limiting | ✅ Global | ✅ Global + Per-download | ✅ Global |
| HTTP Authentication | ✅ Basic/Digest/NTLM | ✅ All types | ✅ Basic/Digest |
| Proxy Support | ✅ HTTP/HTTPS/SOCKS | ✅ All types | ✅ HTTP/SOCKS |
| Scheduled Downloads | ❌ | ✅ | ✅ |
| Browser Integration | ✅ Bookmarklet | ✅ Extensions | ✅ Extensions |
| Categories | ❌ | ✅ | ✅ |
| Modern UI | ✅ Dark theme | ⚠️ Dated | ⚠️ Basic |
| Vietnamese Support | ✅ Full | ❌ English only | ❌ English only |
| Price | **Free** | $24.95 | Free |

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

### Priority Areas for Contribution
1. ✅ Testing and bug reports
2. 🌐 Translation to other languages
3. 📱 Browser extensions (Chrome, Firefox, Edge)
4. 🎨 UI/UX improvements
5. 📝 Documentation improvements

## 🐛 Troubleshooting

### Application won't start
- Ensure .NET 9.0 runtime is installed (or use self-contained build)
- Check Windows Event Viewer for error details
- Try running as administrator

### Downloads fail or are slow
- Check your internet connection
- Disable VPN/proxy temporarily (or configure proxy settings)
- Try reducing segment count in settings
- Check firewall settings
- Verify URL is accessible in browser

### Speed limiting not working
- Ensure speed limit is enabled in settings
- Verify speed limit value is > 0
- Pause and resume existing downloads to apply changes
- For accurate limiting, use speeds ≥ 100 KB/s

### HTTP Authentication issues
- Verify credentials are correct
- Test connection in Credential Manager
- Check authentication type matches server
- Try wildcard domain (*.company.com) if subdomain doesn't work
- Ensure domain matches exactly (case-sensitive)

### Proxy connection fails
- Verify proxy host and port
- Test proxy connection in Proxy Manager
- Check proxy authentication credentials
- Ensure proxy type matches (HTTP vs SOCKS)
- Check bypass list for conflicts
- Try System proxy if company managed

### Browser integration not working
- Check if port 4153 is available
- Allow MyFastDownloader through Windows Firewall
- Verify the bookmarklet was copied correctly
- Try using direct URL entry method instead
- Restart application and try again

### Delete button not working
- Ensure download is not currently active
- Check if file is locked by another process
- Try pausing download first, then delete
- Check file permissions

For more help, see the [User Guide](docs/USER_GUIDE.md) or open an [issue](../../issues).

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **MahApps.Metro** - For the beautiful modern UI components
- **CommunityToolkit.MVVM** - For MVVM helpers
- **Serilog** - For structured logging capabilities
- **Microsoft** - For .NET 9.0 and WPF framework
- All contributors and users of MyFastDownloader

## 📞 Contact & Support

- **GitHub Issues**: [Report bugs or request features](../../issues)
- **Email**: lqviet455@gmail.com
- **Repository**: [github.com/lqviet45/MyFastDownloader](https://github.com/lqviet45/MyFastDownloader)

## 🌟 Star History

If you find this project useful, please consider giving it a star! ⭐

## 📊 Project Stats

![GitHub stars](https://img.shields.io/github/stars/lqviet45/MyFastDownloader?style=social)
![GitHub forks](https://img.shields.io/github/forks/lqviet45/MyFastDownloader?style=social)
![GitHub issues](https://img.shields.io/github/issues/lqviet45/MyFastDownloader)
![GitHub pull requests](https://img.shields.io/github/issues-pr/lqviet45/MyFastDownloader)

---

**Made with ❤️ by lqviet45**

*Download faster, work smarter!* 🚀

---

## 🎓 Learning Resources

This project demonstrates:
- ✅ Modern WPF application development
- ✅ MVVM architecture pattern
- ✅ Dependency Injection in desktop apps
- ✅ Async/await patterns
- ✅ Multi-threaded programming
- ✅ HTTP networking and protocols
- ✅ Security best practices (encryption)
- ✅ UI/UX design principles
- ✅ Token Bucket algorithm
- ✅ Windows DPAPI encryption

Perfect for learning enterprise-grade C# development! 📚
