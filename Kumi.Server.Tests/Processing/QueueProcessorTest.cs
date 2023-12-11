using System.Reflection;
using Kumi.Server.Processing;
using NUnit.Framework;

namespace Kumi.Server.Tests.Processing;

public partial class QueueProcessorTest : TestsWithDependencies
{
    public TestQueueProcessor Queue { get; set; } = new TestQueueProcessor();

    protected override void SetupSelf()
    {
        Dependencies.Inject(Queue);
        var qp = Queue.GetType();
        var methods = qp.GetMethods()
            .Where(m => m.GetCustomAttributes(typeof(QueueAttribute), false).Length > 0)
            .ToList();

        foreach (var method in methods)
        {
            // Get the queue attribute.
            var queue = (QueueAttribute)method.GetCustomAttributes(typeof(QueueAttribute), false)[0];
                
            if (queue == null)
                continue;
                
            Queue.Methods.Add(new Tuple<string, MethodInfo>(QueueProcessor.QUEUE_PREFIX + queue.QueueName, method));
        }
    }

    protected override void SetupBeforeTest()
    {
        Queue.ClearQueue("test_queue"); // Clear the queue before each test.
        Queue.ClearQueue("test_queue2");
    }
    
    [Test]
    public void AddQueueItem()
    {
        Queue.AddItem("test_queue", "{ \"Data\": \"Test\" } ");

        Assert.AreEqual(1, Queue.GetQueueLength("test_queue"));
        
        // Remove the item from the queue.
        Queue.GetNextItem("test_queue");
    }
    
    [Test]
    public void GetQueueItem()
    {
        Queue.AddItem("test_queue", "{ \"Data\": \"Test\" } ");
        
        var item = Queue.GetNextItem<TestQueueItem>("test_queue");
        
        Assert.AreEqual(item.Data.Data, "Test");
    }

    [Test]
    public void ProcessPackedQueue()
    {
        Queue.AddItem("test_queue", "{ \"Data\": \"Test\" } ");
        Queue.AddItem("test_queue", "{ \"Data\": \"Test\" } ");
        Queue.AddItem("test_queue", "{ \"Data\": \"Test\" } ");
        Queue.AddItem("test_queue", "{ \"Data\": \"Test\" } ");
        Queue.AddItem("test_queue", "{ \"Data\": \"Test\" } ");
        
        Assert.AreEqual(5, Queue.GetQueueLength("test_queue"));
        
        Queue.Run(true);
        
        Assert.AreEqual(0, Queue.GetQueueLength("test_queue"));
    }

    [Test]
    public void ProcessTwoDifferentQueueItems()
    {
        Queue.AddItem("test_queue", "{ \"Data\": \"Test\" } ");
        Queue.AddItem("test_queue", "{ \"Data\": \"Test\" } ");
        Queue.AddItem("test_queue2", "{ \"Data2\": \"Test2\" } ");
        Queue.AddItem("test_queue2", "{ \"Data2\": \"Test2\" } ");
        
        Assert.AreEqual(2, Queue.GetQueueLength("test_queue"));
        Assert.AreEqual(2, Queue.GetQueueLength("test_queue2"));
        
        Queue.Run(true);
        
        Assert.AreEqual(0, Queue.GetQueueLength("test_queue"));
        Assert.AreEqual(0, Queue.GetQueueLength("test_queue2"));
    }
    
    [Test]
    public void ProcessQueueItem()
    {
        Queue.AddItem("test_queue", "{ \"Data\": \"Test\" } ");
        Queue.ForceProcessNextItem("test_queue");
        
        // Assert that the item was processed.
        Assert.AreEqual(0, Queue.GetQueueLength("test_queue"));
    }
    
    public partial class TestQueueProcessor
        : QueueProcessor
    {
        public TestQueueProcessor()
        {
        }
        
        [Queue("test_queue")]
        public void ProcessTestItem(TestQueueItem item)
        {
            // Do nothing.
            Console.WriteLine(item.Data.Data);
            return;
        }
        
        [Queue("test_queue2")]
        public void ProcessSecondTestItem(SecondTestQueueItem item)
        {
            // Do nothing.
            Console.WriteLine(item.Data.Data2);
            return;
        }
        
        protected override void PreprocessItem(QueueItem item)
        {
            return;
        }
    }
    
    public class TestQueueItem
        : QueueItem<TestQueueItem.TestQueueItemData>
    {
        public class TestQueueItemData
        {
            public string? Data { get; set; }
        }
        
    }
    
    public class SecondTestQueueItem
        : QueueItem<SecondTestQueueItem.SecondTestQueItemData>
    {
        public class SecondTestQueItemData
        {
            public string? Data2 { get; set; }
        }
    }
}
