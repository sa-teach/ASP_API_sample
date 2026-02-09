namespace ASP_API_sample.DTOs;

/// <summary>
/// DTO для товара (ответ API).
/// </summary>
public record ProductDto(int Id, string Name, string? Description, decimal Price, int StockQuantity, int CategoryId);

/// <summary>
/// DTO для создания товара.
/// </summary>
public record CreateProductDto(string Name, string? Description, decimal Price, int StockQuantity, int CategoryId);

/// <summary>
/// DTO для обновления товара.
/// </summary>
public record UpdateProductDto(string Name, string? Description, decimal Price, int StockQuantity, int CategoryId);
