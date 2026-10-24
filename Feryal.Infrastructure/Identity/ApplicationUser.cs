using Feryal.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Feryal.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<int>
{
    public string FirstName { get; private set; } = null!;

    public string LastName { get; private set; } = null!;

    public int? DeliveryCityId { get; private set; }

    public string? Area { get; private set; }

    public string? Street { get; private set; }

    public string? BuildingNumber { get; private set; }

    public string? AdditionalDetails { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsActive { get; private set; }

    public int TokenVersion { get; private set; }

    public DeliveryCity? DeliveryCity { get; private set; }

    public Cart? Cart { get; private set; }

    public Wishlist? Wishlist { get; private set; }

    public ICollection<Order> Orders { get; private set; } =
        new List<Order>();

    public ICollection<Notification> Notifications { get; private set; } =
        new List<Notification>();

    public ICollection<CouponUsage> CouponUsages { get; private set; } =
        new List<CouponUsage>();

    public ICollection<RefreshToken> RefreshTokens { get; private set; } =
        new List<RefreshToken>();

    private ApplicationUser()
    {
    }

    public ApplicationUser(
        string firstName,
        string lastName,
        string email)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException(
                "First name is required.",
                nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException(
                "Last name is required.",
                nameof(lastName));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException(
                "Email is required.",
                nameof(email));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();

        Email = email.Trim();
        UserName = email.Trim();

        CreatedAt = DateTimeOffset.UtcNow;
        IsActive = true;
        TokenVersion = 0;
    }

    public void UpdateName(
        string firstName,
        string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException(
                "First name is required.",
                nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException(
                "Last name is required.",
                nameof(lastName));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }

    public void UpdateAddress(
        int? deliveryCityId,
        string? area,
        string? street,
        string? buildingNumber,
        string? additionalDetails)
    {
        DeliveryCityId = deliveryCityId;
        Area = area?.Trim();
        Street = street?.Trim();
        BuildingNumber = buildingNumber?.Trim();
        AdditionalDetails = additionalDetails?.Trim();
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void IncrementTokenVersion()
    {
        TokenVersion++;
    }
}