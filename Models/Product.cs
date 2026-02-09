namespace ASP_API_sample.Models;

/// <summary>
/// Товар. Сущность для примера учебного API.
/// </summary>
public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }

    // Внешний ключ: товар принадлежит категории
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}
