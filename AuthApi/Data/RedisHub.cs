using StackExchange.Redis;

namespace AuthApi.Data;

public class RedisHub(IConnectionMultiplexer connectionMultiplexer, IDatabase database)
{
    private readonly IConnectionMultiplexer connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
    private readonly IDatabase database = database ?? throw new ArgumentNullException(nameof(database));
}