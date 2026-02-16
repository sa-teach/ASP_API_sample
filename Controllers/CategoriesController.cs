using ASP_API_sample.DTOs;
using ASP_API_sample.Models;
using ASP_API_sample.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ASP_API_sample.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoriesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll(CancellationToken ct)
    {
        var categories = await _unitOfWork.Categories.GetAllAsync(ct);
        var dtos = categories.Select(c => new CategoryDto(c.Id, c.Name, c.Description));
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> GetById(int id, CancellationToken ct)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id, ct);
        if (category is null)
            return NotFound();

        return Ok(new CategoryDto(category.Id, category.Name, category.Description));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CreateCategoryDto dto, CancellationToken ct)
    {
        var category = new Category { Name = dto.Name, Description = dto.Description };
        await _unitOfWork.Categories.AddAsync(category, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = category.Id },
            new CategoryDto(category.Id, category.Name, category.Description));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> Update(int id, [FromBody] UpdateCategoryDto dto, CancellationToken ct)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id, ct);
        if (category is null)
            return NotFound();

        category.Name = dto.Name;
        category.Description = dto.Description;
        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync(ct);

        return Ok(new CategoryDto(category.Id, category.Name, category.Description));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id, ct);
        if (category is null)
            return NotFound();

        _unitOfWork.Categories.Delete(category);
        await _unitOfWork.SaveChangesAsync(ct);

        return NoContent();
    }
}
