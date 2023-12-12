using Newtonsoft.Json;

namespace Kumi.Server.Processing;

/// <summary>
/// An item in a Redis queue.
/// </summary>
public class QueueItem<T> : QueueItem
    where T : notnull
{
    /// <summary>
    /// The data associated with this queue item.
    /// </summary>
    public T Data { get; set; }

    public override void Requeue()
    {
        this.Processor?.AddItem(this.Queue, JsonConvert.SerializeObject(this.Data));
    }

    public override string ToString() => JsonConvert.SerializeObject(this.Data);
}

public class QueueItem
{
    /// <summary>
    /// The queue processor that this item belongs to.
    /// </summary>
    [JsonIgnore]
    public QueueProcessor? Processor { get; set; } = null!;
    
    /// <summary>
    /// The queue that this item belongs to.
    /// </summary>
    [JsonIgnore]
    public string Queue { get; set; } = string.Empty;
    
    public virtual void Requeue()
    {
        this.Processor?.AddItem(this.Queue, JsonConvert.SerializeObject(this));
    }
}