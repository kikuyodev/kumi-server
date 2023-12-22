using System.Reflection;
using Kumi.Server.Database;
using Kumi.Server.Redis;
using Kumi.Server.WebSockets;
using Newtonsoft.Json;
using osu.Framework.Allocation;
using StackExchange.Redis;

namespace Kumi.Server.Processing;

/// <summary>
/// A class that processes a set of data from a Redis queue.
/// </summary>
public abstract partial class QueueProcessor : IDependencyInjectionCandidate
{
    public const string QUEUE_PREFIX = "kumi.queue:";

    /// <summary>
    /// The database connection to Redis.
    /// </summary>
    public ConnectionMultiplexer Connection => redis?.Connection;
    
    /// <summary>
    /// Methods that are called when an item is processed.
    /// </summary>
    public List<Tuple<string, MethodInfo>> Methods { get; set; } = new();

    [Resolved]
    private RedisConnection redis { get; set; } = null!;
    
    /// <summary>
    /// The websocket server.
    /// This must be here, because it won't be resolved in any child classes.
    /// </summary>
    [Resolved]
    protected WebsocketServer WebsocketServer { get; private set; } = null!;
    
    /// <summary>
    /// The PostgreSQL database context.
    /// </summary>
    [Resolved]
    protected DatabaseContext Database { get; private set; } = null!;

    /// <summary>
    /// Pre-processes an item from the queue.
    /// </summary>
    /// <param name="item">The item to process.</param>
    protected virtual void PreprocessItem(QueueItem item)
    {
        return;
    }
    
    public void AddItem(string queueName, string item)
    {
        if (string.IsNullOrEmpty(resolveQueueName(queueName)))
            throw new ArgumentNullException(nameof(queueName));

        if (!Methods.Any(m => m.Item1 == resolveQueueName(queueName)))
            throw new ArgumentException("Queue name does not exist.", nameof(queueName));
        
        var db = Connection.GetDatabase();
        db.ListRightPush(resolveQueueName(queueName), item);
    }
    
    public void ClearQueue(string queueName)
    {
        if (string.IsNullOrEmpty(resolveQueueName(queueName)))
            throw new ArgumentNullException(nameof(queueName));

        if (!Methods.Any(m => m.Item1 == resolveQueueName(queueName)))
            throw new ArgumentException("Queue name does not exist.", nameof(queueName));
        
        var db = Connection.GetDatabase();
        db.KeyDelete(resolveQueueName(queueName));
    }
    
    public T GetNextItem<T>(string queueName)
    where T : QueueItem
        => (T)GetNextItem(queueName);
    
    public QueueItem GetNextItem(string queueName)
    {
        if (string.IsNullOrEmpty(resolveQueueName(queueName)))
            throw new ArgumentNullException(nameof(queueName));

        if (!Methods.Any(m => m.Item1 == resolveQueueName(queueName)))
            throw new ArgumentException("Queue name does not exist.", nameof(queueName));
        
        var queue = Methods.First(m => m.Item1 == resolveQueueName(queueName));
        var db = Connection.GetDatabase();
        var item = db.ListLeftPop(queue.Item1);

        if (item.IsNullOrEmpty)
            return null;

        // Get the parameter type.
        var paramType = queue.Item2.GetParameters().First().ParameterType;
        
        // Construct the base queue item.
        var queueItem = (QueueItem)Activator.CreateInstance(paramType);
        
        // Deserialize the item into it's data.
        // Simply set the data with reflection.
        // Convert the data to the generic of the Data property.
        var queueType = queueItem.GetType();
        var dataProperty = queueType.GetProperty("Data");
        var data = JsonConvert.DeserializeObject(item.ToString(), dataProperty.PropertyType);
        
        queueItem.Processor = this;
        queueItem.Queue = queueName;
        queueItem.RawData = item.ToString();
        
        // Set the data property.
        dataProperty.SetValue(queueItem, data);
        
        return (QueueItem)queueItem;
    }
    
    public int GetQueueLength()
    {
        var db = Connection.GetDatabase();
        int len = 0;
        
        // Check every queue.
        foreach (var method in Methods)
        {
            len += (int)db.ListLength(method.Item1);
        }
        
        return len;
    }
    
    public int GetQueueLength(string queueName)
    {
        if (string.IsNullOrEmpty(resolveQueueName(queueName)))
            throw new ArgumentNullException(nameof(queueName));

        if (!Methods.Any(m => m.Item1 == resolveQueueName(queueName)))
            throw new ArgumentException("Queue name does not exist.", nameof(queueName));
        
        var db = Connection.GetDatabase();
        return (int)db.ListLength(resolveQueueName(queueName));
    }
    
    public IEnumerable<Tuple<string, MethodInfo>> GetQueuesWithItems()
    {
        var db = Connection.GetDatabase();
        
        foreach (var method in Methods)
        {
            if (db.ListLength(method.Item1) > 0)
                yield return method;
        }
    }

    public void Run(bool closeAfterProcesssing = false)
    {
        if (closeAfterProcesssing) {
            foreach ((string queueName, MethodInfo method) in GetQueuesWithItems())
            {
                while (GetQueueLength(queueName.Remove(0, QUEUE_PREFIX.Length)) > 0)
                {
                    var len = GetQueueLength(queueName.Remove(0, QUEUE_PREFIX.Length));
                    ForceProcessNextItem(queueName.Remove(0, QUEUE_PREFIX.Length));
                }
            }

            return;
        }
        
        // Listen to the queue.
        while (true)
        {
            if (GetQueueLength() == 0)
            {
                Thread.Sleep(1000);
                continue;
            }
            
            foreach ((string queueName, MethodInfo method) in GetQueuesWithItems())
            {
                while (GetQueueLength(queueName.Remove(0, QUEUE_PREFIX.Length)) > 0)
                {
                    ForceProcessNextItem(queueName.Remove(0, QUEUE_PREFIX.Length));
                }
            }
        }
    }
    
    public void ForceProcessNextItem(string queueName)
    {
        var item = GetNextItem(queueName);
        
        if (item == null)
            return;
        
        var method = Methods.First(m => m.Item1 == resolveQueueName(queueName));
        processItem(queueName, method.Item2, item);
    }

    private string resolveQueueName(string name)
    {
        return QUEUE_PREFIX + name;
    }
    
    private void processItem(string queueName, MethodInfo method, QueueItem item)
    {
        PreprocessItem(item);
        method.Invoke(this, new object[] { item });
        
        // Delete the item from the queue
        var db = Connection.GetDatabase();
    }
}