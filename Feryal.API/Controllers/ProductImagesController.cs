using Feryal.Application.Products.Images;
using Feryal.Application.Products.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Feryal.API.Controllers;

[ApiController]
[Route("api/products/{productId:int}/images")]
[Authorize(Roles = "Admin")]
public sealed class ProductImagesController : ControllerBase
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private const int MaxImages = 20;

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

    private readonly IProductImageService _imageService;

    // Initializes the controller with the product image service.
    public ProductImagesController(IProductImageService imageService)
    {
        _imageService = imageService;
    }

    // Uploads new images for a product and optionally designates one of them as the primary image.
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddImages(
        int productId,
        [FromForm] List<IFormFile> images,
        [FromForm] int? primaryNewImageIndex,
        CancellationToken cancellationToken)
    {
        if (productId <= 0)
        {
            return BadRequest(new
            {
                message = "Product ID must be greater than zero."
            });
        }

        if (images is null || images.Count == 0)
        {
            return BadRequest(new
            {
                message = "At least one image is required."
            });
        }

        if (images.Count > MaxImages)
        {
            return BadRequest(new
            {
                message = $"You cannot upload more than {MaxImages} images at once."
            });
        }

        if (!ValidateImages(images, out string? error))
        {
            return BadRequest(new { message = error });
        }

        if (primaryNewImageIndex.HasValue &&
            (primaryNewImageIndex.Value < 0 ||
             primaryNewImageIndex.Value >= images.Count))
        {
            return BadRequest(new
            {
                message = "PrimaryNewImageIndex is outside the uploaded images range."
            });
        }

        var streams = new List<Stream>();

        try
        {
            var uploads = CreateUploads(images, streams);

            var result = await _imageService.AddImagesAsync(
                productId,
                uploads,
                primaryNewImageIndex,
                cancellationToken);

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
        finally
        {
            foreach (Stream stream in streams)
            {
                await stream.DisposeAsync();
            }
        }
    }

    // Updates a product's images by keeping selected existing images, adding new images, and optionally selecting a primary image.
    [HttpPut]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateImages(
        int productId,
        [FromForm] List<int>? keepImageIds,
        [FromForm] List<IFormFile>? newImages,
        [FromForm] int? primaryImageId,
        [FromForm] int? primaryNewImageIndex,
        CancellationToken cancellationToken)
    {
        if (productId <= 0)
        {
            return BadRequest(new
            {
                message = "Product ID must be greater than zero."
            });
        }

        keepImageIds ??= new List<int>();
        newImages ??= new List<IFormFile>();

        if (keepImageIds.Any(id => id <= 0))
        {
            return BadRequest(new
            {
                message = "All image IDs must be greater than zero."
            });
        }

        if (keepImageIds.Distinct().Count() != keepImageIds.Count)
        {
            return BadRequest(new
            {
                message = "Duplicate image IDs are not allowed."
            });
        }

        if (keepImageIds.Count + newImages.Count > MaxImages)
        {
            return BadRequest(new
            {
                message = $"A product cannot have more than {MaxImages} images."
            });
        }

        if (newImages.Any(file => file is null))
        {
            return BadRequest(new
            {
                message = "Uploaded image entries cannot be null."
            });
        }

        if (!ValidateImages(newImages, out string? error))
        {
            return BadRequest(new { message = error });
        }

        if (primaryImageId.HasValue &&
            !keepImageIds.Contains(primaryImageId.Value))
        {
            return BadRequest(new
            {
                message = "PrimaryImageId must be one of the kept image IDs."
            });
        }

        if (primaryNewImageIndex.HasValue &&
            (primaryNewImageIndex.Value < 0 ||
             primaryNewImageIndex.Value >= newImages.Count))
        {
            return BadRequest(new
            {
                message = "PrimaryNewImageIndex is outside the uploaded images range."
            });
        }

        if (primaryImageId.HasValue &&
            primaryNewImageIndex.HasValue)
        {
            return BadRequest(new
            {
                message = "Choose either an existing primary image or a new primary image, not both."
            });
        }

        var streams = new List<Stream>();

        try
        {
            var uploads = CreateUploads(newImages, streams);

            var result = await _imageService.UpdateImagesAsync(
                productId,
                keepImageIds,
                uploads,
                primaryImageId,
                primaryNewImageIndex,
                cancellationToken);

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
        finally
        {
            foreach (Stream stream in streams)
            {
                await stream.DisposeAsync();
            }
        }
    }

    // Deletes a specific image from a product.
    [HttpDelete("{imageId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteImage(
        int productId,
        int imageId,
        CancellationToken cancellationToken)
    {
        if (productId <= 0 || imageId <= 0)
        {
            return BadRequest(new
            {
                message = "Product ID and Image ID must be greater than zero."
            });
        }

        try
        {
            await _imageService.DeleteImageAsync(
                productId,
                imageId,
                cancellationToken);

            return NoContent();
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

    // Validates uploaded images against file size, extension, and content type restrictions.
    private static bool ValidateImages(
        IReadOnlyCollection<IFormFile> images,
        out string? error)
    {
        foreach (IFormFile file in images)
        {
            if (file is null || file.Length == 0)
            {
                error = "Image files cannot be empty.";
                return false;
            }

            if (file.Length > MaxFileSize)
            {
                error = "Each image must be 5 MB or smaller.";
                return false;
            }

            string extension = Path.GetExtension(file.FileName);

            if (!AllowedExtensions.Contains(extension))
            {
                error = "Only JPG, JPEG, PNG, and WebP images are allowed.";
                return false;
            }

            if (!AllowedContentTypes.Contains(file.ContentType))
            {
                error = "Unsupported image content type.";
                return false;
            }
        }

        error = null;
        return true;
    }


    // Creates product image upload objects and tracks the opened streams for later disposal.
    private static List<ProductImageUpload> CreateUploads(
        IReadOnlyCollection<IFormFile> files,
        List<Stream> openedStreams)
    {
        var uploads = new List<ProductImageUpload>();

        foreach (IFormFile file in files)
        {
            Stream stream = file.OpenReadStream();
            openedStreams.Add(stream);

            uploads.Add(new ProductImageUpload(
                content: stream,
                fileName: file.FileName,
                contentType: file.ContentType,
                length: file.Length));
        }

        return uploads;
    }

}