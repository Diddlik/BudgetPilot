using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BudgetPilot.Mobile.Services;

public sealed class OfflineCache
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task StoreAsync<T>(string instance, string key, T value, CancellationToken cancellationToken)
    {
        var entry = new CacheEntry<T>(DateTime.UtcNow, value);
        await gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(CacheDirectory);
            await using var stream = File.Create(PathFor(instance, key));
            await JsonSerializer.SerializeAsync(stream, entry, options, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<(T Value, DateTime CachedAt)?> TryReadAsync<T>(
        string instance,
        string key,
        CancellationToken cancellationToken)
    {
        var path = PathFor(instance, key);
        if (!File.Exists(path))
        {
            return null;
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            await using var stream = File.OpenRead(path);
            var entry = await JsonSerializer.DeserializeAsync<CacheEntry<T>>(stream, options, cancellationToken);
            return entry is null ? null : (entry.Value, entry.CachedAtUtc);
        }
        catch (JsonException)
        {
            return null;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (Directory.Exists(CacheDirectory))
            {
                Directory.Delete(CacheDirectory, recursive: true);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<OfflineCacheInfo> GetInfoAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (!Directory.Exists(CacheDirectory))
            {
                return new(0, 0);
            }

            var files = Directory.EnumerateFiles(CacheDirectory, "*.json").Select(path => new FileInfo(path)).ToList();
            return new(files.Count, files.Sum(file => file.Length));
        }
        finally
        {
            gate.Release();
        }
    }

    private static string CacheDirectory => Path.Combine(FileSystem.AppDataDirectory, "read-cache");

    private static string PathFor(string instance, string key)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{instance}|{key}"));
        return Path.Combine(CacheDirectory, $"{Convert.ToHexString(bytes)}.json");
    }

    private sealed record CacheEntry<T>(DateTime CachedAtUtc, T Value);
}

public sealed record OfflineCacheInfo(int EntryCount, long SizeInBytes);
