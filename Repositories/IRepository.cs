using System.Linq.Expressions;

namespace ASP_API_sample.Repositories;

/// <summary>
/// Базовый интерфейс репозитория для работы с сущностями типа T.
/// Инкапсулирует логику доступа к данным.
/// </summary>
/// <typeparam name="T">Тип сущности (должен быть класс).</typeparam>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Delete(T entity);
}
