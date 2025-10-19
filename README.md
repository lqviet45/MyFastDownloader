# MyFastDownloader - Modern Download Manager

A modern, fast, and reliable download manager built with .NET 9 and WPF, featuring multi-segment downloads, browser integration, and a beautiful user interface.

## 🚀 Features

### ✅ Implemented Features

- **Multi-Segment Downloads**: Download files using multiple connections for maximum speed
- **Pause & Resume**: Pause downloads and resume them later without losing progress
- **Browser Integration**: Add downloads directly from your browser using bookmarks
- **Modern UI**: Beautiful, dark-themed interface with Vietnamese language support
- **Real-time Progress**: Live progress tracking with download speed monitoring
- **File Management**: Open downloaded files or their containing folders
- **Download Queue**: Manage multiple downloads with automatic queue processing
- **Resume Support**: Automatic resume capability for interrupted downloads
- **HTTP/HTTPS Support**: Download from HTTP and HTTPS sources
- **Range Request Support**: Efficient downloading with HTTP Range requests

### 🔄 Planned Features

- **Scheduled Downloads**: Schedule downloads for specific times
- **Download Categories**: Organize downloads into categories/folders
- **Speed Limiting**: Control download speed to manage bandwidth
- **Proxy Support**: Download through HTTP/SOCKS proxies
- **Authentication**: Support for HTTP Basic/Digest authentication
- **Batch Downloads**: Download multiple files from URLs or lists
- **Advanced Retry Logic**: Smart retry with exponential backoff
- **Download History**: Keep track of all completed downloads
- **Bandwidth Management**: Global bandwidth controls and scheduling

## 📋 System Requirements

- **OS**: Windows 10/11 (x64)
- **Framework**: .NET 9.0 Runtime (included in self-contained build)
- **RAM**: 100 MB minimum
- **Disk Space**: 50 MB for application + download storage space

## 🛠️ Installation

### Option 1: Download Pre-built Binary (Recommended)
1. Download the latest release from the [Releases](https://github.com/yourusername/MyFastDownloader/releases) page
2. Extract the ZIP file to your desired location
3. Run `MyFastDownloader.exe`

### Option 2: Build from Source
1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/MyFastDownloader.git
   cd MyFastDownloader
   ```

2. Build the application:
   ```bash
   dotnet build --configuration Release
   ```

3. Run the application:
   ```bash
   dotnet run --project MyFastDownloader.csproj
   ```

## 🎯 Usage

### Adding Downloads

#### Method 1: Manual URL Input
1. Launch MyFastDownloader
2. Enter the download URL in the input field
3. Click "Thêm" (Add) button
4. Choose save location in the file dialog
5. Download starts automatically

#### Method 2: Browser Integration
1. Click "📋 Copy Bookmark" in the footer
2. Drag the bookmarklet to your browser's bookmarks bar
3. Click the bookmark on any webpage to add it to MyFastDownloader

#### Method 3: Direct Browser Integration
- Navigate to `http://localhost:4153/add?url=YOUR_URL` to add downloads directly

### Managing Downloads

- **Start/Resume**: Click the play button (▶) to start or resume a download
- **Pause**: Click the pause button (⏸) to pause a download
- **Open File**: Click the folder button (📂) to open the file location
- **View Progress**: Monitor download progress, speed, and file size in real-time

### Download Status

- **🔄 Đang tải** (Downloading): Active download in progress
- **✅ Hoàn thành** (Completed): Download finished successfully
- **⏸ Tạm dừng** (Paused): Download paused by user
- **❌ Lỗi** (Error): Download failed due to error

## 🔧 Configuration

### Default Settings
- **Segments**: 6 parallel connections per download
- **Buffer Size**: 32KB per segment
- **Max Retries**: 5 attempts per segment
- **Server Port**: 4153 (for browser integration)

### Customization
Currently, most settings are hardcoded. Future versions will include:
- Settings dialog for configuration
- Custom download folders
- Bandwidth limits
- Proxy settings

## 🏗️ Architecture

### Core Components

```
MyFastDownloader/
├── Models/           # Data models and entities
│   ├── DownloadTaskItem.cs    # Main download item
│   ├── DownloadMetadata.cs    # Download metadata persistence
│   ├── DownloadSegment.cs     # Individual download segments
│   └── TaskStatus.cs          # Download status enumeration
├── Services/         # Business logic services
│   ├── DownloadManager.cs     # Main download orchestration
│   ├── SegmentedDownloader.cs # Multi-segment download engine
│   └── LocalHttpServer.cs     # Browser integration server
├── ViewModels/       # MVVM view models
│   └── MainViewModel.cs       # Main window view model
├── Views/            # WPF user interface
│   ├── MainWindow.xaml        # Main application window
│   └── MainWindow.xaml.cs     # Code-behind
└── Assets/           # Application resources
    └── app.ico       # Application icon
```

### Key Technologies
- **.NET 9.0**: Latest .NET framework with performance improvements
- **WPF**: Modern Windows desktop UI framework
- **HttpClient**: Async HTTP client with modern features
- **JSON**: Metadata persistence format
- **MVVM Pattern**: Clean separation of concerns

## 🔍 Technical Details

### Multi-Segment Download Algorithm
1. **HEAD Request**: Check if server supports Range requests
2. **Content-Length**: Get total file size
3. **Segment Creation**: Split file into N segments (default: 6)
4. **Parallel Download**: Download each segment concurrently
5. **Progress Tracking**: Aggregate progress from all segments
6. **Resume Support**: Save metadata to resume interrupted downloads

### Performance Optimizations
- **Buffer Management**: 32KB buffers for optimal throughput
- **Connection Reuse**: HttpClient connection pooling
- **Memory Efficiency**: Stream-based processing
- **Disk I/O**: Sequential write operations per segment

## 🐛 Troubleshooting

### Common Issues

#### Download Stuck at 0%
- **Cause**: Server doesn't support Range requests
- **Solution**: The downloader will fall back to single-connection download

#### Browser Integration Not Working
- **Cause**: Port 4153 might be blocked or in use
- **Solution**: 
  1. Check Windows Firewall settings
  2. Ensure no other application is using port 4153
  3. Try restarting the application

#### Downloads Fail Immediately
- **Cause**: Network connectivity or server issues
- **Solution**:
  1. Check internet connection
  2. Verify the download URL is accessible
  3. Try downloading from a different source

### Performance Tips
- **More Segments**: Increase segment count for faster downloads (if server allows)
- **SSD Storage**: Save downloads to SSD for better performance
- **Network**: Use wired connection for stability

## 🤝 Contributing

We welcome contributions! Here's how you can help:

### Development Setup
1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Make your changes
4. Commit: `git commit -m 'Add amazing feature'`
5. Push: `git push origin feature/amazing-feature`
6. Open a Pull Request

### Areas Needing Help
- **UI/UX Improvements**: Better user experience design
- **New Features**: Scheduled downloads, categories, etc.
- **Performance**: Optimize download algorithms
- **Testing**: Unit tests and integration tests
- **Documentation**: Improve guides and API docs

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with .NET 9 and WPF
- Inspired by modern download managers
- Vietnamese language support for local users
- Open source community contributions

## 📞 Support

- **Issues**: Report bugs and request features on [GitHub Issues](https://github.com/yourusername/MyFastDownloader/issues)
- **Discussions**: Join community discussions on [GitHub Discussions](https://github.com/yourusername/MyFastDownloader/discussions)
- **Email**: contact@myfastdownloader.com

---

**Made with ❤️ for fast and reliable downloads**
