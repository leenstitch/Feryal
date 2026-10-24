using System.Security.Claims;
using System.Text;
using CloudinaryDotNet;
using Feryal.Application.Delivery.Services;
using Feryal.Application.Interfaces;
using Feryal.Application.Products.Interfaces;
using Feryal.Application.Products.Services;
using Feryal.Application.Services;
using Feryal.Application.Services.Categories;
using Feryal.Application.Services.ProductGroups;
using Feryal.Application.Wishlist.Services;
using Feryal.Infrastructure.Authentication;
using Feryal.Infrastructure.BackgroundServices;
using Feryal.Infrastructure.Caching.Cart;
using Feryal.Infrastructure.Caching.Categories;
using Feryal.Infrastructure.Caching.Delivery;
using Feryal.Infrastructure.Caching.ProductGroups;
using Feryal.Infrastructure.Caching.Products;
using Feryal.Infrastructure.Caching.Wishlist;
using Feryal.Infrastructure.CloudinaryStorage;
using Feryal.Infrastructure.Identity;
using Feryal.Infrastructure.Persistence;
using Feryal.Infrastructure.Persistence.Migrations;
using Feryal.Infrastructure.Products;
using Feryal.Infrastructure.Repositories;
using Feryal.Infrastructure.Services;
using Feryal.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Feryal.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        var connectionString =
            configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "DefaultConnection is missing from configuration.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Identity
        services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
        {
            options.User.RequireUniqueEmail = true;

            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;

            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan =
                TimeSpan.FromMinutes(15);

            options.SignIn.RequireConfirmedEmail = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Cloudinary
        var cloudinarySettings =
            configuration.GetSection("Cloudinary")
                .Get<CloudinarySettings>()
            ?? throw new InvalidOperationException(
                "Cloudinary configuration is missing.");

        if (string.IsNullOrWhiteSpace(cloudinarySettings.CloudName) ||
            string.IsNullOrWhiteSpace(cloudinarySettings.ApiKey) ||
            string.IsNullOrWhiteSpace(cloudinarySettings.ApiSecret))
        {
            throw new InvalidOperationException(
                "Cloudinary configuration is incomplete.");
        }

        var cloudinaryAccount = new Account(
            cloudinarySettings.CloudName,
            cloudinarySettings.ApiKey,
            cloudinarySettings.ApiSecret);

        var cloudinary = new Cloudinary(cloudinaryAccount);
        cloudinary.Api.Secure = true;

        services.AddSingleton(cloudinary);

        services.AddScoped<
            ICloudinaryImageStorage,
            CloudinaryImageStorage>();

        services.AddScoped<
            IProductImageService,
            ProductImageService>();

        // Redis cache
        var redisConnection = configuration.GetConnectionString("Redis");

        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            throw new InvalidOperationException(
                "Redis connection string is missing from configuration.");
        }

        var redisOptions =
            StackExchange.Redis.ConfigurationOptions.Parse(redisConnection);

        redisOptions.AbortOnConnectFail = false;
        redisOptions.Ssl = false;

        services.AddStackExchangeRedisCache(options =>
        {
            options.ConfigurationOptions = redisOptions;
        });

        services.AddHybridCache();

        services.AddHybridCache();

        // HybridCache
        services.AddHybridCache();

        // JWT
        var jwtSection = configuration.GetSection("Jwt");

        var jwtOptions = jwtSection.Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                "JWT configuration is missing.");

        if (string.IsNullOrWhiteSpace(jwtOptions.Key) ||
            string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
            string.IsNullOrWhiteSpace(jwtOptions.Audience))
        {
            throw new InvalidOperationException(
                "JWT configuration is incomplete.");
        }

        var key = Encoding.UTF8.GetBytes(jwtOptions.Key);

        services.Configure<JwtOptions>(jwtSection);

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = jwtOptions.Issuer,
                        ValidAudience = jwtOptions.Audience,

                        IssuerSigningKey =
                            new SymmetricSecurityKey(key),

                        ClockSkew = TimeSpan.Zero
                    };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var userManager =
                            context.HttpContext.RequestServices
                                .GetRequiredService<
                                    UserManager<ApplicationUser>>();

                        var userIdClaim =
                            context.Principal?.FindFirst(
                                ClaimTypes.NameIdentifier);

                        var tokenVersionClaim =
                            context.Principal?.FindFirst("TokenVersion");

                        if (userIdClaim is null ||
                            tokenVersionClaim is null)
                        {
                            context.Fail("Invalid token.");
                            return;
                        }

                        if (!int.TryParse(
                                userIdClaim.Value,
                                out var userId) ||
                            !int.TryParse(
                                tokenVersionClaim.Value,
                                out var tokenVersion))
                        {
                            context.Fail("Invalid token.");
                            return;
                        }

                        var user = await userManager.FindByIdAsync(
                            userId.ToString());

                        if (user is null || !user.IsActive)
                        {
                            context.Fail("User is inactive.");
                            return;
                        }

                        if (user.TokenVersion != tokenVersion)
                        {
                            context.Fail("Token has been revoked.");
                        }
                    }
                };
            });

        // Background service
        services.AddHostedService<OutboxProcessor>();

        // Authentication services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<
            IRefreshTokenRepository,
            RefreshTokenRepository>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRefreshTokenHasher, RefreshTokenHasher>();

        // Generic repository and transaction services
        services.AddScoped(
            typeof(IRepository<>),
            typeof(Repository<>));

        services.AddScoped<ITransactionManager, TransactionManager>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Category services
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICategoryCacheService, CategoryCacheService>();

        // Product services
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductCacheService, ProductCacheService>();

        // Product group services
        services.AddScoped<
            IProductGroupRepository,
            ProductGroupRepository>();

        services.AddScoped<
            IProductGroupService,
            ProductGroupService>();

        services.AddScoped<
            IProductGroupCacheService,
            ProductGroupCacheService>();

        // Outbox
        services.AddScoped<
            IOutboxMessageRepository,
            OutboxMessageRepository>();
        //cheackkk
        services.AddScoped<IProductRepository, ProductRepository>();

        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductOptionService, ProductOptionService>();
        services.AddScoped<IProductVariantService, ProductVariantService>();
        services.AddScoped<IProductImageService, ProductImageService>();

        services.AddScoped<IProductCacheService, ProductCacheService>();
        services.AddScoped<ICloudinaryImageStorage, CloudinaryImageStorage>();

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<
    IDeliverySettingsQueryRepository,
    DeliverySettingsQueryRepository>();

        services.AddScoped<
            IDeliverySettingsCacheService,
            DeliverySettingsCacheService>();

        services.AddScoped<
            IDeliverySettingsService,
            DeliverySettingsService>();

        services.AddScoped<IWishlistRepository, WishlistRepository>();

        services.AddScoped<IWishlistCacheService, WishlistCacheService>();

        services.AddScoped<IWishlistService, WishlistService>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<ICartCacheService, CartCacheService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddHybridCache();

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderCacheService, OrderCacheService>();
        services.AddScoped<IOrderService, OrderService>();

        services.AddHostedService<InventoryReservationExpiryService>();
        return services;
    }
}