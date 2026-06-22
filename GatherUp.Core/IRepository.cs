using System.Threading.Tasks;
using System.Collections.Generic;

public interface IRepository<T> where T : IEntity       
{
    Task AddAsync(T entity);
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}                   