using Microsoft.AspNetCore.SignalR;

namespace Kumi.Server.Hubs;

/// <summary>
/// A SignalR hub that handles generic states for the current connection attached to it.
/// </summary>
public class GenericStateHub<T> : Hub<T>
    where T : class
{
    protected GenericStateHub()
    {
        
    }
    
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
}
