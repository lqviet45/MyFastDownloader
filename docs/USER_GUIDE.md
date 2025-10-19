# MyFastDownloader User Guide

Welcome to MyFastDownloader! This comprehensive user guide will help you get the most out of your download manager.

## 📖 Table of Contents

- [Getting Started](#getting-started)
- [Basic Usage](#basic-usage)
- [Browser Integration](#browser-integration)
- [Managing Downloads](#managing-downloads)
- [Advanced Features](#advanced-features)
- [Troubleshooting](#troubleshooting)
- [Tips & Tricks](#tips--tricks)

## 🚀 Getting Started

### First Launch

1. **Launch MyFastDownloader** - Double-click the application icon
2. **Interface Overview** - Familiarize yourself with the main window:
   - **Header**: URL input field and add button
   - **Main Area**: Download list with progress tracking
   - **Footer**: Information and bookmark copy button

3. **Set Up Browser Integration** (Optional):
   - Click "📋 Copy Bookmark" in the footer
   - Drag the bookmarklet to your browser's bookmarks bar
   - You can now add downloads directly from any webpage

### System Requirements

- **Windows 10/11** (64-bit)
- **Internet Connection** for downloads
- **Available Disk Space** for downloaded files
- **Firewall Access** for browser integration (port 4153)

## 🎯 Basic Usage

### Adding Downloads

#### Method 1: Manual URL Entry
1. **Copy the download URL** from your browser or source
2. **Paste it** into the URL input field at the top
3. **Click "Thêm"** (Add) button
4. **Choose save location** in the file dialog
5. **Download starts automatically**

#### Method 2: Browser Bookmark
1. **Navigate** to any webpage with a download link
2. **Click your MyFastDownloader bookmark**
3. **Choose save location** when prompted
4. **Download starts automatically**

#### Method 3: Drag & Drop (Future Feature)
*Coming in version 1.2 - drag files directly to the application*

### Understanding the Download List

Each download shows:

| Column | Description |
|--------|-------------|
| **Tên file** | File name and size |
| **Trạng thái** | Current status (Đang tải/Completed/Paused) |
| **Tiến trình** | Progress bar and percentage |
| **Tốc độ** | Current download speed |
| **Hành động** | Control buttons (Play/Pause/Open) |

### Download Status Meanings

| Status | Icon | Description |
|--------|------|-------------|
| **Đang tải** | 🔄 | Download in progress |
| **Hoàn thành** | ✅ | Download completed successfully |
| **Tạm dừng** | ⏸ | Download paused by user |
| **Lỗi** | ❌ | Download failed due to error |

## 🌐 Browser Integration

### Setting Up Bookmarks

1. **Copy the bookmarklet**:
   - Click "📋 Copy Bookmark" in the footer
   - A JavaScript bookmarklet is copied to your clipboard

2. **Add to browser**:
   - **Chrome/Edge**: Right-click bookmarks bar → Paste
   - **Firefox**: Right-click bookmarks toolbar → Paste
   - **Safari**: Drag from bookmarks menu to toolbar

3. **Test the integration**:
   - Visit any webpage with downloadable content
   - Click your MyFastDownloader bookmark
   - The download should be added automatically

### Browser Extension (Planned)

Future versions will include dedicated browser extensions for:
- **One-click downloads** from context menus
- **Automatic detection** of downloadable files
- **Download monitoring** directly in browser
- **Site-specific settings** and preferences

## 🎮 Managing Downloads

### Basic Controls

#### Starting Downloads
- **Click the play button (▶)** to start a new download
- **Click the play button (▶)** to resume a paused download

#### Pausing Downloads
- **Click the pause button (⏸)** to pause an active download
- Downloads can be resumed later without losing progress

#### Opening Files
- **Click the folder button (📂)** to open the file location in Windows Explorer
- **Completed downloads** can be opened directly from their location

### Queue Management

#### Automatic Queue Processing
- Downloads start automatically when added
- Multiple downloads run concurrently
- System automatically manages bandwidth distribution

#### Manual Queue Control (Future)
*Planned features for version 1.2:*
- Priority-based queue management
- Scheduled download start times
- Bandwidth allocation per download

### Progress Monitoring

#### Real-time Information
- **Progress Bar**: Visual representation of download progress
- **Percentage**: Exact completion percentage
- **Speed**: Current download speed in KB/s or MB/s
- **File Size**: Total file size and downloaded amount

#### Speed Optimization
- **Multi-segment downloads**: Files are downloaded in 6 parallel segments
- **Automatic optimization**: System adjusts based on server capabilities
- **Resume capability**: Interrupted downloads resume from where they left off

## ⚙️ Advanced Features

### Download Settings

#### Segment Configuration
- **Default**: 6 parallel segments per download
- **Automatic**: System detects optimal segment count
- **Server dependent**: Some servers limit concurrent connections

#### File Management
- **Auto-naming**: Files are automatically named based on URL
- **Custom naming**: You can rename files when saving
- **Folder organization**: Choose custom download folders

### Browser Integration Settings

#### HTTP Server Configuration
- **Port**: Default port 4153 for browser integration
- **Security**: Local-only access for security
- **Compatibility**: Works with all major browsers

### Performance Optimization

#### Network Optimization
- **Connection pooling**: Efficient HTTP connection management
- **Buffer management**: Optimized for different network speeds
- **Retry logic**: Automatic retry for failed segments

#### System Resource Management
- **Memory efficient**: Minimal memory usage during downloads
- **CPU optimization**: Low CPU usage for background downloads
- **Disk I/O optimization**: Sequential writes for better performance

## 🛠️ Troubleshooting

### Common Issues and Solutions

#### Download Stuck at 0%
**Symptoms**: Download shows 0% progress and doesn't start
**Causes**:
- Server doesn't support multi-segment downloads
- Network connectivity issues
- Firewall blocking connections

**Solutions**:
1. Check your internet connection
2. Try downloading from a different source
3. Temporarily disable antivirus/firewall
4. Restart the application

#### Browser Integration Not Working
**Symptoms**: Bookmark doesn't add downloads
**Causes**:
- Port 4153 blocked by firewall
- Another application using the port
- JavaScript disabled in browser

**Solutions**:
1. Check Windows Firewall settings
2. Restart MyFastDownloader
3. Try a different browser
4. Check if JavaScript is enabled

#### Downloads Fail Immediately
**Symptoms**: Downloads fail within seconds of starting
**Causes**:
- Invalid or broken URLs
- Server authentication required
- Network proxy issues

**Solutions**:
1. Verify the download URL is correct
2. Try downloading manually in browser first
3. Check if the server requires authentication
4. Contact the file provider

#### Slow Download Speed
**Symptoms**: Downloads are slower than expected
**Causes**:
- Server bandwidth limitations
- Network congestion
- Antivirus scanning interference

**Solutions**:
1. Try downloading during off-peak hours
2. Temporarily disable real-time antivirus scanning
3. Check if other applications are using bandwidth
4. Try a different download mirror (if available)

### Performance Issues

#### High Memory Usage
**Symptoms**: Application uses excessive RAM
**Solutions**:
1. Restart the application periodically
2. Limit concurrent downloads
3. Close unnecessary applications

#### Slow Application Response
**Symptoms**: UI becomes unresponsive during downloads
**Solutions**:
1. Reduce number of concurrent downloads
2. Increase system RAM if possible
3. Close other resource-intensive applications

### Network Issues

#### Proxy Configuration (Future)
*Planned for version 1.1:*
- HTTP/HTTPS proxy support
- SOCKS proxy support
- Automatic proxy detection

#### Authentication (Future)
*Planned for version 1.1:*
- HTTP Basic authentication
- HTTP Digest authentication
- Credential management

## 💡 Tips & Tricks

### Maximizing Download Speed

1. **Use wired connection** instead of WiFi for large downloads
2. **Download during off-peak hours** for better server performance
3. **Close bandwidth-heavy applications** (streaming, gaming)
4. **Use SSD storage** for faster file writes
5. **Ensure good internet connection** with stable speed

### Organizing Downloads

1. **Create dedicated folders** for different file types
2. **Use descriptive filenames** when saving files
3. **Monitor download history** to avoid duplicates
4. **Clean up completed downloads** regularly

### Browser Integration Tips

1. **Test bookmarks** on different websites
2. **Use bookmarks for media downloads** (videos, images, documents)
3. **Keep MyFastDownloader running** for instant bookmark functionality
4. **Create multiple bookmarks** for different download types

### System Optimization

1. **Exclude download folders** from antivirus real-time scanning
2. **Use dedicated download drive** if possible
3. **Monitor disk space** before starting large downloads
4. **Regular system maintenance** for optimal performance

## 🔮 Future Features Preview

### Coming in Version 1.1
- **Speed limiting**: Control download speed
- **Proxy support**: Download through proxies
- **Authentication**: Support for password-protected downloads

### Coming in Version 1.2
- **Download categories**: Organize downloads by type
- **Scheduled downloads**: Download at specific times
- **Batch downloads**: Download multiple files at once
- **Download history**: Track all completed downloads

### Coming in Version 1.3
- **File type associations**: Auto-open downloaded files
- **Download acceleration**: Advanced speed optimization
- **Enhanced UI**: Better statistics and graphs

### Coming in Version 1.4
- **Virus scanning**: Automatic security checks
- **Browser extensions**: Native browser integration
- **Cloud integration**: Direct cloud storage uploads

## 📞 Getting Help

### Documentation
- **README.md**: Basic installation and setup
- **API.md**: Developer documentation
- **Feature Roadmap**: Planned features and timeline

### Community Support
- **GitHub Issues**: Report bugs and request features
- **GitHub Discussions**: Ask questions and share tips
- **Email Support**: contact@myfastdownloader.com

### Reporting Issues
When reporting problems, please include:
1. **MyFastDownloader version**
2. **Windows version**
3. **Steps to reproduce the issue**
4. **Error messages** (if any)
5. **Download URL** (if relevant)

---

**Happy Downloading! 🚀**

*This guide is updated regularly. Last updated: January 2024*
