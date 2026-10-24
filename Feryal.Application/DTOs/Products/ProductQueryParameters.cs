
namespace Feryal.Application.DTOs.Products
{

    public sealed class ProductQueryParameters
    {
        private int _pageNumber = 1;
        private int _pageSize = 12;

        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

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

        public int? CategoryId { get; set; }

        public int? ProductGroupId { get; set; }

        public string? Search { get; set; }
      public bool ? IsActive { get; init; }
    }

}