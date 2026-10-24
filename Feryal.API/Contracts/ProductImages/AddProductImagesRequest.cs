
using Microsoft.AspNetCore.Http;

namespace Feryal.API.Contracts.ProductImages;

public sealed class AddProductImagesRequest
{
    public List<IFormFile> NewImages { get; set; } = [];

    // Zero-based index in NewImages.
    public int? PrimaryNewImageIndex { get; set; }
}