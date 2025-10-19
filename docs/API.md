# MyFastDownloader API Documentation

This document provides comprehensive API documentation for developers who want to extend or integrate with MyFastDownloader.

## 📚 Table of Contents

- [Core Models](#core-models)
- [Services](#services)
- [ViewModels](#viewmodels)
- [HTTP Server API](#http-server-api)
- [Browser Integration](#browser-integration)
- [Extension Points](#extension-points)

## 🏗️ Core Models

### DownloadTaskItem

Represents a single download task in the application.

```csharp
public class DownloadTaskItem : INotifyPropertyChanged
{
    // Properties
    public Guid Id { get; }                    // Unique identifier
    public string Url { get; set; }            // Download URL
    public string FilePath { get; set; }       // Local save path
    public long TotalSize { get; set; }        // Total file size in bytes
    public long Downloaded { get; set; }       // Downloaded bytes
    public TaskStatus Status { get; set; }     // Current status
    public double SpeedBytesPerSec { get; set; } // Download speed
    public int SegmentsCount { get; set; }     // Number of segments (default: 6)
    public DateTimeOffset CreatedAt { get; set; } // Creation timestamp
    
    // Computed Properties
    public string FileName { get; }            // Extracted filename
    public double ProgressPercent { get; }     // Progress percentage (0-100)
    public string SizeText { get; }            // Formatted size string
    public string SpeedText { get; }           // Formatted speed string
    public string StatusText { get; }          // Localized status text
    public Brush StatusColor { get; }          // Status color for UI
}
```

**Usage Example:**
```csharp
var download = new DownloadTaskItem
{
    Url = "https://example.com/file.zip",
    FilePath = @"C:\Downloads\file.zip",
    SegmentsCount = 8
};
```

### TaskStatus Enum

Represents the current state of a download.

```csharp
public enum TaskStatus
{
    Queued,      // Added to queue, not started
    Downloading, // Currently downloading
    Paused,      // Paused by user
    Completed,   // Successfully completed
    Error,       // Failed due to error
    Canceled     // Canceled by user
}
```

### DownloadMetadata

Internal metadata for download persistence and resume functionality.

```csharp
public class DownloadMetadata
{
    public string Url { get; set; }                    // Original URL
    public string FilePath { get; set; }               // Local file path
    public long TotalSize { get; set; }                // Total file size
    public List<DownloadSegment> Segments { get; set; } // Download segments
}
```

### DownloadSegment

Represents a single segment of a multi-part download.

```csharp
public class DownloadSegment
{
    public long From { get; set; }         // Start byte position
    public long To { get; set; }           // End byte position
    public long Downloaded { get; set; }   // Downloaded bytes in this segment
    public bool Completed { get; }         // Whether segment is complete
}
```

## 🔧 Services

### DownloadManager

Central service for managing download operations.

```csharp
public class DownloadManager
{
    // Events
    public event Action<DownloadTaskItem>? Updated;
    
    // Methods
    public void Add(DownloadTaskItem item);
    public async Task StartAsync(DownloadTaskItem item);
    public void Pause(DownloadTaskItem item);
    public void Cancel(DownloadTaskItem item);
}
```

**Key Features:**
- Manages concurrent downloads
- Handles download lifecycle
- Provides progress updates via events
- Supports pause/resume functionality

**Usage Example:**
```csharp
var manager = new DownloadManager();
manager.Updated += (item) => {
    Console.WriteLine($"Download progress: {item.ProgressPercent}%");
};

var download = new DownloadTaskItem { Url = "https://example.com/file.zip" };
manager.Add(download);
await manager.StartAsync(download);
```

### SegmentedDownloader

Core download engine implementing multi-segment downloads.

```csharp
public class SegmentedDownloader
{
    // Constructor
    public SegmentedDownloader(int maxParallel = 4, HttpMessageHandler? handler = null);
    
    // Events
    public event Action<long, long>? Progress; // (downloaded, total)
    public event Action<double>? Speed;        // bytes/sec
    
    // Methods
    public async Task<long> GetContentLengthAsync(string url, CancellationToken token);
    public async Task StartAsync(string url, string filePath, int segmentsCount, CancellationToken token);
}
```

**Key Features:**
- Multi-segment parallel downloads
- Automatic retry with exponential backoff
- Progress and speed reporting
- Resume capability via metadata persistence
- HTTP Range request support

**Configuration:**
```csharp
var downloader = new SegmentedDownloader(
    maxParallel: 8,  // Maximum concurrent segments
    handler: customHandler  // Optional custom HTTP handler
);
```

### LocalHttpServer

HTTP server for browser integration.

```csharp
public class LocalHttpServer : IDisposable
{
    // Constructor
    public LocalHttpServer(int port = 4153);
    
    // Properties
    public Func<string, Task>? OnAddUrl; // Callback for new URLs
    
    // Methods
    public void Start();
    public void Dispose();
}
```

**Endpoints:**
- `GET /add?url=<encoded_url>` - Add new download from URL

## 🎯 ViewModels

### MainViewModel

Main application view model implementing MVVM pattern.

```csharp
public class MainViewModel : INotifyPropertyChanged
{
    // Properties
    public ObservableCollection<DownloadTaskItem> Items { get; }
    public string UrlInput { get; set; }
    public string Summary { get; }  // Download summary text
    
    // Commands
    public ICommand AddCommand { get; }
    public ICommand StartCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand OpenCommand { get; }
    public ICommand CopyBookmarkCommand { get; }
}
```

**Key Responsibilities:**
- Manages download list UI binding
- Handles user commands
- Coordinates between UI and services
- Provides data for status display

## 🌐 HTTP Server API

### Add Download Endpoint

Add a new download via HTTP request.

**Endpoint:** `GET /add`

**Parameters:**
- `url` (required): URL-encoded download URL

**Example:**
```http
GET http://localhost:4153/add?url=https%3A//example.com/file.zip
```

**Response:**
- `200 OK`: Download added successfully
- `400 Bad Request`: Missing or invalid URL parameter
- `404 Not Found`: Invalid endpoint

**Usage in JavaScript:**
```javascript
// Add current page URL to downloader
const url = encodeURIComponent(window.location.href);
window.open(`http://localhost:4153/add?url=${url}`, '_blank');
```

## 🔌 Browser Integration

### Bookmarklet

JavaScript bookmarklet for browser integration.

```javascript
javascript:(function(){
    var url=window.location.href;
    window.open('http://localhost:4153/add?url='+encodeURIComponent(url),'_blank');
})();
```

### Browser Extension (Future)

Planned browser extension API for seamless integration.

```javascript
// Chrome Extension API (planned)
chrome.runtime.sendMessage({
    action: 'addDownload',
    url: window.location.href
});
```

## 🔧 Extension Points

### Custom HTTP Handler

Implement custom HTTP handling for advanced scenarios.

```csharp
public class CustomHttpHandler : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        // Add custom headers, authentication, proxy support, etc.
        request.Headers.Add("User-Agent", "MyFastDownloader/1.0");
        
        // Forward to default handler
        var handler = new SocketsHttpHandler();
        return await handler.SendAsync(request, cancellationToken);
    }
}

// Usage
var downloader = new SegmentedDownloader(handler: new CustomHttpHandler());
```

### Custom Progress Handler

Implement custom progress reporting.

```csharp
public class CustomProgressHandler
{
    public void OnProgress(long downloaded, long total)
    {
        // Custom progress logic
        Console.WriteLine($"Progress: {downloaded}/{total} bytes");
    }
    
    public void OnSpeed(double bytesPerSecond)
    {
        // Custom speed reporting
        Console.WriteLine($"Speed: {bytesPerSecond / 1024 / 1024:F2} MB/s");
    }
}
```

### Plugin System (Future)

Planned plugin architecture for extending functionality.

```csharp
public interface IDownloadPlugin
{
    string Name { get; }
    string Version { get; }
    
    Task<bool> CanHandleUrl(string url);
    Task<DownloadTaskItem> ProcessUrl(string url);
    Task OnDownloadCompleted(DownloadTaskItem item);
}
```

## 🧪 Testing

### Unit Testing

Example unit tests for core components.

```csharp
[Test]
public async Task DownloadManager_ShouldAddAndStartDownload()
{
    // Arrange
    var manager = new DownloadManager();
    var download = new DownloadTaskItem 
    { 
        Url = "https://httpbin.org/bytes/1024",
        FilePath = Path.GetTempFileName()
    };
    
    // Act
    manager.Add(download);
    await manager.StartAsync(download);
    
    // Assert
    Assert.AreEqual(TaskStatus.Completed, download.Status);
    Assert.IsTrue(File.Exists(download.FilePath));
}
```

### Integration Testing

Test HTTP server integration.

```csharp
[Test]
public async Task HttpServer_ShouldAcceptAddRequest()
{
    // Arrange
    var server = new LocalHttpServer(port: 4154);
    var receivedUrl = "";
    server.OnAddUrl = url => { receivedUrl = url; return Task.CompletedTask; };
    server.Start();
    
    // Act
    var client = new HttpClient();
    var url = "https://example.com/test";
    var response = await client.GetAsync($"http://localhost:4154/add?url={Uri.EscapeDataString(url)}");
    
    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    Assert.AreEqual(url, receivedUrl);
}
```

## 🚀 Performance Considerations

### Memory Management

- Use `IAsyncEnumerable` for large file streaming
- Implement proper disposal patterns
- Monitor memory usage for concurrent downloads

### Network Optimization

- Connection pooling with `HttpClient`
- Configurable timeout settings
- Bandwidth throttling support

### Disk I/O

- Sequential writes per segment
- Buffer size optimization
- SSD vs HDD performance considerations

## 🔒 Security Considerations

### URL Validation

```csharp
public static bool IsValidDownloadUrl(string url)
{
    return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
           (uri.Scheme == "http" || uri.Scheme == "https") &&
           !uri.IsFile;
}
```

### File Path Security

```csharp
public static string SanitizeFilePath(string path)
{
    var invalidChars = Path.GetInvalidFileNameChars();
    return string.Join("_", path.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
}
```

## 📝 Best Practices

### Error Handling

```csharp
try
{
    await downloader.StartAsync(url, filePath, segments, cancellationToken);
}
catch (OperationCanceledException)
{
    // Handle cancellation gracefully
    Log.Info("Download was canceled by user");
}
catch (HttpRequestException ex)
{
    // Handle network errors
    Log.Error($"Network error: {ex.Message}");
}
catch (Exception ex)
{
    // Handle unexpected errors
    Log.Error($"Unexpected error: {ex.Message}");
}
```

### Async/Await Patterns

```csharp
// Good: ConfigureAwait(false) for library code
public async Task StartAsync(DownloadTaskItem item)
{
    await SomeAsyncOperation().ConfigureAwait(false);
}

// Good: Proper cancellation token usage
public async Task DownloadAsync(string url, CancellationToken cancellationToken)
{
    await httpClient.GetAsync(url, cancellationToken);
}
```

---

For more information, see the [User Guide](USER_GUIDE.md) or [Contributing Guidelines](CONTRIBUTING.md).
