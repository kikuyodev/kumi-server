using Kumi.Server.Processing;
using Newtonsoft.Json;

namespace Kumi.Server.Queues.Accounts;

public class AccountNotification : QueueItem<AccountNotification.AccountNotificationData>
{
    public class AccountNotificationData
    {
        /// <summary>
        /// The target connection to send the notification to, if any.
        /// </summary>
        [JsonProperty("target")]
        public string? Target { get; set; }
        
        /// <summary>
        /// The account ID to send the notification to.
        /// </summary>
        [JsonProperty("account_id")]
        public int AccountId { get; set; }
        
        /// <summary>
        /// The message to send to the account.
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
