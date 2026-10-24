using Feryal.Domain.Enums;

namespace Feryal.Domain.Entities;

public class Coupon
{
    private readonly List<CouponUsage> _usages = new();

    private Coupon()
    {
    }

    public Coupon(
        string code,
        DiscountTarget discountTarget,
        DiscountType discountType,
        decimal value,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        int? usageLimit)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException(
                "Coupon code is required.",
                nameof(code));

        if (value <= 0)
            throw new ArgumentException(
                "Value must be greater than zero.",
                nameof(value));

        if (endDate <= startDate)
            throw new ArgumentException(
                "End date must be after start date.",
                nameof(endDate));

        if (usageLimit is <= 0)
            throw new ArgumentException(
                "Usage limit must be greater than zero.",
                nameof(usageLimit));

        Code = code.Trim().ToUpperInvariant();

        DiscountTarget = discountTarget;
        DiscountType = discountType;

        Value = value;

        StartDate = startDate;
        EndDate = endDate;

        UsageLimit = usageLimit;

        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public int Id { get; private set; }

    public string Code { get; private set; } = null!;

    public DiscountTarget DiscountTarget { get; private set; }

    public DiscountType DiscountType { get; private set; }

    public decimal Value { get; private set; }

    public DateTimeOffset StartDate { get; private set; }

    public DateTimeOffset EndDate { get; private set; }

    public int? UsageLimit { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<CouponUsage> Usages =>
        _usages.AsReadOnly();

    public bool IsValid(DateTimeOffset now)
    {
        return IsActive &&
               now >= StartDate &&
               now <= EndDate;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}