using Kumi.Game.Online.Server.Packets.Dispatch;
using Kumi.Server.Processing;
using Kumi.Server.WebSockets;
using OpenTabletDriver.Plugin.DependencyInjection;

namespace Kumi.Server.Queues.Accounts;

public partial class AccountQueue : QueueProcessor
{
    
    [Queue("accounts:notify")]
    public async Task NotifyAccount(AccountNotification notification)
    {
        IEnumerable<Connection> targets;

        if (notification.Data.Target != null)
        {
            var target = WebsocketServer.GetConnection(notification.Data.Target);
            
            if (target == null)
                return;

            targets = new List<Connection> { target };
        }
        else
        {
            targets = WebsocketServer.GetConnections(c => c.Account.Id == notification.Data.AccountId);
        }
        
        foreach (var target in targets)
        {
            target.Send(new NotificationDispatchPacket
            {
                Data = new NotificationDispatchPacket.NotificationDispatchData
                {
                    Message = notification.Data.Message
                }
            });
        }
    }
}
