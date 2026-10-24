namespace Feryal.Domain.Entities;

public class CouponUsage
{
    private CouponUsage()
    {
    }

    public CouponUsage(
        int couponId,
        int userId,
        int orderId,
        decimal discountAmount)
    {
        if (couponId <= 0)
            throw new ArgumentException(
                "Coupon is required.",
                nameof(couponId));

        if (userId <= 0)
            throw new ArgumentException(
                "User is required.",
                nameof(userId));

        if (orderId <= 0)
            throw new ArgumentException(
                "Order is required.",
                nameof(orderId));

        if (discountAmount < 0)
            throw new ArgumentException(
                "Discount amount cannot be negative.",
                nameof(discountAmount));

        CouponId = couponId;
        UserId = userId;
        OrderId = orderId;

        DiscountAmount = discountAmount;

        UsedAt = DateTimeOffset.UtcNow;
    }

    public int Id { get; private set; }

    public int CouponId { get; private set; }

    public int UserId { get; private set; }

    public int OrderId { get; private set; }

    public decimal DiscountAmount { get; private set; }

    public DateTimeOffset UsedAt { get; private set; }

    public Coupon Coupon { get; private set; } = null!;
}