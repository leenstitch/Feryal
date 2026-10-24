
using Feryal.Application.DTOs.Products;
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Infrastructure.Persistence;
using Feryal.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Feryal.Infrastructure.Repositories;

public sealed class ProductRepository : Repository<Product>, IProductRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ProductRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }


    public async Task<(List<Product> Items, int TotalCount)> GetPagedAsync(
        ProductQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        IQueryable<Product> query = _dbContext.Products
            .AsNoTracking();

        if (parameters.CategoryId.HasValue)
        {
            query = query.Where(product =>
                product.CategoryId == parameters.CategoryId.Value);
        }

        if (parameters.ProductGroupId.HasValue)
        {
            query = query.Where(product =>
                product.ProductGroupId == parameters.ProductGroupId.Value);
        }

        if (parameters.IsActive.HasValue)
        {
            query = query.Where(product =>
                product.IsActive == parameters.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            string search = parameters.Search.Trim();

            query = query.Where(product =>
                product.Name.Contains(search) ||
                (product.Description != null &&
                 product.Description.Contains(search)));
        }

        int totalCount = await query.CountAsync(cancellationToken);

        int pageNumber = Math.Max(1, parameters.PageNumber);
        int pageSize = Math.Clamp(parameters.PageSize, 1, 100);

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

        return (products, totalCount);
    }



    public async Task<Product?> GetByIdWithDetailsAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .AsNoTracking()
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

    public async Task<(List<Product> Items, int TotalCount)> GetPagedActiveAsync(
        ProductQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        IQueryable<Product> query = _dbContext.Products
            .AsNoTracking()
            .Where(product => product.IsActive);

        if (parameters.CategoryId.HasValue)
        {
            query = query.Where(product =>
                product.CategoryId == parameters.CategoryId.Value);
        }

        if (parameters.ProductGroupId.HasValue)
        {
            query = query.Where(product =>
                product.ProductGroupId == parameters.ProductGroupId.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            string search = parameters.Search.Trim();

            query = query.Where(product =>
                product.Name.Contains(search) ||
                (product.Description != null &&
                 product.Description.Contains(search)));
        }

        int totalCount = await query.CountAsync(cancellationToken);

        int pageNumber = Math.Max(1, parameters.PageNumber);
        int pageSize = Math.Clamp(parameters.PageSize, 1, 100);

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

        return (products, totalCount);
    }

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

    public async Task<bool> ExistsByNameAsync(
        string name,
        int? excludeProductId,
        CancellationToken cancellationToken)
    {
        IQueryable<Product> query = _dbContext.Products
            .Where(product => product.Name == name);

        if (excludeProductId.HasValue)
        {
            query = query.Where(product =>
                product.Id != excludeProductId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

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

    public async Task<List<ProductVariant>?> GetVariantsByProductIdAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        bool productExists = await _dbContext.Products
            .AnyAsync(
                product => product.Id == productId,
                cancellationToken);

        if (!productExists)
        {
            return null;
        }

        return await _dbContext.ProductVariants
            .AsNoTracking()
            .Include(variant => variant.Color)
            .Include(variant => variant.Size)
            .Include(variant => variant.Scent)
            .Where(variant => variant.ProductId == productId)
            .OrderBy(variant => variant.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ProductVariant>?> GetVisibleVariantsByProductIdAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        bool productExists = await _dbContext.Products
            .AnyAsync(
                product => product.Id == productId,
                cancellationToken);

        if (!productExists)
        {
            return null;
        }

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

    public async Task<(bool ProductExists, List<ProductVariant> Items, int TotalCount)>
        GetPagedVariantsByProductIdAsync(
            int productId,
            ProductVariantQueryParameters parameters,
            CancellationToken cancellationToken)
    {
        bool productExists = await _dbContext.Products
            .AnyAsync(
                product => product.Id == productId,
                cancellationToken);

        if (!productExists)
        {
            return (false, [], 0);
        }

        IQueryable<ProductVariant> query = _dbContext.ProductVariants
            .AsNoTracking()
            .Include(variant => variant.Color)
            .Include(variant => variant.Size)
            .Include(variant => variant.Scent)
            .Where(variant => variant.ProductId == productId);

        int totalCount = await query.CountAsync(cancellationToken);

        int pageNumber = Math.Max(1, parameters.PageNumber);
        int pageSize = Math.Clamp(parameters.PageSize, 1, 100);

        List<ProductVariant> items = await query
            .OrderBy(variant => variant.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (true, items, totalCount);
    }


    public async Task<(bool ProductExists, List<ProductVariant> Items, int TotalCount)>
        GetPagedAvailableVariantsByProductIdAsync(
            int productId,
            ProductVariantQueryParameters parameters,
            CancellationToken cancellationToken)
    {
        bool productExists = await _dbContext.Products
            .AnyAsync(
                product => product.Id == productId && product.IsActive,
                cancellationToken);

        if (!productExists)
        {
            return (false, [], 0);
        }

        IQueryable<ProductVariant> query = _dbContext.ProductVariants
            .AsNoTracking()
            .Include(variant => variant.Color)
            .Include(variant => variant.Size)
            .Include(variant => variant.Scent)
            .Where(variant =>
                variant.ProductId == productId &&
                variant.IsConfigured);

        int totalCount = await query.CountAsync(cancellationToken);

        int pageNumber = Math.Max(1, parameters.PageNumber);
        int pageSize = Math.Clamp(parameters.PageSize, 1, 100);

        List<ProductVariant> items = await query
            .OrderBy(variant => variant.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (true, items, totalCount);
    }


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
