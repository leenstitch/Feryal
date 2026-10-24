namespace Feryal.Application.DTOs.Products
{
    // Represents the parameters used to filter and paginate product queries.
    public sealed class ProductQueryParameters
    {
        // Stores the current page number, defaulting to page 1.
        private int _pageNumber = 1;

        // Stores the number of products per page, defaulting to 12.
        private int _pageSize = 12;

        // Gets or sets the page number, ensuring it is at least 1.
        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

        // Gets or sets the page size, defaulting to 12 if below 1 and limiting it to 100.
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value switch
            {
                < 1 => 12,
                > 100 => 100,
                _ => value
            };
        }

        // Gets or sets the category ID used to filter products.
        public int? CategoryId { get; set; }

        // Gets or sets the product group ID used to filter products.
        public int? ProductGroupId { get; set; }

        // Gets or sets the search term used to filter products by matching text.
        public string? Search { get; set; }

        // Gets or initializes the active status used to filter products.
        public bool? IsActive { get; init; }
    }

}