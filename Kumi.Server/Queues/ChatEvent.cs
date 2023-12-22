using Kumi.Game.Online.API.Accounts;
using Kumi.Game.Online.API.Chat;
using Kumi.Server.Processing;
using Newtonsoft.Json;

namespace Kumi.Server.Queues;

public class ChatEvent<T> : QueueItem<ChatEvent<T>.ChatEventData<T>>
{
    public class ChatEventData<T> : ChatEvent.ChatEventData
    {
        /// <summary>
        /// The data associated with this event.
        /// </summary>
        [JsonProperty("data")]
        public T Data { get; set; }
    }
}

public class ChatEvent : QueueItem<ChatEvent.ChatEventData>
{
    public class ChatEventData 
    {
        /// <summary>
        /// The type of event sent.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }
        
        /// <summary>
        /// The channel this event is associated with.
        /// </summary>
        [JsonProperty("channel")]
        public APIChatChannel Channel { get; set; }
        
        /// <summary>
        /// The account that sent the event.
        /// </summary>
        [JsonProperty("account")]
        public APIAccount Account { get; set; } 
        
    }
}
