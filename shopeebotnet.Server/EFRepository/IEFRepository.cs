using System;

namespace shopeebotnet.Server.EFRepository;

public interface IEFRepository<T> where T : class
{
    Microsoft.EntityFrameworkCore.DbSet<T> GetDbSet();
    Microsoft.EntityFrameworkCore.DbContext GetDbContext();
    void Add(T entity);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T?> GetByIdAsync(params object[] keyValues);
    void Update(T entity);
    void Remove(T entity);
}
