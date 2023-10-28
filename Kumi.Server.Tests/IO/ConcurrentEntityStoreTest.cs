using Kumi.Server.IO;
using NUnit.Framework;

namespace Kumi.Server.Tests.IO;

public class ConcurrentEntityStoreTest
{
    private readonly ConcurrentEntityStore<int, TestEntity> store;
    
    public ConcurrentEntityStoreTest()
    {
        store = new ConcurrentEntityStore<int, TestEntity>();
    }

    [Test]
    public async Task TestGetEntity()
    {
        // This should throw an exception, as the entity does not exist.
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await store.Get(1, false));
        
        // Getting the entity unsafely should also return null.
        Assert.Null(store.GetUnsafe(1));

        using (var container = await store.Get(1))
        {
            // This should not throw an exception, as this is in a locked environment
            container.Value = new TestEntity("test");
            
            // This should not throw an exception, as the entity gets created.
            Assert.NotNull(container.Value);
        }
    }

    [Test]
    public async Task TestGetEntityUnsafely()
    {
        // This should return null, as the entity does not exist.
        Assert.Null(store.GetUnsafe(1));

        using (var container = await store.Get(1))
        {
            container.Value = new TestEntity("test");
        }
        
        // This should return the entity, as it now exists.
        Assert.NotNull(store.GetUnsafe(1));
    }

    [Test]
    public async Task TestGetEntityWithoutLockShouldFail()
    {
        // This should throw an exception, as the entity does not exist.
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await store.Get(1, false));
        
        // Getting the entity unsafely should also return null.
        Assert.Null(store.GetUnsafe(1));

        ConcurrentEntityContainer<int, TestEntity> container;

        using (container = await store.Get(1))
        {
            container.Value = new TestEntity("test");
            
            // This should not throw an exception, as the entity gets created.
            Assert.NotNull(container.Value);
            
        }
        
        // However, this should throw an exception, as we are accessing the entity without a lock.
        Assert.Throws<InvalidOperationException>(() =>
        {
            var _ = container.Value;
        });
    }
    
    public class TestEntity
    {
        public string Test { get; set; }

        public TestEntity(string test)
        {
            Test = test;
        }
    }
}
