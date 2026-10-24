using Feryal.Application.DTOs.Categories;
using Feryal.Application.DTOs.Common;
using Feryal.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Feryal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<CategoryResponse>>> GetAll(
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        var categories = await _categoryService.GetAllAsync(
            pageNumber,
            pageSize,
            cancellationToken);

        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetByIdAsync(
            id,
            cancellationToken);

        return Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await _categoryService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = category.Id },
            category);
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> Update(
        int id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await _categoryService.UpdateAsync(
            id,
            request,
            cancellationToken);

        return Ok(category);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        await _categoryService.DeleteAsync(
            id,
            cancellationToken);

        return NoContent();
    }

    [HttpPatch("{id:int}/activate")]
    public async Task<ActionResult<CategoryResponse>> Activate(
    int id,
    CancellationToken cancellationToken)
    {
        var category = await _categoryService.ActivateAsync(
            id,
            cancellationToken);

        return Ok(category);
    }

    [HttpPatch("{id:int}/deactivate")]
    public async Task<ActionResult<CategoryResponse>> Deactivate(
        int id,
        CancellationToken cancellationToken)
    {
        var category = await _categoryService.DeactivateAsync(
            id,
            cancellationToken);

        return Ok(category);
    }
}