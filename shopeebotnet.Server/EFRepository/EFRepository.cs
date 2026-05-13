using System;
using Microsoft.EntityFrameworkCore;

namespace shopeebotnet.Server.EFRepository;

public class EFRepository<T> : IEFRepository<T> where T : class
{
    private readonly Microsoft.EntityFrameworkCore.DbContext _dbContext;
    private readonly DbSet<T> _entitySet;

    public EFRepository(Microsoft.EntityFrameworkCore.DbContext dbContext)
    {
        _dbContext = dbContext;
        _entitySet = dbContext.Set<T>();
    }

    public Microsoft.EntityFrameworkCore.DbSet<T> GetDbSet()
    {
        return _entitySet;
    }

    public Microsoft.EntityFrameworkCore.DbContext GetDbContext()
    => _dbContext;

    public void Add(T entity)
        => _dbContext.Add(entity);

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await _dbContext.AddAsync(entity);

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _entitySet.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<T?> GetByIdAsync(params object[] keyValues)
        => await _entitySet.FindAsync(keyValues);

    public void Update(T entity)
        => _dbContext.Update(entity);

    public void Remove(T entity)
        => _dbContext.Remove(entity);
}
