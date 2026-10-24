
using System.Data;
using System.Text.Json;
using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.Delivery;
using Feryal.Application.Events.Delivery;
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Application.Interfaces;
namespace Feryal.Application.Delivery.Services;

public sealed class DeliverySettingsService : IDeliverySettingsService
{
    private readonly IRepository<DeliveryCity> _cityRepository;
    private readonly IRepository<PickupPoint> _pickupPointRepository;
    private readonly IDeliverySettingsQueryRepository _queryRepository;
    private readonly IDeliverySettingsCacheService _cacheService;
    private readonly IOutboxMessageRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionManager _transactionManager;

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

    public Task<DeliveryCityResponse?> GetCityByIdAsync(
        int cityId,
        CancellationToken cancellationToken)
    {
        ValidateId(cityId, nameof(cityId));

        return _cacheService.GetCityByIdAsync(
            cityId,
            cancellationToken);
    }

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

    public Task<PickupPointResponse?> GetPickupPointByIdAsync(
        int pickupPointId,
        CancellationToken cancellationToken)
    {
        ValidateId(pickupPointId, nameof(pickupPointId));

        return _cacheService.GetPickupPointByIdAsync(
            pickupPointId,
            cancellationToken);
    }

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

                // Save the city first to generate its ID.
                await _unitOfWork.SaveChangesAsync(token);

                AddCacheInvalidation(city.Id, null);

                // Save the outbox message in the same transaction.
                await _unitOfWork.SaveChangesAsync(token);

                return MapCity(city);
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }

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
                // A pickup point is independent of DeliveryCity.
                var point = new PickupPoint(
                    name,
                    address,
                    request.PickupFee);

                await _pickupPointRepository.AddAsync(point, token);

                // Save first to generate the pickup point ID.
                await _unitOfWork.SaveChangesAsync(token);

                AddCacheInvalidation(null, point.Id);

                await _unitOfWork.SaveChangesAsync(token);

                return MapPickupPoint(point);
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }

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

                // Activation and deactivation are both handled by PATCH.
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
