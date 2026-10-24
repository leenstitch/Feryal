
using Feryal.Application.DTOs.Products;
using Feryal.Application.DTOs.Products.Request;
using Feryal.Application.Products.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Feryal.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
public sealed class ProductVariantsController : ControllerBase
{
    private readonly IProductVariantService _variantService;

    public ProductVariantsController(
        IProductVariantService variantService)
    {
        _variantService = variantService;
    }

    [HttpGet("~/api/products/{productId:int}/variants")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVariants(
        int productId,
        [FromQuery] ProductVariantQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        if (productId <= 0)
        {
            return BadRequest(new
            {
                message = "Product ID must be greater than zero."
            });
        }

        var result = await _variantService.GetVariantsAsync(
            productId,
            parameters,
            cancellationToken);

        if (result is null)
        {
            return NotFound(new
            {
                message = "Product was not found."
            });
        }

        return Ok(result);
    }

    [HttpGet("~/api/products/{productId:int}/variants/available")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailableVariants(
        int productId,
        [FromQuery] ProductVariantQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        if (productId <= 0)
        {
            return BadRequest(new
            {
                message = "Product ID must be greater than zero."
            });
        }

        var result = await _variantService.GetAvailableVariantsAsync(
            productId,
            parameters,
            cancellationToken);

        if (result is null)
        {
            return NotFound(new
            {
                message = "Product was not found."
            });
        }

        return Ok(result);
    }

    [HttpPatch("~/api/product-variants/{variantId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchVariant(
        int variantId,
        [FromBody] UpdateProductVariantRequest request,
        CancellationToken cancellationToken)
    {
        if (variantId <= 0)
        {
            return BadRequest(new
            {
                message = "Variant ID must be greater than zero."
            });
        }

        try
        {
            var result = await _variantService.UpdateVariantAsync(
                variantId,
                request,
                cancellationToken);

            if (result is null)
            {
                return NotFound(new
                {
                    message = "Variant was not found."
                });
            }

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("~/api/product-variants/{variantId:int}/activate")]
    public async Task<IActionResult> Activate(
        int variantId,
        CancellationToken cancellationToken)
    {
        if (variantId <= 0)
        {
            return BadRequest(new
            {
                message = "Variant ID must be greater than zero."
            });
        }

        bool success = await _variantService.ActivateAsync(
            variantId,
            cancellationToken);

        if (!success)
        {
            return NotFound(new
            {
                message = "Variant was not found."
            });
        }

        return NoContent();
    }

    [HttpPatch("~/api/product-variants/{variantId:int}/deactivate")]
    public async Task<IActionResult> Deactivate(
        int variantId,
        CancellationToken cancellationToken)
    {
        if (variantId <= 0)
        {
            return BadRequest(new
            {
                message = "Variant ID must be greater than zero."
            });
        }

        bool success = await _variantService.DeactivateAsync(
            variantId,
            cancellationToken);

        if (!success)
        {
            return NotFound(new
            {
                message = "Variant was not found."
            });
        }

        return NoContent();
    }

    [HttpPatch("~/api/product-variants/{variantId:int}/show")]
    public async Task<IActionResult> Show(
        int variantId,
        CancellationToken cancellationToken)
    {
        if (variantId <= 0)
        {
            return BadRequest(new
            {
                message = "Variant ID must be greater than zero."
            });
        }

        bool success = await _variantService.ShowAsync(
            variantId,
            cancellationToken);

        if (!success)
        {
            return NotFound(new
            {
                message = "Variant was not found."
            });
        }

        return NoContent();
    }

    [HttpPatch("~/api/product-variants/{variantId:int}/hide")]
    public async Task<IActionResult> Hide(
        int variantId,
        CancellationToken cancellationToken)
    {
        if (variantId <= 0)
        {
            return BadRequest(new
            {
                message = "Variant ID must be greater than zero."
            });
        }

        bool success = await _variantService.HideAsync(
            variantId,
            cancellationToken);

        if (!success)
        {
            return NotFound(new
            {
                message = "Variant was not found."
            });
        }

        return NoContent();
    }
}
