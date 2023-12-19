using Kumi.Game.Online.Server;
using Kumi.Server.WebSockets;

namespace Kumi.Server.IO;

public interface IConnectionEnumerable : IEnumerable<Connection>
{
    public void Send<T>(T packet) where T : Packet;
}
