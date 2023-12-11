namespace Kumi.Server.Processing;

[AttributeUsage(AttributeTargets.Method)]
public class QueueAttribute : Attribute
{
    public string QueueName { get; }
    
    public QueueAttribute(string queueName)
    {
        QueueName = queueName;
    }
}
