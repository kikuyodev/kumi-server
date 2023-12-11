using Kumi.Server.Redis;
using NUnit.Framework;
using osu.Framework.Allocation;

namespace Kumi.Server.Tests;

public class TestsWithDependencies
{
    /// <summary>
    /// The dependencies across the entire branch of unit tests..
    /// </summary>
    public DependencyContainer Dependencies { get; set; } = new();
    
    bool _setup = false;
    
    [SetUp]
    public void Setup()
    {
        if (_setup)
        {
            SetupBeforeTest();
            return;
        }

        _setup = true;
        Dependencies.CacheAs(this);
        Dependencies.CacheAs(new RedisConnection());
        
        SetupSelf();
        SetupBeforeTest();
    }

    protected virtual void SetupSelf()
    {
        
    }
    
    protected virtual void SetupBeforeTest()
    {
        
    }
}
