using System;
using System.Collections.Generic;
using System.Linq;

public class MemoryRepository<T> : IRepository<T> where T : IEntity
{
    private readonly List<T> _store = new();
    private readonly object _sync = new();

    public void Add(T entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        lock (_sync) { _store.Add(entity); }
    }

    public T? GetById(int id)
    {
        lock (_sync) { return _store.FirstOrDefault(x => x.Id == id); }
    }

    public IEnumerable<T> GetAll()
    {
        lock (_sync) { return _store.ToList(); }
    }

    public void Update(T entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        lock (_sync)
        {
            var idx = _store.FindIndex(x => x.Id == entity.Id);
            if (idx == -1) throw new KeyNotFoundException($"Entity with Id {entity.Id} not found.");
            _store[idx] = entity;
        }
    }

    public void Delete(int id)
    {
        lock (_sync)
        {
            var idx = _store.FindIndex(x => x.Id == id);
            if (idx != -1) _store.RemoveAt(idx);
        }
    }
}