using StackExchange.Redis;
using IDatabase = StackExchange.Redis.IDatabase;

namespace AuthApi.Data.Redis;

public sealed class RedisHub : IRedisHub
{
    private readonly Lazy<ConnectionMultiplexer> lazyConnection;
    private readonly IDatabase db;

    public RedisHub(RedisConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        config.Validate();

        this.lazyConnection = new(() => ConnectionMultiplexer.Connect(config.ToConnectionString()));
        this.db = this.lazyConnection.Value.GetDatabase(config.Database);
    }


    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        throw new NotImplementedException();
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> ExistsAsync(string key)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> RemoveAsync(string key)
    {
        throw new NotImplementedException();
    }

    public async Task<long> IncrementAsync(string key, long value = 1)
    {
        throw new NotImplementedException();
    }

    public async Task<long> DecrementAsync(string key, long value = 1)
    {
        throw new NotImplementedException();
    }

    public IDatabase Raw { get; set; }
}