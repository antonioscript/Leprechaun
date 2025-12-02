using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Leprechaun.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Category>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _categoryService.GetAllAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Category>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _categoryService.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return NotFound();

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Category>> Create([FromBody] Category model, CancellationToken cancellationToken)
    {
        var created = await _categoryService.CreateAsync(model, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Category model, CancellationToken cancellationToken)
    {
        var existing = await _categoryService.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return NotFound();

        existing.Name = model.Name;
        existing.Description = model.Description;
        existing.IsActive = model.IsActive;

        await _categoryService.UpdateAsync(existing, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _categoryService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}