
using Microsoft.AspNetCore.Http;

namespace Feryal.API.Contracts.ProductImages;

public sealed class UpdateProductImagesRequest
{
    // Keep only these existing image IDs.
    // Existing images omitted from this list will be removed.
    public List<int> KeepImageIds { get; set; } = [];

    // New images to upload.
    public List<IFormFile> NewImages { get; set; } = [];

    // Select an existing image to become primary.
    public int? PrimaryImageId { get; set; }

    // Alternatively, select a new image by its zero-based index.
    public int? PrimaryNewImageIndex { get; set; }
}