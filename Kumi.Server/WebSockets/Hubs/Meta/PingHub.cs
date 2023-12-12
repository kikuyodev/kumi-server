using Kumi.Game.Online.Server.Packets;

namespace Kumi.Server.WebSockets.Hubs.Meta;

public partial class PingHub : Hub<PingPacket>
{
    public override void Handle(Connection conn, PingPacket packet)
    {
        conn.Send(new PongPacket());
    }
}
