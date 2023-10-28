using osu.Framework.Allocation;

namespace Kumi.Server.IO;

public class ConcurrentEntityContainer<TKey, TValue> : InvokeOnDisposal<ConcurrentEntityStore<TKey, TValue>.ConcurrentEntity>
    where TKey : notnull
    where TValue : class
{
    private readonly ConcurrentEntityStore<TKey, TValue>.ConcurrentEntity _entity;
        
    public ConcurrentEntityContainer(ConcurrentEntityStore<TKey, TValue>.ConcurrentEntity sender)
        : base(sender, action)
    {
        _entity = sender;
    }
        
    public TValue Value
    {
        get => _entity.Value;
        set => _entity.Value = value;
    }
        
    public TKey Key => _entity.Key;
        
    public ConcurrentEntityStore<TKey, TValue> Store => _entity.Store;
        
    public void Dispose()
    {
        Value = null;
        _entity.Dispose();
    }
        
    public void Release()
    {
        _entity.Release();
    }
        
    private static void action(ConcurrentEntityStore<TKey, TValue>.ConcurrentEntity sender)
    {
        if (!sender.Disposed && sender.Value == null)
        {
            sender.Dispose();
        }
            
        sender.Release();
    }
}
