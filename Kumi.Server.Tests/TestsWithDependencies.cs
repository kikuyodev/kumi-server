using Kumi.Server.Redis;
using NUnit.Framework;
using osu.Framework.Allocation;

namespace Kumi.Server.Tests;

public class TestsWithDependencies
{
    /// <summary>
    /// The dependencies across the entire branch of unit tests..
    /// </summary>
    protected DependencyContainer Dependencies { get; set; } = new();
    
    private bool setup = false;
    
    [OneTimeSetUp]
    public void Setup()
    {
        if (setup)
        {
            SetupBeforeTest();
            return;
        }

        setup = true;
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
