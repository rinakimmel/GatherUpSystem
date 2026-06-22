using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class MemoryRepository<T> : IRepository<T> where T : IEntity
{
    private readonly List<T> _store = new();
    private readonly object _sync = new();

    public Task AddAsync(T entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        lock (_sync)
        {
            int newId = _store.Count == 0 ? 1 : _store.Max(x => x.Id) + 1;
            typeof(T).GetProperty("Id")?.SetValue(entity, newId);
            _store.Add(entity);
        }
        return Task.CompletedTask;
    }

    public Task<T?> GetByIdAsync(int id)
    {
        lock (_sync) { return Task.FromResult(_store.FirstOrDefault(x => x.Id == id)); }
    }

    public Task<IEnumerable<T>> GetAllAsync()
    {
        lock (_sync) { return Task.FromResult<IEnumerable<T>>(_store.ToList()); }
    }

    public Task UpdateAsync(T entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        lock (_sync)
        {
            var idx = _store.FindIndex(x => x.Id == entity.Id);
            if (idx == -1) throw new KeyNotFoundException($"Entity with Id {entity.Id} not found.");
            _store[idx] = entity;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id)
    {
        lock (_sync)
        {
            var idx = _store.FindIndex(x => x.Id == id);
            if (idx != -1) _store.RemoveAt(idx);
        }
        return Task.CompletedTask;
    }
}