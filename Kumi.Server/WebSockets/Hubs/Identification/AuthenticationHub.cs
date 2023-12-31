﻿using Kumi.Game.Online.API.Chat;
using Kumi.Game.Online.Server;
using Kumi.Game.Online.Server.Packets;
using Kumi.Game.Online.Server.Packets.Dispatch;
using Kumi.Server.Database;
using Kumi.Server.Queues.Accounts;
using Kumi.Server.Redis;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using osu.Framework.Allocation;
using StackExchange.Redis;

namespace Kumi.Server.WebSockets.Hubs.Identification;

[Hub(OpCode.Identify, expectsData: true)]
public partial class AuthenticationHub : Hub<IdentifyPacket>
{
    [Resolved]
    private IDatabase cache { get; set; }
    
    [Resolved]
    private DatabaseContext databaseContext { get; set; }
    
    [Resolved]
    private AccountQueue accountQueue { get; set; }

    public override void Handle(Connection conn, IdentifyPacket packet)
    {
        if (packet.Data.Token == null)
            return;
        
        var position= cache.ListPosition("kumi.server:tokens", packet.Data.Token);
        
        if (position == -1)
            return;
        
        //cache.ListRemove("kumi.server:tokens", packet.Data.Token);
        var data = cache.StringGet($"kumi.server:tokens:{packet.Data.Token}");
        
        if (data.IsNullOrEmpty)
            return;

        int id;
        data.TryParse(out id);
        
        var account = databaseContext.Accounts.Find(id);
        
        if (account == null)
            return;
        
        conn.Account = account;
        accountQueue.AddItem("accounts:notify", new AccountNotification()
        {
            Data = new AccountNotification.AccountNotificationData()
            {
                Target = conn.Id.ToString(),
                AccountId = account.Id,
                Message = "Welcome to Kumi! Enjoy your stay!"
            }
        }.ToString());
        
        databaseContext.ChatChannels.ForEachAsync(x =>
        {
            // Send the account the channels they joined.
            var isMember = cache.SetContains($"kumi.server:chat:{x.Id}:participants", account.Id);
            
            if (isMember)
            {
                conn.Send(new ChatChannelAddEvent()
                {
                    Data = new ChatChannelAddEvent.ChatChannelAddEventData()
                    {
                        Channel = new APIChatChannel()
                        {
                            Id = x.Id,
                        }
                    }
                });
            }
        });
    }
}
