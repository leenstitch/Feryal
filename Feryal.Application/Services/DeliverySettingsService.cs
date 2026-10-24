using System.Data;
using System.Text.Json;
using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.Delivery;
using Feryal.Application.Events.Delivery;
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Application.Interfaces;

namespace Feryal.Application.Delivery.Services;

// Handles delivery city and pickup point operations, including validation, persistence, and cache invalidation.
public sealed class DeliverySettingsService : IDeliverySettingsService
{
    // Provides database operations for delivery cities.
    private readonly IRepository<DeliveryCity> _cityRepository;

    // Provides database operations for pickup points.
    private readonly IRepository<PickupPoint> _pickupPointRepository;

    // Provides query operations for checking delivery settings and retrieving related data.
    private readonly IDeliverySettingsQueryRepository _queryRepository;

    // Retrieves cached delivery settings and manages cache invalidation.
    private readonly IDeliverySettingsCacheService _cacheService;

    // Stores cache invalidation events in the outbox for reliable processing.
    private readonly IOutboxMessageRepository _outboxRepository;

    // Coordinates saving changes to the database.
    private readonly IUnitOfWork _unitOfWork;

    // Executes operations within database transactions.
    private readonly ITransactionManager _transactionManager;

    // Initializes the delivery settings service with its required dependencies.
    public DeliverySettingsService(
        IRepository<DeliveryCity> cityRepository,
        IRepository<PickupPoint> pickupPointRepository,
        IDeliverySettingsQueryRepository queryRepository,
        IDeliverySettingsCacheService cacheService,
        IOutboxMessageRepository outboxRepository,
        IUnitOfWork unitOfWork,
        ITransactionManager transactionManager)
    {
        _cityRepository = cityRepository;
        _pickupPointRepository = pickupPointRepository;
        _queryRepository = queryRepository;
        _cacheService = cacheService;
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
        _transactionManager = transactionManager;
    }

    // Retrieves a paginated list of delivery cities, optionally filtering for active cities only.
    public Task<PagedResponse<DeliveryCityResponse>> GetCitiesAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        ValidatePagination(pageNumber, pageSize);

        return _cacheService.GetCitiesAsync(
            pageNumber,
            pageSize,
            activeOnly,
            cancellationToken);
    }

    // Retrieves a delivery city by its ID after validating the ID.
    public Task<DeliveryCityResponse?> GetCityByIdAsync(
        int cityId,
        CancellationToken cancellationToken)
    {
        ValidateId(cityId, nameof(cityId));

        return _cacheService.GetCityByIdAsync(
            cityId,
            cancellationToken);
    }

    // Retrieves a paginated list of pickup points, optionally filtering for active pickup points only.
    public Task<PagedResponse<PickupPointResponse>> GetPickupPointsAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        ValidatePagination(pageNumber, pageSize);

        return _cacheService.GetPickupPointsAsync(
            pageNumber,
            pageSize,
            activeOnly,
            cancellationToken);
    }

    // Retrieves a pickup point by its ID after validating the ID.
    public Task<PickupPointResponse?> GetPickupPointByIdAsync(
        int pickupPointId,
        CancellationToken cancellationToken)
    {
        ValidateId(pickupPointId, nameof(pickupPointId));

        return _cacheService.GetPickupPointByIdAsync(
            pickupPointId,
            cancellationToken);
    }

    // Creates a delivery city after validating its name and fee, then saves a cache invalidation event.
    public async Task<DeliveryCityResponse> CreateCityAsync(
        CreateDeliveryCityRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string name = ValidateName(
            request.Name,
            100,
            "City name");

        ValidateFee(request.DeliveryFee, "DeliveryFee");

        return await _transactionManager.ExecuteAsync(
            async token =>
            {
                if (await _queryRepository.CityNameExistsAsync(
                    name,
                    null,
                    token))
                {
                    throw new InvalidOperationException(
                        "A delivery city with this name already exists.");
                }

                var city = new DeliveryCity(
                    name,
                    request.DeliveryFee);

                await _cityRepository.AddAsync(city, token);

                // Saves the city first to generate its ID.
                await _unitOfWork.SaveChangesAsync(token);

                AddCacheInvalidation(city.Id, null);

                // Saves the outbox message within the same transaction.
                await _unitOfWork.SaveChangesAsync(token);

                return MapCity(city);
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }

    // Partially updates a delivery city's name, fee, or active status and schedules cache invalidation.
    public async Task<DeliveryCityResponse?> PatchCityAsync(
        int cityId,
        PatchDeliveryCityRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateId(cityId, nameof(cityId));

        return await _transactionManager.ExecuteAsync(
            async token =>
            {
                var city = await _cityRepository.GetByIdAsync(
                    cityId,
                    token);

                if (city is null)
                {
                    return null;
                }

                string name = city.Name;
                decimal fee = city.DeliveryFee;

                if (request.Name.ValueKind != JsonValueKind.Undefined)
                {
                    if (request.Name.ValueKind != JsonValueKind.String)
                    {
                        throw new ArgumentException(
                            "Name must be a non-empty string.");
                    }

                    name = ValidateName(
                        request.Name.GetString(),
                        100,
                        "Name");
                }

                if (request.DeliveryFee.ValueKind != JsonValueKind.Undefined)
                {
                    fee = ReadFee(
                        request.DeliveryFee,
                        "DeliveryFee");
                }

                if (!string.Equals(
                    name,
                    city.Name,
                    StringComparison.OrdinalIgnoreCase))
                {
                    if (await _queryRepository.CityNameExistsAsync(
                        name,
                        cityId,
                        token))
                    {
                        throw new InvalidOperationException(
                            "A delivery city with this name already exists.");
                    }
                }

                bool? isActive = ReadOptionalBoolean(
                    request.IsActive,
                    "IsActive");

                city.Update(name, fee);

                if (isActive.HasValue)
                {
                    if (isActive.Value)
                    {
                        city.Activate();
                    }
                    else
                    {
                        city.Deactivate();
                    }
                }

                _cityRepository.Update(city);

                AddCacheInvalidation(city.Id, null);

                await _unitOfWork.SaveChangesAsync(token);

                return MapCity(city);
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }

    // Creates a pickup point after validating its name, address, and fee, then schedules cache invalidation.
    public async Task<PickupPointResponse> CreatePickupPointAsync(
        CreatePickupPointRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string name = ValidateName(
            request.Name,
            150,
            "Pickup point name");

        string address = ValidateName(
            request.Address,
            500,
            "Address");

        ValidateFee(request.PickupFee, "PickupFee");

        return await _transactionManager.ExecuteAsync(
            async token =>
            {
                if (await _queryRepository.pickuppointNameExistsAsync(
                   name,
                   null,
                   token))
                {
                    throw new InvalidOperationException(
                        "A pickup point  with this name already exists.");
                }

                // Creates a pickup point independently of any delivery city.
                var point = new PickupPoint(
                    name,
                    address,
                    request.PickupFee);

                await _pickupPointRepository.AddAsync(point, token);

                // Saves the pickup point first to generate its ID.
                await _unitOfWork.SaveChangesAsync(token);

                AddCacheInvalidation(null, point.Id);

                await _unitOfWork.SaveChangesAsync(token);

                return MapPickupPoint(point);
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }

    // Partially updates a pickup point's name, address, fee, or active status.
    public async Task<PickupPointResponse?> PatchPickupPointAsync(
        int pickupPointId,
        PatchPickupPointRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateId(pickupPointId, nameof(pickupPointId));

        return await _transactionManager.ExecuteAsync(
            async token =>
            {
                var point = await _pickupPointRepository.GetByIdAsync(
                    pickupPointId,
                    token);

                if (point is null)
                {
                    return null;
                }

                string name = point.Name;
                string address = point.Address;
                decimal fee = point.PickupFee;

                if (request.Name.ValueKind != JsonValueKind.Undefined)
                {
                    if (request.Name.ValueKind != JsonValueKind.String)
                    {
                        throw new ArgumentException(
                            "Name must be a non-empty string.");
                    }

                    name = ValidateName(
                        request.Name.GetString(),
                        150,
                        "Name");
                }

                if (request.Address.ValueKind != JsonValueKind.Undefined)
                {
                    if (request.Address.ValueKind != JsonValueKind.String)
                    {
                        throw new ArgumentException(
                            "Address must be a non-empty string.");
                    }

                    address = ValidateName(
                        request.Address.GetString(),
                        500,
                        "Address");
                }

                if (request.PickupFee.ValueKind != JsonValueKind.Undefined)
                {
                    fee = ReadFee(
                        request.PickupFee,
                        "PickupFee");
                }

                bool? isActive = ReadOptionalBoolean(
                    request.IsActive,
                    "IsActive");

                point.Update(name, address, fee);

                // Applies activation or deactivation when an active status is provided.
                if (isActive.HasValue)
                {
                    if (isActive.Value)
                    {
                        point.Activate();
                    }
                    else
                    {
                        point.Deactivate();
                    }
                }

                _pickupPointRepository.Update(point);

                AddCacheInvalidation(null, point.Id);

                await _unitOfWork.SaveChangesAsync(token);

                return MapPickupPoint(point);
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }

    // Creates an outbox message requesting cache invalidation for the specified delivery city or pickup point.
    private void AddCacheInvalidation(
        int? deliveryCityId,
        int? pickupPointId)
    {
        var cacheEvent = new DeliverySettingsCacheInvalidationRequested(
            deliveryCityId,
            pickupPointId);

        string payload = JsonSerializer.Serialize(cacheEvent);

        var message = new OutboxMessage(
            typeof(DeliverySettingsCacheInvalidationRequested).FullName!,
            payload);

        _outboxRepository.Add(message);
    }

    // Validates that a string is not empty and does not exceed the specified maximum length.
    private static string ValidateName(
        string? value,
        int maxLength,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{fieldName} is required.");
        }

        string result = value.Trim();

        if (result.Length > maxLength)
        {
            throw new ArgumentException(
                $"{fieldName} cannot exceed {maxLength} characters.");
        }

        return result;
    }

    // Validates that a delivery or pickup fee is non-negative.
    private static void ValidateFee(
        decimal fee,
        string fieldName)
    {
        if (fee < 0)
        {
            throw new ArgumentOutOfRangeException(
                fieldName,
                "Fee cannot be negative.");
        }
    }

    // Reads a non-negative decimal fee from a JSON element.
    private static decimal ReadFee(
        JsonElement element,
        string fieldName)
    {
        if (element.ValueKind != JsonValueKind.Number ||
            !element.TryGetDecimal(out decimal fee) ||
            fee < 0)
        {
            throw new ArgumentException(
                $"{fieldName} must be a non-negative number.");
        }

        return fee;
    }

    // Reads an optional Boolean value from a JSON element, returning null when it is undefined.
    private static bool? ReadOptionalBoolean(
        JsonElement element,
        string fieldName)
    {
        if (element.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        if (element.ValueKind is JsonValueKind.True
            or JsonValueKind.False)
        {
            return element.GetBoolean();
        }

        throw new ArgumentException(
            $"{fieldName} must be true or false.");
    }

    // Validates that the supplied ID is a positive integer.
    private static void ValidateId(
        int id,
        string fieldName)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(
                fieldName,
                "ID must be greater than zero.");
        }
    }

    // Validates the page number and page size for paginated queries.
    private static void ValidatePagination(
        int pageNumber,
        int pageSize)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageNumber),
                "Page number must be at least 1.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "Page size must be between 1 and 100.");
        }
    }

    // Maps a delivery city entity to its response DTO.
    private static DeliveryCityResponse MapCity(
        DeliveryCity city)
    {
        return new DeliveryCityResponse
        {
            Id = city.Id,
            Name = city.Name,
            DeliveryFee = city.DeliveryFee,
            IsActive = city.IsActive,
            CreatedAt = city.CreatedAt,
            UpdatedAt = city.UpdatedAt
        };
    }

    // Maps a pickup point entity to its response DTO.
    private static PickupPointResponse MapPickupPoint(
        PickupPoint point)
    {
        return new PickupPointResponse
        {
            Id = point.Id,
            Name = point.Name,
            Address = point.Address,
            PickupFee = point.PickupFee,
            IsActive = point.IsActive,
            CreatedAt = point.CreatedAt,
            UpdatedAt = point.UpdatedAt
        };
    }
}