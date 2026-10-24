using Feryal.Application.DTOs.Products;
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Infrastructure.Persistence;
using Feryal.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Feryal.Infrastructure.Repositories;

// Provides database operations for products and their variants.
public sealed class ProductRepository : Repository<Product>, IProductRepository
{
    private readonly ApplicationDbContext _dbContext;

    // Inject the database context and initialize the base repository.
    public ProductRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    // Retrieves a paginated list of products using the supplied filters.
    public async Task<(List<Product> Items, int TotalCount)> GetPagedAsync(
        ProductQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        // Ensure the query parameters object is provided.
        ArgumentNullException.ThrowIfNull(parameters);

        // Build a read-only query for products.
        IQueryable<Product> query = _dbContext.Products
            .AsNoTracking();

        // Filter products by category when a category ID is provided.
        if (parameters.CategoryId.HasValue)
        {
            query = query.Where(product =>
                product.CategoryId == parameters.CategoryId.Value);
        }

        // Filter products by product group when a group ID is provided.
        if (parameters.ProductGroupId.HasValue)
        {
            query = query.Where(product =>
                product.ProductGroupId == parameters.ProductGroupId.Value);
        }

        // Filter products by their active status when specified.
        if (parameters.IsActive.HasValue)
        {
            query = query.Where(product =>
                product.IsActive == parameters.IsActive.Value);
        }

        // Search product names and descriptions when a search term is provided.
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            string search = parameters.Search.Trim();

            query = query.Where(product =>
                product.Name.Contains(search) ||
                (product.Description != null &&
                 product.Description.Contains(search)));
        }

        // Count matching products before applying pagination.
        int totalCount = await query.CountAsync(cancellationToken);

        // Normalize pagination values to keep the page number and size within valid bounds.
        int pageNumber = Math.Max(1, parameters.PageNumber);
        int pageSize = Math.Clamp(parameters.PageSize, 1, 100);

        // Retrieve the requested page with product options, variants, and related details.
        List<Product> products = await query
            .OrderByDescending(product => product.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            // Split the query to avoid large joins across multiple collections.
            .AsSplitQuery()
            .Include(product => product.Images)
            .Include(product => product.Colors)
            .Include(product => product.Sizes)
            .Include(product => product.Scents)
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.Color)
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.Size)
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.Scent)
            .ToListAsync(cancellationToken);

        // Return the requested page and the total number of matching products.
        return (products, totalCount);
    }

    // Retrieves a product with all its details for read-only operations.
    public async Task<Product?> GetByIdWithDetailsAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .AsNoTracking()
            // Split related collection queries to reduce join duplication.
            .AsSplitQuery()
            .Include(product => product.Colors)
            .Include(product => product.Sizes)
            .Include(product => product.Scents)
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.Color)
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.Size)
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.Scent)
            .Include(product => product.Images)
            .FirstOrDefaultAsync(
                product => product.Id == productId,
                cancellationToken);
    }

    // Retrieves a paginated list of active products using the supplied filters.
    public async Task<(List<Product> Items, int TotalCount)> GetPagedActiveAsync(
        ProductQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        // Ensure the query parameters object is provided.
        ArgumentNullException.ThrowIfNull(parameters);

        // Restrict the query to active products.
        IQueryable<Product> query = _dbContext.Products
            .AsNoTracking()
            .Where(product => product.IsActive);

        // Filter by category when a category ID is provided.
        if (parameters.CategoryId.HasValue)
        {
            query = query.Where(product =>
                product.CategoryId == parameters.CategoryId.Value);
        }

        // Filter by product group when a group ID is provided.
        if (parameters.ProductGroupId.HasValue)
        {
            query = query.Where(product =>
                product.ProductGroupId == parameters.ProductGroupId.Value);
        }

        // Search product names and descriptions when a search term is provided.
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            string search = parameters.Search.Trim();

            query = query.Where(product =>
                product.Name.Contains(search) ||
                (product.Description != null &&
                 product.Description.Contains(search)));
        }

        // Count matching active products before applying pagination.
        int totalCount = await query.CountAsync(cancellationToken);

        // Normalize pagination values to valid bounds.
        int pageNumber = Math.Max(1, parameters.PageNumber);
        int pageSize = Math.Clamp(parameters.PageSize, 1, 100);

        // Retrieve the requested page with images, options, and variant details.
        List<Product> products = await query
            .OrderByDescending(product => product.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .Include(product => product.Images)
            .Include(product => product.Colors)
            .Include(product => product.Sizes)
            .Include(product => product.Scents)
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.Color)
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.Size)
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.Scent)
            .ToListAsync(cancellationToken);

        // Return the active products and the total matching count.
        return (products, totalCount);
    }

    // Retrieves a product and its related entities for update operations.
    // Tracking is enabled so changes can be persisted through the database context.
    public async Task<Product?> GetByIdWithDetailsForUpdateAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .AsSplitQuery()
            .Include(product => product.Colors)
            .Include(product => product.Sizes)
            .Include(product => product.Scents)
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.Color)
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.Size)
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.Scent)
            .Include(product => product.Images)
            .FirstOrDefaultAsync(
                product => product.Id == productId,
                cancellationToken);
    }

    // Checks whether a product with the specified name exists.
    // Optionally excludes a product ID when checking during an update.
    public async Task<bool> ExistsByNameAsync(
        string name,
        int? excludeProductId,
        CancellationToken cancellationToken)
    {
        // Build a query matching the specified product name.
        IQueryable<Product> query = _dbContext.Products
            .Where(product => product.Name == name);

        // Exclude the current product when an ID is provided.
        if (excludeProductId.HasValue)
        {
            query = query.Where(product =>
                product.Id != excludeProductId.Value);
        }

        // Return true if a matching product exists.
        return await query.AnyAsync(cancellationToken);
    }

    // Checks whether the specified category exists and is active.
    public async Task<bool> IsCategoryAvailableAsync(
        int categoryId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Categories
            .AnyAsync(
                category => category.Id == categoryId &&
                            category.IsActive,
                cancellationToken);
    }

    // Checks whether the specified product group exists and is active.
    public async Task<bool> IsProductGroupAvailableAsync(
        int productGroupId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ProductGroups
            .AnyAsync(
                group => group.Id == productGroupId &&
                         group.IsActive,
                cancellationToken);
    }

    // Retrieves all variants belonging to a product, including their option details.
    // Returns null when the product does not exist.
    public async Task<List<ProductVariant>?> GetVariantsByProductIdAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        // Check whether the product exists.
        bool productExists = await _dbContext.Products
            .AnyAsync(
                product => product.Id == productId,
                cancellationToken);

        // Distinguish a nonexistent product from a product with no variants.
        if (!productExists)
        {
            return null;
        }

        // Retrieve the product's variants in ID order without tracking them.
        return await _dbContext.ProductVariants
            .AsNoTracking()
            .Include(variant => variant.Color)
            .Include(variant => variant.Size)
            .Include(variant => variant.Scent)
            .Where(variant => variant.ProductId == productId)
            .OrderBy(variant => variant.Id)
            .ToListAsync(cancellationToken);
    }

    // Retrieves only visible variants that meet the specified availability conditions.
    // Returns null when the product does not exist.
    public async Task<List<ProductVariant>?> GetVisibleVariantsByProductIdAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        // Check whether the product exists.
        bool productExists = await _dbContext.Products
            .AnyAsync(
                product => product.Id == productId,
                cancellationToken);

        // Return null when the product cannot be found.
        if (!productExists)
        {
            return null;
        }

        // Retrieve variants that are active, configured, priced, and have positive stock.
        return await _dbContext.ProductVariants
            .AsNoTracking()
            .Include(variant => variant.Color)
            .Include(variant => variant.Size)
            .Include(variant => variant.Scent)
            .Where(variant =>
                variant.ProductId == productId &&
                variant.IsActive &&
                variant.IsConfigured &&
                variant.Price.HasValue &&
                variant.Price.Value > 0 &&
                variant.StockQuantity.HasValue &&
                variant.StockQuantity.Value > 0)
            .OrderBy(variant => variant.Id)
            .ToListAsync(cancellationToken);
    }

    // Retrieves a paginated list of variants for a product.
    // Returns ProductExists=false when the product does not exist.
    public async Task<(bool ProductExists, List<ProductVariant> Items, int TotalCount)>
        GetPagedVariantsByProductIdAsync(
            int productId,
            ProductVariantQueryParameters parameters,
            CancellationToken cancellationToken)
    {
        // Check whether the specified product exists.
        bool productExists = await _dbContext.Products
            .AnyAsync(
                product => product.Id == productId,
                cancellationToken);

        // Return an empty result when the product does not exist.
        if (!productExists)
        {
            return (false, [], 0);
        }

        // Build a query for all variants belonging to this product.
        IQueryable<ProductVariant> query = _dbContext.ProductVariants
            .AsNoTracking()
            .Include(variant => variant.Color)
            .Include(variant => variant.Size)
            .Include(variant => variant.Scent)
            .Where(variant => variant.ProductId == productId);

        // Count all matching variants before pagination.
        int totalCount = await query.CountAsync(cancellationToken);

        // Normalize pagination values to valid bounds.
        int pageNumber = Math.Max(1, parameters.PageNumber);
        int pageSize = Math.Clamp(parameters.PageSize, 1, 100);

        // Retrieve the requested page in variant ID order.
        List<ProductVariant> items = await query
            .OrderBy(variant => variant.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Return the existence flag, requested page, and total variant count.
        return (true, items, totalCount);
    }

    // Retrieves a paginated list of configured variants for an active product.
    public async Task<(bool ProductExists, List<ProductVariant> Items, int TotalCount)>
        GetPagedAvailableVariantsByProductIdAsync(
            int productId,
            ProductVariantQueryParameters parameters,
            CancellationToken cancellationToken)
    {
        // Check whether the product exists and is active.
        bool productExists = await _dbContext.Products
            .AnyAsync(
                product => product.Id == productId && product.IsActive,
                cancellationToken);

        // Return an empty result when the product is missing or inactive.
        if (!productExists)
        {
            return (false, [], 0);
        }

        // Retrieve configured variants and include their option details.
        IQueryable<ProductVariant> query = _dbContext.ProductVariants
            .AsNoTracking()
            .Include(variant => variant.Color)
            .Include(variant => variant.Size)
            .Include(variant => variant.Scent)
            .Where(variant =>
                variant.ProductId == productId &&
                variant.IsConfigured);

        // Count matching configured variants before pagination.
        int totalCount = await query.CountAsync(cancellationToken);

        // Normalize pagination values to valid bounds.
        int pageNumber = Math.Max(1, parameters.PageNumber);
        int pageSize = Math.Clamp(parameters.PageSize, 1, 100);

        // Retrieve the requested page in variant ID order.
        List<ProductVariant> items = await query
            .OrderBy(variant => variant.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Return the product existence flag, page of variants, and total count.
        return (true, items, totalCount);
    }

    // Retrieves a product variant for update operations with its related option entities.
    // Tracking remains enabled so changes can be persisted.
    public async Task<ProductVariant?> GetVariantByIdForUpdateAsync(
        int variantId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ProductVariants
            .Include(variant => variant.Color)
            .Include(variant => variant.Size)
            .Include(variant => variant.Scent)
            .FirstOrDefaultAsync(
                variant => variant.Id == variantId,
                cancellationToken);
    }
}