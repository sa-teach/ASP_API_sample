namespace ASP_API_sample.DTOs;

public record CategoryDto(int Id, string Name, string? Description);

public record CreateCategoryDto(string Name, string? Description);

public record UpdateCategoryDto(string Name, string? Description);
