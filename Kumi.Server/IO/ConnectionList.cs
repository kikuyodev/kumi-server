using Kumi.Game.Online.Server;
using Kumi.Server.WebSockets;

namespace Kumi.Server.IO;

public class ConnectionList : List<Connection>, IConnectionEnumerable
{
    public ConnectionList()
    {
    }
    
    public ConnectionList(IEnumerable<Connection> connections)
        : base(connections)
    {
    }
    
    public void Send<T>(T packet) where T : Packet
    {
        foreach (var connection in this)
        {
            connection.Send(packet);
        }
    }
}
