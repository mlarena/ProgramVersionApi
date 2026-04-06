using System.Collections.Concurrent;
using System.Text.Json;

namespace ProgramVersionApi.Services;

public class ProgramService
{
    private readonly string _filePath;
    private ConcurrentDictionary<string, string>? _cache;
    private DateTime _lastLoadTime;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public ProgramService()
    {
        _filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "programs.json");
    }

    private async Task EnsureCacheLoadedAsync()
    {
        if (_cache != null && DateTime.UtcNow - _lastLoadTime < _cacheDuration)
            return;

        await _semaphore.WaitAsync();
        try
        {
            // Double-check after acquiring semaphore
            if (_cache != null && DateTime.UtcNow - _lastLoadTime < _cacheDuration)
                return;

            if (!File.Exists(_filePath))
            {
                _cache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                return;
            }

            var json = await File.ReadAllTextAsync(_filePath);
            using var doc = JsonDocument.Parse(json);
            
            var newCache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var root = doc.RootElement;
            if (root.TryGetProperty("programs", out var programs))
            {
                foreach (var program in programs.EnumerateArray())
                {
                    if (program.TryGetProperty("ProgramName", out var name) &&
                        program.TryGetProperty("Version", out var version))
                    {
                        var nameStr = name.GetString();
                        var versionStr = version.GetString();
                        if (nameStr != null && versionStr != null)
                        {
                            newCache[nameStr] = versionStr;
                        }
                    }
                }
            }

            _cache = newCache;
            _lastLoadTime = DateTime.UtcNow;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<string?> GetVersionByNameAsync(string programName)
    {
        await EnsureCacheLoadedAsync();
        
        if (_cache != null && _cache.TryGetValue(programName, out var version))
        {
            return version;
        }
        
        return null;
    }

    public IEnumerable<string> GetAvailablePrograms()
    {
        return _cache?.Keys ?? Enumerable.Empty<string>();
    }
}