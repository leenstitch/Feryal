namespace Feryal.Application.Products.Images;

// Represents an uploaded product image and its associated file metadata.
public sealed class ProductImageUpload
{
    // Initializes a new product image upload with its content stream and file metadata.
    public ProductImageUpload(
        Stream content,
        string fileName,
        string contentType,
        long length)
    {
        // Sets the image content stream and throws an exception if it is null.
        Content = content
            ?? throw new ArgumentNullException(nameof(content));

        // Sets the file name and throws an exception if it is null.
        FileName = fileName
            ?? throw new ArgumentNullException(nameof(fileName));

        // Sets the content type and throws an exception if it is null.
        ContentType = contentType
            ?? throw new ArgumentNullException(nameof(contentType));

        // Sets the length of the uploaded file in bytes.
        Length = length;
    }

    // Gets the stream containing the uploaded image data.
    public Stream Content { get; }

    // Gets the name of the uploaded image file.
    public string FileName { get; }

    // Gets the MIME content type of the uploaded image.
    public string ContentType { get; }

    // Gets the size of the uploaded image in bytes.
    public long Length { get; }
}