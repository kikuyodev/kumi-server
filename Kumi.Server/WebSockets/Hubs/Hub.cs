using Kumi.Game.Online.Server;
using osu.Framework.Allocation;

namespace Kumi.Server.WebSockets.Hubs;

/// <summary>
/// A handler for a specific message type sent by a client.
/// </summary>
public abstract partial class Hub<T> : Hub
    where T : class
{
    public abstract void Handle(Connection conn, T packet);
    
    public override void Handle(Connection conn, Packet packet)
    {
        Handle(conn, packet as Packet<T> ?? throw new InvalidCastException());
    }
}

public abstract partial class Hub : IDependencyInjectionCandidate
{
    /// <summary>
    /// The opcode that this hub handles.
    /// </summary>
    public OpCode OpCode { get; set; }
    
    /// <summary>
    /// Whether this hub expects data.
    /// </summary>
    public bool ExpectsData { get; set; }
    
    public abstract void Handle(Connection conn, Packet packet);
}