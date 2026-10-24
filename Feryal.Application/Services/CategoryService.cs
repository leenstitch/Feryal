
using System.Text.Json;
using Feryal.Application.DTOs.Categories;
using Feryal.Application.DTOs.Common;
using Feryal.Application.Events.Categories;
using Feryal.Application.Exceptions;
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Feryal.Application.Services.Categories;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CategoryService> _logger;
    private readonly ICategoryCacheService _categoryCacheService;
    private readonly IOutboxMessageRepository _outboxMessageRepository;

    public CategoryService(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        ILogger<CategoryService> logger,
        ICategoryCacheService categoryCacheService,
        IOutboxMessageRepository outboxMessageRepository)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _categoryCacheService = categoryCacheService;
        _outboxMessageRepository = outboxMessageRepository;
    }

    public async Task<PagedResponse<CategoryResponse>> GetAllAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (pageNumber < 1)
        {
            throw new InvalidOperationException(
                "Page number must be at least 1.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new InvalidOperationException(
                "Page size must be between 1 and 100.");
        }

        return await _categoryCacheService.GetAllAsync(
            pageNumber,
            pageSize,
            cancellationToken);
    }

    public async Task<CategoryResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var category = await _categoryCacheService.GetByIdAsync(
            id,
            cancellationToken);

        if (category is null)
        {
            throw new NotFoundException(
                $"Category with ID {id} was not found.");
        }

        return category;
    }

    public async Task<CategoryResponse> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException(
                "Category name is required.");
        }

        if (!request.IsActive.HasValue)
        {
            throw new InvalidOperationException(
                "Category active status must be specified.");
        }

        var name = request.Name.Trim();

        var nameExists = await _categoryRepository.ExistsByNameAsync(
            name,
            null,
            cancellationToken);

        if (nameExists)
        {
            _logger.LogWarning(
                "Category creation failed because the name already exists.");

            throw new ConflictException(
                "A category with this name already exists.");
        }

        var category = new Category(
            name,
            request.Description,
            request.IsActive.Value);

        await _categoryRepository.AddAsync(
            category,
            cancellationToken);

        AddCacheInvalidationMessage(null);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "Category created successfully. CategoryId: {CategoryId}",
            category.Id);

        return MapToResponse(category);
    }

    public async Task<CategoryResponse> UpdateAsync(
        int id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var category = await _categoryRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (category is null)
        {
            throw new NotFoundException(
                $"Category with ID {id} was not found.");
        }

        if (!request.Name.HasValue &&
            !request.Description.HasValue)
        {
            throw new InvalidOperationException(
                "At least one field must be provided for update.");
        }

        var name = category.Name;
        var description = category.Description;

        if (request.Name.HasValue)
        {
            var nameElement = request.Name.Value;

            if (nameElement.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException(
                    "Category name must be a string.");
            }

            var requestedName = nameElement.GetString();

            if (string.IsNullOrWhiteSpace(requestedName))
            {
                throw new InvalidOperationException(
                    "Category name cannot be empty.");
            }

            name = requestedName.Trim();

            var nameExists = await _categoryRepository.ExistsByNameAsync(
                name,
                id,
                cancellationToken);

            if (nameExists)
            {
                throw new ConflictException(
                    "A category with this name already exists.");
            }
        }

        if (request.Description.HasValue)
        {
            var descriptionElement = request.Description.Value;

            if (descriptionElement.ValueKind == JsonValueKind.Null)
            {
                description = null;
            }
            else if (descriptionElement.ValueKind == JsonValueKind.String)
            {
                description = descriptionElement.GetString()?.Trim();

                if (string.IsNullOrWhiteSpace(description))
                {
                    description = null;
                }
            }
            else
            {
                throw new InvalidOperationException(
                    "Category description must be a string or null.");
            }
        }

        category.Update(name, description);

        AddCacheInvalidationMessage(id);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "Category updated successfully. CategoryId: {CategoryId}",
            category.Id);

        return MapToResponse(category);
    }

    public async Task DeleteAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (category is null)
        {
            throw new NotFoundException(
                $"Category with ID {id} was not found.");
        }

        var hasProducts = await _categoryRepository.HasProductsAsync(
            id,
            cancellationToken);

        if (hasProducts)
        {
            throw new ConflictException(
                "This category cannot be deleted because it has products.");
        }

        _categoryRepository.Delete(category);

        AddCacheInvalidationMessage(id);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "Category deleted successfully. CategoryId: {CategoryId}",
            id);
    }

    public async Task<CategoryResponse> ActivateAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (category is null)
        {
            throw new NotFoundException(
                $"Category with ID {id} was not found.");
        }

        category.Activate();

        AddCacheInvalidationMessage(id);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "Category activated. CategoryId: {CategoryId}",
            id);

        return MapToResponse(category);
    }

    public async Task<CategoryResponse> DeactivateAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (category is null)
        {
            throw new NotFoundException(
                $"Category with ID {id} was not found.");
        }

        category.Deactivate();

        AddCacheInvalidationMessage(id);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "Category deactivated. CategoryId: {CategoryId}",
            id);

        return MapToResponse(category);
    }

    private void AddCacheInvalidationMessage(int? categoryId)
    {
        var cacheInvalidationEvent =
            new CategoryCacheInvalidationRequested(categoryId);

        var payload = JsonSerializer.Serialize(
            cacheInvalidationEvent);

        var outboxMessage = new OutboxMessage(
            typeof(CategoryCacheInvalidationRequested).FullName!,
            payload);

        _outboxMessageRepository.Add(outboxMessage);
    }

    private static CategoryResponse MapToResponse(Category category)
    {
        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }
}