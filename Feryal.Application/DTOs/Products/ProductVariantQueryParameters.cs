using System;
using System.Collections.Generic;
using System.Text;

namespace Feryal.Application.DTOs.Products
{
    // Represents the parameters used to paginate product variant queries.
    public sealed class ProductVariantQueryParameters
    {
        // Stores the current page number, defaulting to page 1.
        private int _pageNumber = 1;

        // Stores the number of product variants per page, defaulting to 20.
        private int _pageSize = 20;

        // Gets or sets the page number, ensuring it is at least 1.
        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

        // Gets or sets the page size, defaulting to 20 if below 1 and limiting it to 100.
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value switch
            {
                < 1 => 20,
                > 100 => 100,
                _ => value
            };
        }
    }
}