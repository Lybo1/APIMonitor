using StackExchange.Redis;

namespace AuthApi.Data.Redis;

public interface IRedisHub
{
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task<T?> GetAsync<T>(string key);
    Task<bool> ExistsAsync(string key);
    Task<bool> RemoveAsync(string key);
    Task<long> IncrementAsync(string key, long value = 1);
    Task<long> DecrementAsync(string key, long value = 1);
    IDatabase Raw { get; }
}