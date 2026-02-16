namespace ASP_API_sample.DTOs;

public record ProductDto(int Id, string Name, string? Description, decimal Price, int StockQuantity, int CategoryId);

public record CreateProductDto(string Name, string? Description, decimal Price, int StockQuantity, int CategoryId);

public record UpdateProductDto(string Name, string? Description, decimal Price, int StockQuantity, int CategoryId);
