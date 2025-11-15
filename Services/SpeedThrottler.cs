using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MyFastDownloader.App.Services;

/// <summary>
/// Speed throttler using Token Bucket algorithm for smooth bandwidth limiting
/// </summary>
public class SpeedThrottler
{
    private readonly object _lock = new object();
    private long _maxBytesPerSecond;
    private long _availableTokens;
    private DateTime _lastRefillTime;
    private readonly Stopwatch _stopwatch;
    
    // Token bucket configuration
    private const double RefillIntervalMs = 100; // Refill every 100ms
    private readonly long _bucketCapacity;
    
    public SpeedThrottler(long maxBytesPerSecond)
    {
        _maxBytesPerSecond = maxBytesPerSecond;
        _bucketCapacity = maxBytesPerSecond; // Bucket can hold 1 second worth of data
        _availableTokens = _bucketCapacity;
        _lastRefillTime = DateTime.UtcNow;
        _stopwatch = Stopwatch.StartNew();
    }
    
    /// <summary>
    /// Update the speed limit (thread-safe)
    /// </summary>
    public void UpdateSpeedLimit(long maxBytesPerSecond)
    {
        lock (_lock)
        {
            _maxBytesPerSecond = maxBytesPerSecond;
            // Reset bucket on speed limit change
            _availableTokens = Math.Min(_availableTokens, maxBytesPerSecond);
        }
    }
    
    /// <summary>
    /// Get current speed limit
    /// </summary>
    public long GetSpeedLimit()
    {
        lock (_lock)
        {
            return _maxBytesPerSecond;
        }
    }
    
    /// <summary>
    /// Throttle the download by waiting if necessary
    /// Returns the number of bytes that can be downloaded now
    /// </summary>
    public async Task<int> ThrottleAsync(int requestedBytes, CancellationToken cancellationToken = default)
    {
        if (_maxBytesPerSecond <= 0)
        {
            return requestedBytes; // No throttling
        }
        
        lock (_lock)
        {
            RefillTokens();
            
            if (_availableTokens >= requestedBytes)
            {
                // Can download all requested bytes immediately
                _availableTokens -= requestedBytes;
                return requestedBytes;
            }
            
            if (_availableTokens > 0)
            {
                // Can download partial bytes
                int bytesToDownload = (int)_availableTokens;
                _availableTokens = 0;
                return bytesToDownload;
            }
        }
        
        // No tokens available, need to wait
        double waitTimeMs = CalculateWaitTime(requestedBytes);
        
        if (waitTimeMs > 0)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(waitTimeMs), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
        }
        
        // After waiting, try again
        lock (_lock)
        {
            RefillTokens();
            int bytesToDownload = (int)Math.Min(requestedBytes, _availableTokens);
            _availableTokens -= bytesToDownload;
            return bytesToDownload;
        }
    }
    
    /// <summary>
    /// Refill tokens based on elapsed time
    /// Must be called with lock held
    /// </summary>
    private void RefillTokens()
    {
        var now = DateTime.UtcNow;
        var elapsedMs = (now - _lastRefillTime).TotalMilliseconds;
        
        if (elapsedMs >= RefillIntervalMs)
        {
            // Calculate tokens to add based on elapsed time
            long tokensToAdd = (long)((_maxBytesPerSecond * elapsedMs) / 1000.0);
            
            _availableTokens = Math.Min(_availableTokens + tokensToAdd, _bucketCapacity);
            _lastRefillTime = now;
        }
    }
    
    /// <summary>
    /// Calculate wait time needed before next download
    /// </summary>
    private double CalculateWaitTime(int requestedBytes)
    {
        lock (_lock)
        {
            if (_maxBytesPerSecond <= 0)
                return 0;
            
            // How long until we have enough tokens?
            long tokensNeeded = requestedBytes;
            double secondsNeeded = (double)tokensNeeded / _maxBytesPerSecond;
            return secondsNeeded * 1000; // Convert to milliseconds
        }
    }
    
    /// <summary>
    /// Reset the throttler state
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _availableTokens = _bucketCapacity;
            _lastRefillTime = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Check if throttling is enabled
    /// </summary>
    public bool IsEnabled()
    {
        lock (_lock)
        {
            return _maxBytesPerSecond > 0;
        }
    }
}

/// <summary>
/// Global speed throttler manager for coordinating bandwidth across all downloads
/// </summary>
public class GlobalSpeedThrottler
{
    private static readonly Lazy<GlobalSpeedThrottler> _instance = 
        new Lazy<GlobalSpeedThrottler>(() => new GlobalSpeedThrottler());
    
    public static GlobalSpeedThrottler Instance => _instance.Value;
    
    private SpeedThrottler? _globalThrottler;
    private readonly object _lock = new object();
    private long _globalSpeedLimit = 0;
    private bool _enabled = false;
    
    private GlobalSpeedThrottler() { }
    
    /// <summary>
    /// Configure global speed limit
    /// </summary>
    public void Configure(bool enabled, long maxBytesPerSecond)
    {
        lock (_lock)
        {
            _enabled = enabled;
            _globalSpeedLimit = maxBytesPerSecond;
            
            if (_enabled && _globalSpeedLimit > 0)
            {
                if (_globalThrottler == null)
                {
                    _globalThrottler = new SpeedThrottler(_globalSpeedLimit);
                }
                else
                {
                    _globalThrottler.UpdateSpeedLimit(_globalSpeedLimit);
                }
            }
            else
            {
                _globalThrottler = null;
            }
        }
    }
    
    /// <summary>
    /// Get the global throttler (null if disabled)
    /// </summary>
    public SpeedThrottler? GetThrottler()
    {
        lock (_lock)
        {
            return _globalThrottler;
        }
    }
    
    /// <summary>
    /// Check if global throttling is enabled
    /// </summary>
    public bool IsEnabled()
    {
        lock (_lock)
        {
            return _enabled && _globalSpeedLimit > 0;
        }
    }
    
    /// <summary>
    /// Get current speed limit
    /// </summary>
    public long GetSpeedLimit()
    {
        lock (_lock)
        {
            return _globalSpeedLimit;
        }
    }
}