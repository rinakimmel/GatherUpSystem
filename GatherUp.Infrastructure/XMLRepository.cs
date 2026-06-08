using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class XMLRepository<T> : IRepository<T> where T : class, IEntity, new()
{
    private readonly string _filePath;

    public XMLRepository(string directoryPath, string entityName)
    {
        if (string.IsNullOrEmpty(directoryPath))
            throw new ArgumentException("Directory path cannot be empty", nameof(directoryPath));

        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        _filePath = Path.Combine(directoryPath, $"{entityName}.xml");
    }

    public void Add(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var items = LoadAll();
        if (items.Any(x => x.Id == entity.Id))
            throw new InvalidOperationException($"Entity with Id {entity.Id} already exists.");

        items.Add(entity);
        SaveAll(items);
    }

    public T? GetById(int id)
    {
        var items = LoadAll();
        return items.FirstOrDefault(x => x.Id == id);
    }

    public IEnumerable<T> GetAll()
    {
        return LoadAll();
    }

    public void Update(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var items = LoadAll();
        var index = items.FindIndex(x => x.Id == entity.Id);

        if (index == -1)
            throw new KeyNotFoundException($"Entity with Id {entity.Id} not found.");

        items[index] = entity;
        SaveAll(items);
    }

    public void Delete(int id)
    {
        var items = LoadAll();
        var index = items.FindIndex(x => x.Id == id);

        if (index != -1)
        {
            items.RemoveAt(index);
            SaveAll(items);
        }
    }

    protected virtual void SaveAll(List<T> items)
    {
        XMLSerializer<T>.WriteToFile(_filePath, items);
    }

    protected virtual List<T> LoadAll()
    {
        return XMLSerializer<T>.ReadFromFile(_filePath);
    }
}