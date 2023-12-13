using Kumi.Game.Online.Server;
using Kumi.Game.Online.Server.Packets;

namespace Kumi.Server.WebSockets.Hubs.Meta;

[Hub(OpCode.Ping)]
public partial class PingHub : Hub<PingPacket>
{
    public override void Handle(Connection conn, PingPacket packet)
    {
        conn.Send(new PongPacket());
    }
}
