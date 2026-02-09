using ASP_API_sample.Models;

namespace ASP_API_sample.Repositories;

/// <summary>
/// Unit of Work — паттерн для группировки нескольких операций в одну транзакцию.
/// Позволяет сохранять изменения всех репозиториев одним вызовом SaveChangesAsync.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IRepository<Category> Categories { get; }
    IRepository<Product> Products { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
