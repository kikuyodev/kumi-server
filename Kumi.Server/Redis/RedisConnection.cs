using StackExchange.Redis;

namespace Kumi.Server.Redis;

/// <summary>
/// A class that handles the connection to Redis.
/// </summary>
public class RedisConnection
{
    public ConnectionMultiplexer Connection { get; private set; }
    
    public RedisConnection()
    {
        Connection = ConnectionMultiplexer.Connect("localhost");
    }
}
