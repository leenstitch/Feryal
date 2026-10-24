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

// Registers infrastructure services and their dependencies in the dependency injection container.
public static class InfrastructureServiceExtensions
{
    // Registers database, authentication, caching, repositories, and application infrastructure services.
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        // Read the database connection string and validate that it exists.
        var connectionString =
            configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "DefaultConnection is missing from configuration.");
        }

        // Register the Entity Framework Core database context using SQL Server.
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Identity
        // Configure ASP.NET Core Identity for application users and integer role IDs.
        services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
        {
            // Require every account to have a unique email address.
            options.User.RequireUniqueEmail = true;

            // Configure password length and complexity requirements.
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;

            // Configure account lockout after repeated failed login attempts.
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan =
                TimeSpan.FromMinutes(15);

            // Configure whether users must confirm their email before signing in.
            options.SignIn.RequireConfirmedEmail = false;
        })
        // Store Identity data using the application's EF Core database context.
        .AddEntityFrameworkStores<ApplicationDbContext>()
        // Register the default Identity token providers.
        .AddDefaultTokenProviders();

        // Cloudinary
        // Load Cloudinary credentials from the application configuration.
        var cloudinarySettings =
            configuration.GetSection("Cloudinary")
                .Get<CloudinarySettings>()
            ?? throw new InvalidOperationException(
                "Cloudinary configuration is missing.");

        // Ensure all required Cloudinary credentials are provided.
        if (string.IsNullOrWhiteSpace(cloudinarySettings.CloudName) ||
            string.IsNullOrWhiteSpace(cloudinarySettings.ApiKey) ||
            string.IsNullOrWhiteSpace(cloudinarySettings.ApiSecret))
        {
            throw new InvalidOperationException(
                "Cloudinary configuration is incomplete.");
        }

        // Create the Cloudinary account using its configured credentials.
        var cloudinaryAccount = new Account(
            cloudinarySettings.CloudName,
            cloudinarySettings.ApiKey,
            cloudinarySettings.ApiSecret);

        // Initialize the Cloudinary client and enable secure URLs.
        var cloudinary = new Cloudinary(cloudinaryAccount);
        cloudinary.Api.Secure = true;

        // Register the Cloudinary client as a singleton.
        services.AddSingleton(cloudinary);

        // Register the service responsible for uploading and deleting Cloudinary images.
        services.AddScoped<
            ICloudinaryImageStorage,
            CloudinaryImageStorage>();

        // Register the product image management service.
        services.AddScoped<
            IProductImageService,
            ProductImageService>();

        // Redis cache
        // Read and validate the Redis connection string.
        var redisConnection = configuration.GetConnectionString("Redis");

        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            throw new InvalidOperationException(
                "Redis connection string is missing from configuration.");
        }

        // Parse Redis connection settings and configure connection behavior.
        var redisOptions =
            StackExchange.Redis.ConfigurationOptions.Parse(redisConnection);

        redisOptions.AbortOnConnectFail = false;
        redisOptions.Ssl = false;

        // Register Redis as the distributed cache provider.
        services.AddStackExchangeRedisCache(options =>
        {
            options.ConfigurationOptions = redisOptions;
        });

        // Register hybrid caching.
        services.AddHybridCache();

        // JWT
        // Load JWT configuration and validate the required settings.
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

        // Convert the signing key into bytes for token signature validation.
        var key = Encoding.UTF8.GetBytes(jwtOptions.Key);

        // Register the JWT options with the dependency injection container.
        services.Configure<JwtOptions>(jwtSection);

        // Configure JWT Bearer authentication as the default authentication and challenge scheme.
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            // Configure JWT validation and additional token verification.
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        // Validate the token issuer, audience, expiration, and signature.
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        // Specify the expected token issuer and audience.
                        ValidIssuer = jwtOptions.Issuer,
                        ValidAudience = jwtOptions.Audience,

                        // Configure the symmetric key used to validate token signatures.
                        IssuerSigningKey =
                            new SymmetricSecurityKey(key),

                        // Disable the default clock tolerance for token expiration.
                        ClockSkew = TimeSpan.Zero
                    };

                // Perform additional user and token-version checks after token validation.
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        // Resolve UserManager to retrieve the authenticated user.
                        var userManager =
                            context.HttpContext.RequestServices
                                .GetRequiredService<
                                    UserManager<ApplicationUser>>();

                        // Read the user ID and token version from the validated token.
                        var userIdClaim =
                            context.Principal?.FindFirst(
                                ClaimTypes.NameIdentifier);

                        var tokenVersionClaim =
                            context.Principal?.FindFirst("TokenVersion");

                        // Reject tokens that do not contain both required claims.
                        if (userIdClaim is null ||
                            tokenVersionClaim is null)
                        {
                            context.Fail("Invalid token.");
                            return;
                        }

                        // Ensure both claims contain valid integer values.
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

                        // Retrieve the user associated with the token.
                        var user = await userManager.FindByIdAsync(
                            userId.ToString());

                        // Reject tokens belonging to missing or inactive users.
                        if (user is null || !user.IsActive)
                        {
                            context.Fail("User is inactive.");
                            return;
                        }

                        // Reject tokens whose version no longer matches the user's current token version.
                        if (user.TokenVersion != tokenVersion)
                        {
                            context.Fail("Token has been revoked.");
                        }
                    }
                };
            });

        // Background service
        // Register the background worker responsible for processing outbox messages.
        services.AddHostedService<OutboxProcessor>();

        // Authentication services
        // Register services for JWT generation, refresh tokens, identity, and authentication.
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<
            IRefreshTokenRepository,
            RefreshTokenRepository>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRefreshTokenHasher, RefreshTokenHasher>();

        // Generic repository and transaction services
        // Register the generic repository for different entity types.
        services.AddScoped(
            typeof(IRepository<>),
            typeof(Repository<>));

        // Register transaction management and unit-of-work services.
        services.AddScoped<ITransactionManager, TransactionManager>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Category services
        // Register category business logic, data access, and caching.
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICategoryCacheService, CategoryCacheService>();

        // Product services
        // Register product business logic, data access, and caching.
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductCacheService, ProductCacheService>();

        // Product group services
        // Register product group data access, business logic, and caching.
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
        // Register the repository responsible for retrieving and managing outbox messages.
        services.AddScoped<
            IOutboxMessageRepository,
            OutboxMessageRepository>();

        //cheackkk
        services.AddScoped<IProductOptionService, ProductOptionService>();
        services.AddScoped<IProductVariantService, ProductVariantService>();

        // Register the repository for querying delivery cities and pickup points.
        services.AddScoped<
    IDeliverySettingsQueryRepository,
    DeliverySettingsQueryRepository>();

        // Register delivery settings caching and business logic services.
        services.AddScoped<
            IDeliverySettingsCacheService,
            DeliverySettingsCacheService>();

        services.AddScoped<
            IDeliverySettingsService,
            DeliverySettingsService>();

        // Register wishlist data access, caching, and business logic.
        services.AddScoped<IWishlistRepository, WishlistRepository>();

        services.AddScoped<IWishlistCacheService, WishlistCacheService>();

        services.AddScoped<IWishlistService, WishlistService>();

        // Register cart data access, business logic, and caching.
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<ICartCacheService, CartCacheService>();

        // Register the order business logic service.
        services.AddScoped<IOrderService, OrderService>();

        // Register order data access and caching.
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderCacheService, OrderCacheService>();

        // Register the background worker that expires inventory reservations and releases reserved stock.
        services.AddHostedService<InventoryReservationExpiryService>();

        // Return the service collection for further configuration.
        return services;
    }
}