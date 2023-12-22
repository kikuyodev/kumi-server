using Kumi.Game.Online.API.Chat;
using Kumi.Game.Online.Server.Packets.Dispatch;
using Kumi.Server.Processing;
using Newtonsoft.Json;

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
            
            case "message":
                onMessage(turnEventIntoAnother<APIChatMessage>(chatEvent));
                break;
        }
    }

    private void onMessage(ChatEvent<APIChatMessage> chatMessageEvent)
    {
        // Get all of the participants in the channel.
        var participants = Connection.GetDatabase().SetMembers($"kumi.server:chat:{chatMessageEvent.Data.Channel.Id}:participants");
        var messagePacket = new ChatMessageEvent()
        {
            Data = new ChatMessageEvent.ChatMessageEventData()
            {
                Message = chatMessageEvent.Data.Data
            }
        };
        
        // Send the message to all of the participants.
        foreach (var participant in participants)
        {
            var connections = WebsocketServer.GetConnections(c => c.Account.Id == participant);
            
            if (connections.Count() == 0)
                continue;
            
            connections.Send(messagePacket);
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
    
    private ChatEvent<T> turnEventIntoAnother<T>(ChatEvent baseEvent)
    {
        // can't just cast because of the generic type
        // likely need to json serialize and deserialize
        ChatEvent<T> newEvent = new ChatEvent<T>()
        {
            Data = new ChatEvent<T>.ChatEventData<T>()
            {
                Type = baseEvent.Data.Type,
                Channel = baseEvent.Data.Channel,
                Account = baseEvent.Data.Account,
                Data = JsonConvert.DeserializeObject<T>(baseEvent.RawData)
            }
        };

        return newEvent;
    }
}
