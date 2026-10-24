using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.Products;
using Feryal.Application.DTOs.Products.Request;
using Feryal.Application.DTOs.Products.Response;
using Feryal.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Feryal.API.Controllers;

[ApiController]
[Route("api/products")]
[Authorize(Roles = "Admin")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    // Initializes the controller with the product service.
    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    // Retrieves all products for administration with pagination and query filters.
    [HttpGet("admin")]
    public async Task<ActionResult<PagedResponse<ProductResponse>>> GetAllProducts(
        [FromQuery] ProductQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _productService.GetPagedProductsAsync(
            parameters,
            cancellationToken);

        return Ok(result);
    }

    // Retrieves a product by its ID for administration.
    [HttpGet("admin/{productId:int}")]
    public async Task<ActionResult<ProductResponse>> GetProductById(
        int productId,
        CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductByIdAsync(
            productId,
            cancellationToken);

        if (product is null)
        {
            return NotFound(new
            {
                message = "Product was not found."
            });
        }

        return Ok(product);
    }

    // Retrieves active products for the public catalog with pagination and query filters.
    [AllowAnonymous]
    [HttpGet("catalog")]
    public async Task<ActionResult<PagedResponse<ProductResponse>>> GetCatalog(
        [FromQuery] ProductQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _productService.GetPagedActiveProductsAsync(
            parameters,
            cancellationToken);

        return Ok(result);
    }

    // Creates a new product using the provided request data.
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _productService.CreateAsync(
                request,
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                result);
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

    // Updates selected properties of an existing product.
    [HttpPatch("{productId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Patch(
        int productId,
        [FromBody] PatchProductRequest request,
        CancellationToken cancellationToken)
    {
        if (productId <= 0)
        {
            return BadRequest(new
            {
                message = "Product ID must be greater than zero."
            });
        }

        try
        {
            var result = await _productService.PatchAsync(
                productId,
                request,
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
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Activates a product by its ID.
    [HttpPatch("{productId:int}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(
        int productId,
        CancellationToken cancellationToken)
    {
        if (productId <= 0)
        {
            return BadRequest(new
            {
                message = "Product ID must be greater than zero."
            });
        }

        bool activated = await _productService.ActivateAsync(
            productId,
            cancellationToken);

        if (!activated)
        {
            return NotFound(new
            {
                message = "Product was not found."
            });
        }

        return NoContent();
    }

    // Deactivates a product by its ID.
    [HttpPatch("{productId:int}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(
        int productId,
        CancellationToken cancellationToken)
    {
        if (productId <= 0)
        {
            return BadRequest(new
            {
                message = "Product ID must be greater than zero."
            });
        }

        bool deactivated = await _productService.DeactivateAsync(
            productId,
            cancellationToken);

        if (!deactivated)
        {
            return NotFound(new
            {
                message = "Product was not found."
            });
        }

        return NoContent();
    }

    // Adds a color option to the specified product.
    [HttpPost("{productId:int}/colors")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddColor(
        int productId,
        [FromBody] CreateProductColorRequest request,
        CancellationToken cancellationToken)
    {
        if (productId <= 0)
        {
            return BadRequest(new
            {
                message = "Product ID must be greater than zero."
            });
        }

        try
        {
            var result = await _productService.AddColorAsync(
                productId,
                request,
                cancellationToken);

            if (result is null)
            {
                return NotFound(new
                {
                    message = "Product was not found."
                });
            }

            return StatusCode(
                StatusCodes.Status201Created,
                result);
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

    // Adds a size option to the specified product.
    [HttpPost("{productId:int}/sizes")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddSize(
        int productId,
        [FromBody] CreateProductSizeRequest request,
        CancellationToken cancellationToken)
    {
        if (productId <= 0)
        {
            return BadRequest(new
            {
                message = "Product ID must be greater than zero."
            });
        }

        try
        {
            var result = await _productService.AddSizeAsync(
                productId,
                request,
                cancellationToken);

            if (result is null)
            {
                return NotFound(new
                {
                    message = "Product was not found."
                });
            }

            return StatusCode(
                StatusCodes.Status201Created,
                result);
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

    // Adds a scent option to the specified product.
    [HttpPost("{productId:int}/scents")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddScent(
        int productId,
        [FromBody] CreateProductScentRequest request,
        CancellationToken cancellationToken)
    {
        if (productId <= 0)
        {
            return BadRequest(new
            {
                message = "Product ID must be greater than zero."
            });
        }

        try
        {
            var result = await _productService.AddScentAsync(
                productId,
                request,
                cancellationToken);

            if (result is null)
            {
                return NotFound(new
                {
                    message = "Product was not found."
                });
            }

            return StatusCode(
                StatusCodes.Status201Created,
                result);
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
}