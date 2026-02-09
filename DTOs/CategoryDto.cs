namespace ASP_API_sample.DTOs;

/// <summary>
/// DTO для категории (ответ API).
/// </summary>
public record CategoryDto(int Id, string Name, string? Description);

/// <summary>
/// DTO для создания категории.
/// </summary>
public record CreateCategoryDto(string Name, string? Description);

/// <summary>
/// DTO для обновления категории.
/// </summary>
public record UpdateCategoryDto(string Name, string? Description);
