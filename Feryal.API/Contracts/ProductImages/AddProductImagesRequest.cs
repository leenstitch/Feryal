using Microsoft.AspNetCore.Http;

namespace Feryal.API.Contracts.ProductImages;

public sealed class AddProductImagesRequest
{
   
    // Gets or sets the list of new image files to be added to a product.
    public List<IFormFile> NewImages { get; set; } = [];

    
    // Gets or sets the zero-based index of the new image to be designated as the primary image.
    public int? PrimaryNewImageIndex { get; set; }
}