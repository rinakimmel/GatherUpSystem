public interface IRepository<T> where T : IEntity
{
    void Add(T entity);
    T? GetById(int id);
    IEnumerable<T> GetAll();
    void Update(T entity);
    void Delete(int id);
}