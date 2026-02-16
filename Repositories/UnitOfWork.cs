using ASP_API_sample.Data;
using ASP_API_sample.Models;

namespace ASP_API_sample.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IRepository<Category>? _categories;
    private IRepository<Product>? _products;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IRepository<Category> Categories =>
        _categories ??= new Repository<Category>(_context);

    public IRepository<Product> Products =>
        _products ??= new Repository<Product>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
