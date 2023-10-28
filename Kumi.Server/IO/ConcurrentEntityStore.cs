using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Kumi.Server.IO;

/// <summary>
/// A collection of items that can be accessed by multiple threads, and have their state updated by multiple threads.
/// </summary>
/// <typeparam name="T"></typeparam>
public partial class ConcurrentEntityStore<TKey, TValue>
    where TKey : notnull
    where TValue : class
{
    private const int Timeout = 10000;

    private readonly ConcurrentDictionary<TKey, ConcurrentEntity> _items;
    
    public ConcurrentEntityStore()
    {
        _items = new ConcurrentDictionary<TKey, ConcurrentEntity>();
    }
    
    public TValue GetUnsafe(TKey key)
    {
        lock (_items)
        {
            if (!_items.TryGetValue(key, out var entity))
            {
                return null;
            }

            return entity.GetUnsafe();
        }
    }

    public async Task<ConcurrentEntityContainer<TKey, TValue>> Get(TKey key) => await Get(key, true);
    public async Task<ConcurrentEntityContainer<TKey, TValue>> Get(TKey key, bool shouldBeCreated) => await Get(key, shouldBeCreated, true);
    
    public async Task<ConcurrentEntityContainer<TKey, TValue>> Get(TKey key, bool shouldBeCreated, bool shouldBeRetried)
    {
        int retries = shouldBeRetried ? 5 : 1;

        while (retries-- > 0)
        {
            ConcurrentEntity? entity;
        
            lock (_items)
            {
                if (!_items.TryGetValue(key, out entity))
                {
                    if (!shouldBeCreated)
                    {
                        throw new KeyNotFoundException($"Could not find entity {key}, as it does not exist.");
                    }
                    
                    Create(key);
                    _items.TryGetValue(key, out entity);
                }
            }

            try
            {
                await entity?.LockAsync();
            } catch (InvalidOperationException)
            {
                if (shouldBeCreated)
                {
                    continue;
                }
                
                throw new KeyNotFoundException($"Could not find entity {key}, as it does not exist.");
            } 

            return new ConcurrentEntityContainer<TKey, TValue>(entity);
        }
        
        throw new TimeoutException($"Could not retrieve entity {key} in time.");
    }
    
    public void Create(TKey key, TValue? value = null)
    {
        lock (_items)
        {
            if (!_items.TryGetValue(key, out var _))
            {
                ConcurrentEntity newEntity;

                if (value != null)
                {
                    using (newEntity = new ConcurrentEntity(key, this))
                    {
                        newEntity.Value = value;
                    }
                }
                else
                {
                    newEntity = new ConcurrentEntity(key, this);
                }
                
                _items.TryAdd(key, newEntity);
            }
        }
    }
    
    public void Remove(TKey key)
    {
        lock (_items)
        {
            if (_items.TryGetValue(key, out var entity))
            {
                entity.Dispose();
                _items.TryRemove(key, out _);
            }
        }
    }

    public class ConcurrentEntity : IDisposable
    {
        [AllowNull]
        private TValue _value;
        private TKey _key { get; }
        private ConcurrentEntityStore<TKey, TValue> _store { get; }
        
        // lock
        private readonly SemaphoreSlim _semaphore = new( 1);

        public ConcurrentEntity(TKey key, ConcurrentEntityStore<TKey, TValue> store)
            : base()
        {
            _key = key;
            _store = store;
        }
        
        /// <summary>
        /// Whether or not the entity is locked to a thread currently.
        /// </summary>
        protected bool IsLocked => _semaphore.CurrentCount == 0;
        
        /// <summary>
        /// Whether or not the entity has been safely disposed.
        /// </summary>
        public bool Disposed { get; private set; } = false;
        
        /// <summary>
        /// The value of the entity.
        /// </summary>
        [AllowNull]
        public TValue Value
        {
            get
            {
                isUsable();
                
                return _value;
            }
            set
            {
                isUsable();
                
                _value = value;
            }
        }
        
        public TKey Key => _key;
        
        public ConcurrentEntityStore<TKey, TValue> Store => _store;
        
        public TValue GetUnsafe()
        {
            return _value;
        }

        /// <summary>
        /// Safely disposes of the entity.
        /// </summary>
        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }
            
            Disposed = true;

            _store.Remove(_key);
            _value = null;
            _semaphore.Release();
            _semaphore.Dispose();
        }
        
        /// <summary>
        /// Locks the entity for the current thread.
        /// </summary>
        /// <exception cref="TimeoutException">Throws an exception if a lock took too long tohandle.</exception>
        /// <exception cref="ObjectDisposedException">Throws an exception if the entity has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Throws an exception if the entity is not locked when it should be.</exception>
        public async Task LockAsync()
        {
            isUsable(false);
            
            if (!await _semaphore.WaitAsync(Timeout))
            {
                throw new TimeoutException($"Could not lock entity {_key} in time.");
            }

            isUsable();
        }
        
        /// <summary>
        /// Releases the lock on the entity.
        /// </summary>
        public void Release()
        {
            isUsable(true);
            
            _semaphore.Release();
        }
        
        private void isUsable(bool shouldBeLocked = true)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException($"Entity {_key} has been disposed.");
            }
            
            if (!IsLocked && shouldBeLocked)
            {
                throw new InvalidOperationException($"Entity {_key} is not locked when it should be.");
            }
        }
    }

}
