using Kumi.Game.Online.Server.Packets.Dispatch;
using Kumi.Server.Processing;

namespace Kumi.Server.Queues;

public partial class ChatQueue : QueueProcessor
{
    [Queue("chat:events")]
    public async Task ProcessChatEvent(ChatEvent chatEvent)
    {
        switch (chatEvent.Data.Type)
        {
            case "join":
                onJoin(chatEvent.Data);
                break;
        }
    }
    
    private void onJoin(ChatEvent.ChatEventData data)
    {
        // Find the account that joined the channel, if they exist.
        var connections = WebsocketServer.GetConnections(c => c.Account.Id == data.Account.Id);
        
        if (connections.Count() == 0)
            return;
        
        var joinPacket = new ChatChannelAddEvent()
        {
            Data = new ChatChannelAddEvent.ChatChannelAddEventData()
            {
                Channel = data.Channel
            }
        };
        
        connections.Send(joinPacket);
    }
}
