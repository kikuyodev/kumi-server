using Kumi.Server.Processing;

namespace Kumi.Server.Queues.Accounts;

public partial class AccountQueue : QueueProcessor
{
    [Queue("accounts:notify")]
    public async Task NotifyAccount(AccountNotification notification)
    {
        return;
    }
}
