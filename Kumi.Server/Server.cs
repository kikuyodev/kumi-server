using System.Diagnostics;
using System.Reflection;
using Kumi.Server.Database;
using Kumi.Server.Processing;
using Kumi.Server.Redis;
using Kumi.Server.WebSockets;
using osu.Framework.Allocation;
using osu.Framework.Threading;

namespace Kumi.Server;

public class Server
{
    /// <summary>
    /// The dependencies across the entire application.
    /// </summary>
    public DependencyContainer Dependencies { get; set; } = new();
    
    public RedisConnection Redis { get; set; }
    
    public WebsocketServer WebsocketServer { get; set; } = null!;

    public void Run()
    {
        Dependencies.CacheAs(this);
        Dependencies.CacheAs(typeof(RedisConnection), Redis = new RedisConnection());
        Dependencies.CacheAs(Redis.Connection.GetDatabase());
        Dependencies.CacheAs(new DatabaseContext());
        
        // Start a websocket server.
        Dependencies.CacheAs(WebsocketServer = new WebsocketServer(3403));
        Dependencies.Inject(WebsocketServer);

        // Get all the queue processors.
        var queue_processors = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.BaseType != null && t.BaseType == typeof(QueueProcessor))
            .ToList();
        
        foreach (var qp  in queue_processors)
        {
            // Ignore abstract classes.
            if (qp.IsAbstract)
                continue;
                
            var instance = (QueueProcessor)Activator.CreateInstance(qp);
            
            // Get every method that has the Queue attribute.
            var methods = qp.GetMethods()
                .Where(m => m.GetCustomAttributes(typeof(QueueAttribute), false).Length > 0)
                .ToList();

            foreach (var method in methods)
            {
                // Get the queue attribute.
                var queue = (QueueAttribute)method.GetCustomAttributes(typeof(QueueAttribute), false)[0];
                
                if (queue == null)
                    continue;
                
                instance.Methods.Add(new Tuple<string, MethodInfo>(QueueProcessor.QUEUE_PREFIX + queue.QueueName, method));
            }
            
            Dependencies.CacheAs(instance.GetType(), instance);
            Dependencies.Inject(instance);
            
            var thread = new Thread(() => instance.Run(false));
            thread.IsBackground = true;
            thread.Start();
        }
        
        WebsocketServer.Start();
    }
}
