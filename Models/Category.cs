namespace ASP_API_sample.Models;

/// <summary>
/// Категория товаров. Сущность для примера учебного API.
/// </summary>
public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    // Навигационное свойство: у категории много товаров
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
