using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FinanceApp.Desktop.Services;

public sealed class CacheService
{
    private readonly string _cacheDirectory;

    public CacheService()
    {
        _cacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FinanceApp",
            "Cache"
        );

        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
    }

    public async Task<T?> GetAsync<T>(string key, TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(key);
        if (!File.Exists(filePath))
        {
            return default;
        }

        try
        {
            // Check file age
            var fileInfo = new FileInfo(filePath);
            if (DateTime.UtcNow - fileInfo.LastWriteTimeUtc > maxAge)
            {
                // Expired
                File.Delete(filePath);
                return default;
            }

            using var stream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: cancellationToken);
        }
        catch
        {
            // If corruption occurs or access is locked, fail gracefully
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T data, CancellationToken cancellationToken = default)
    {
        if (data is null) return;
        var filePath = GetFilePath(key);

        try
        {
            using var stream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(stream, data, cancellationToken: cancellationToken);
        }
        catch
        {
            // Fail silently to keep application running offline if disk writes are locked
        }
    }

    public Task InvalidateAsync(string key, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(key);
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Fail silently
        }
        return Task.CompletedTask;
    }

    public Task InvalidateAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (Directory.Exists(_cacheDirectory))
            {
                foreach (var file in Directory.GetFiles(_cacheDirectory))
                {
                    File.Delete(file);
                }
            }
        }
        catch
        {
            // Fail silently
        }
        return Task.CompletedTask;
    }

    private string GetFilePath(string key)
    {
        // Safe filename replacement
        var safeKey = string.Join("_", key.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_cacheDirectory, $"{safeKey}.json");
    }
}
