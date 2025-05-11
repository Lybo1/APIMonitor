using AuthApi.Config;

namespace AuthApi.Data.Redis;

/// <summary>
/// Represents the configuration required to connect to a Redis server.
/// </summary>
public sealed class RedisConfig(string host, int port, string? password, bool ssl, int database, int connectionTimeout) :  Settings<RedisConfig>
{
    public RedisConfig() : this("localhost", 6379, null, false, 0, 5000) { }

    public string Host { get; init; } = host ?? throw new ArgumentNullException(nameof(host), "Hostname is required.");
    public int Port { get; init; } = port > 0 ? port : throw new ArgumentOutOfRangeException(nameof(port), "Port is required.");
    public string? Password { get; init; } = password ?? string.Empty;
    public bool Ssl { get; init; } = ssl;
    public int Database { get; init; } = database > 0 ? database : throw new ArgumentOutOfRangeException(nameof(database), "Database is required.");
    public int ConnectionTimeout { get; init; } = connectionTimeout > 0 ? connectionTimeout : throw new ArgumentOutOfRangeException(nameof(connectionTimeout), "Connection timeout is required.");
    
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToConnectionString() =>
        string.Join(',',
            $"{Host}:{Port}",
            $"defaultDatabase={Database}",
            $"ssl={Ssl.ToString().ToLowerInvariant()}",
            $"connectTimeout={ConnectionTimeout}",
            !string.IsNullOrWhiteSpace(Password) ? $"password={Password}" : null
        ).TrimEnd(',');
}