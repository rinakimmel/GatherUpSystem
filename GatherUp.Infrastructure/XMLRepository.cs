using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

    public Task AddAsync(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        return Task.Run(() =>
        {
            var items = LoadAll();
            int newId = items.Count == 0 ? 1 : items.Max(x => x.Id) + 1;
            typeof(T).GetProperty("Id")?.SetValue(entity, newId);

            items.Add(entity);
            SaveAll(items);
        });
    }

    public Task<T?> GetByIdAsync(int id)
    {
        return Task.Run(() =>
        {
            var items = LoadAll();
            return items.FirstOrDefault(x => x.Id == id);
        });
    }

    public Task<IEnumerable<T>> GetAllAsync()
    {
        return Task.Run<IEnumerable<T>>(() => LoadAll());
    }

    public Task UpdateAsync(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        return Task.Run(() =>
        {
            var items = LoadAll();
            var index = items.FindIndex(x => x.Id == entity.Id);

            if (index == -1)
                throw new KeyNotFoundException($"Entity with Id {entity.Id} not found.");

            items[index] = entity;
            SaveAll(items);
        });
    }

    public Task DeleteAsync(int id)
    {
        return Task.Run(() =>
        {
            var items = LoadAll();
            var index = items.FindIndex(x => x.Id == id);

            if (index != -1)
            {
                items.RemoveAt(index);
                SaveAll(items);
            }
        });
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