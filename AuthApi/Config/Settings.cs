namespace AuthApi.Config;

/// <summary>
/// Defines the base configuration settings for configuration classes with shared properties.
/// </summary>
/// <typeparam name="T">A derived type that inherits from the <see cref="Settings{T}"/> class, used to ensure conformity and provide default instantiation.</typeparam>
public abstract class Settings<T> where T : Settings<T>, new()
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool Ssl { get; set; } = false;
    public int ConnectionTimeout { get; set; } = 5000;

    /// <summary>
    /// Validates the configuration settings for the current instance.
    /// Ensures that all mandatory properties, such as Host, Port, and ConnectionTimeout,
    /// meet the required criteria.
    /// </summary>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when the Host property is null or empty.
    /// </exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// Thrown when the Port or ConnectionTimeout properties are less than the required minimum values.
    /// </exception>
    public virtual void Validate()
    {
        ArgumentNullException.ThrowIfNull(Host, "Host is required.");
        ArgumentOutOfRangeException.ThrowIfLessThan(Port, 0, "Port is required.");
        ArgumentOutOfRangeException.ThrowIfLessThan(ConnectionTimeout, 0, "Timeout is required.");
    }
    
    public abstract string ToConnectionString();
}