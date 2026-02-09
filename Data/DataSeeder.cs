using ASP_API_sample.Models;
using Microsoft.EntityFrameworkCore;

namespace ASP_API_sample.Data;

/// <summary>
/// Заполнение базы данных начальными данными.
/// Вызывается при первом запуске приложения.
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context, CancellationToken ct = default)
    {
        if (await context.Categories.AnyAsync(ct))
            return;

        var categories = new[]
        {
            new Category { Id = 1, Name = "Электроника", Description = "Смартфоны, ноутбуки, гаджеты" },
            new Category { Id = 2, Name = "Одежда", Description = "Мужская и женская одежда" },
            new Category { Id = 3, Name = "Книги", Description = "Художественная и учебная литература" }
        };
        await context.Categories.AddRangeAsync(categories, ct);

        var products = new[]
        {
            new Product { Id = 1, Name = "Смартфон X", Description = "Флагманский смартфон", Price = 49990, StockQuantity = 25, CategoryId = 1 },
            new Product { Id = 2, Name = "Ноутбук Pro", Description = "Для работы и учёбы", Price = 89990, StockQuantity = 10, CategoryId = 1 },
            new Product { Id = 3, Name = "Футболка базовая", Description = "Хлопок 100%", Price = 990, StockQuantity = 100, CategoryId = 2 }
        };
        await context.Products.AddRangeAsync(products, ct);

        await context.SaveChangesAsync(ct);
    }
}
