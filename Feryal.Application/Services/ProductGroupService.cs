using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.ProductGroups;
using Feryal.Application.Events.ProductGroups;
using Feryal.Application.Exceptions;
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Feryal.Application.Services.ProductGroups;

// Handles product group operations, including creation, updates, deletion, and cache invalidation.
public class ProductGroupService : IProductGroupService
{
    // Provides database operations for product groups.
    private readonly IProductGroupRepository _productGroupRepository;

    // Coordinates saving changes to the database.
    private readonly IUnitOfWork _unitOfWork;

    // Records product group operations and related events.
    private readonly ILogger<ProductGroupService> _logger;

    // Provides cached product group data.
    private readonly IProductGroupCacheService _productGroupCacheService;

    // Stores cache invalidation events for reliable processing.
    private readonly IOutboxMessageRepository _outboxMessageRepository;

    // Initializes the service with its required dependencies.
    public ProductGroupService(
        IProductGroupRepository productGroupRepository,
        IUnitOfWork unitOfWork,
        ILogger<ProductGroupService> logger,
        IProductGroupCacheService productGroupCacheService,
        IOutboxMessageRepository outboxMessageRepository)
    {
        _productGroupRepository = productGroupRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _productGroupCacheService = productGroupCacheService;
        _outboxMessageRepository = outboxMessageRepository;
    }

    // Retrieves a paginated list of product groups after validating the pagination parameters.
    public async Task<PagedResponse<ProductGroupResponse>> GetAllAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (pageNumber < 1)
        {
            throw new ValidationException(
                "Page number must be at least 1.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ValidationException(
                "Page size must be between 1 and 100.");
        }

        return await _productGroupCacheService.GetAllAsync(
            pageNumber,
            pageSize,
            cancellationToken);
    }

    // Retrieves a product group by its ID or throws an exception if it is not found.
    public async Task<ProductGroupResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var productGroup = await _productGroupCacheService.GetByIdAsync(
            id,
            cancellationToken);

        if (productGroup is null)
        {
            throw new NotFoundException(
                $"Product group with ID {id} was not found.");
        }

        return productGroup;
    }

    // Creates a product group after validating its fields and checking for duplicate names.
    public async Task<ProductGroupResponse> CreateAsync(
        CreateProductGroupRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException(
                "Product group name is required.");
        }

        if (request.Name.Trim().Length > 100)
        {
            throw new ValidationException(
                "Product group name cannot exceed 100 characters.");
        }

        if (request.Description?.Length > 2000)
        {
            throw new ValidationException(
                "Product group description cannot exceed 2000 characters.");
        }

        if (!request.IsActive.HasValue)
        {
            throw new ValidationException(
                "Product group active status must be specified.");
        }

        var name = request.Name.Trim();

        var exists = await _productGroupRepository.ExistsByNameAsync(
            name,
            null,
            cancellationToken);

        if (exists)
        {
            throw new ConflictException(
                "A product group with this name already exists.");
        }

        var productGroup = new ProductGroup(
            name,
            request.Description,
            request.IsActive.Value);

        await _productGroupRepository.AddAsync(
            productGroup,
            cancellationToken);

        AddCacheInvalidationMessage(null);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "Product group created. ProductGroupId: {ProductGroupId}",
            productGroup.Id);

        return MapToResponse(productGroup);
    }

    // Updates the name or description of an existing product group after validating the supplied fields.
    public async Task<ProductGroupResponse> UpdateAsync(
        int id,
        UpdateProductGroupRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var productGroup =
            await _productGroupRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (productGroup is null)
        {
            throw new NotFoundException(
                $"Product group with ID {id} was not found.");
        }

        var hasName = request.Name.ValueKind != JsonValueKind.Undefined;
        var hasDescription =
            request.Description.ValueKind != JsonValueKind.Undefined;

        if (!hasName && !hasDescription)
        {
            throw new ValidationException(
                "At least one field must be provided for update.");
        }

        var name = productGroup.Name;
        var description = productGroup.Description;

        if (hasName)
        {
            if (request.Name.ValueKind != JsonValueKind.String)
            {
                throw new ValidationException(
                    "Product group name must be a string.");
            }

            var suppliedName = request.Name.GetString();

            if (string.IsNullOrWhiteSpace(suppliedName))
            {
                throw new ValidationException(
                    "Product group name cannot be empty.");
            }

            name = suppliedName.Trim();

            if (name.Length > 100)
            {
                throw new ValidationException(
                    "Product group name cannot exceed 100 characters.");
            }
        }

        if (hasDescription)
        {
            if (request.Description.ValueKind == JsonValueKind.Null)
            {
                description = null;
            }
            else if (request.Description.ValueKind == JsonValueKind.String)
            {
                description = request.Description.GetString();

                if (description?.Length > 2000)
                {
                    throw new ValidationException(
                        "Product group description cannot exceed 2000 characters.");
                }
            }
            else
            {
                throw new ValidationException(
                    "Product group description must be a string or null.");
            }
        }

        var exists = await _productGroupRepository.ExistsByNameAsync(
            name,
            id,
            cancellationToken);

        if (exists)
        {
            throw new ConflictException(
                "A product group with this name already exists.");
        }

        productGroup.Update(name, description);

        AddCacheInvalidationMessage(id);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "Product group updated. ProductGroupId: {ProductGroupId}",
            id);

        return MapToResponse(productGroup);
    }

    // Deletes a product group if it exists and has no associated products.
    public async Task DeleteAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var productGroup =
            await _productGroupRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (productGroup is null)
        {
            throw new NotFoundException(
                $"Product group with ID {id} was not found.");
        }

        var hasProducts =
            await _productGroupRepository.HasProductsAsync(
                id,
                cancellationToken);

        if (hasProducts)
        {
            throw new ConflictException(
                "Cannot delete this product group because it has products associated with it.");
        }

        _productGroupRepository.Delete(productGroup);

        AddCacheInvalidationMessage(id);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "Product group deleted. ProductGroupId: {ProductGroupId}",
            id);
    }

    // Activates an existing product group and schedules cache invalidation.
    public async Task<ProductGroupResponse> ActivateAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var productGroup =
            await _productGroupRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (productGroup is null)
        {
            throw new NotFoundException(
                $"Product group with ID {id} was not found.");
        }

        productGroup.Activate();

        AddCacheInvalidationMessage(id);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "Product group activated. ProductGroupId: {ProductGroupId}",
            id);

        return MapToResponse(productGroup);
    }

    // Deactivates an existing product group and schedules cache invalidation.
    public async Task<ProductGroupResponse> DeactivateAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var productGroup =
            await _productGroupRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (productGroup is null)
        {
            throw new NotFoundException(
                $"Product group with ID {id} was not found.");
        }

        productGroup.Deactivate();

        AddCacheInvalidationMessage(id);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "Product group deactivated. ProductGroupId: {ProductGroupId}",
            id);

        return MapToResponse(productGroup);
    }

    // Creates an outbox message requesting invalidation of product group caches.
    private void AddCacheInvalidationMessage(int? productGroupId)
    {
        var cacheInvalidationEvent =
            new ProductGroupCacheInvalidationRequested(productGroupId);

        var payload = JsonSerializer.Serialize(
            cacheInvalidationEvent);

        var outboxMessage = new OutboxMessage(
            typeof(ProductGroupCacheInvalidationRequested).FullName!,
            payload);

        _outboxMessageRepository.Add(outboxMessage);
    }

    // Maps a product group entity to its response DTO.
    private static ProductGroupResponse MapToResponse(
        ProductGroup productGroup)
    {
        return new ProductGroupResponse
        {
            Id = productGroup.Id,
            Name = productGroup.Name,
            Description = productGroup.Description,
            IsActive = productGroup.IsActive,
            CreatedAt = productGroup.CreatedAt,
            UpdatedAt = productGroup.UpdatedAt
        };
    }
}