namespace Feryal.Application.Products.Images;

public sealed class ProductImageUpload
{
    public ProductImageUpload(
        Stream content,
        string fileName,
        string contentType,
        long length)
    {
        Content = content
            ?? throw new ArgumentNullException(nameof(content));

        FileName = fileName
            ?? throw new ArgumentNullException(nameof(fileName));

        ContentType = contentType
            ?? throw new ArgumentNullException(nameof(contentType));

        Length = length;
    }

    public Stream Content { get; }

    public string FileName { get; }

    public string ContentType { get; }

    public long Length { get; }
}