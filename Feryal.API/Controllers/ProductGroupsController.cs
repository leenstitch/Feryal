
using Feryal.Application.DTOs.ProductGroups;
using Feryal.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Feryal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ProductGroupsController : ControllerBase
{
    private readonly IProductGroupService _productGroupService;

    public ProductGroupsController(
        IProductGroupService productGroupService)
    {
        _productGroupService = productGroupService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        var result = await _productGroupService.GetAllAsync(
            pageNumber,
            pageSize,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _productGroupService.GetByIdAsync(
            id,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _productGroupService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateProductGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _productGroupService.UpdateAsync(
            id,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        await _productGroupService.DeleteAsync(
            id,
            cancellationToken);

        return NoContent();
    }

    [HttpPatch("{id:int}/activate")]
    public async Task<IActionResult> Activate(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _productGroupService.ActivateAsync(
            id,
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _productGroupService.DeactivateAsync(
            id,
            cancellationToken);

        return Ok(result);
    }
}