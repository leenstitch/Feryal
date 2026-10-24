# Feryal – E-Commerce Web API

Feryal is a backend-focused e-commerce platform designed for selling fragrance-related products. It provides a structured RESTful API for product management, shopping cart operations, order processing, inventory management, delivery configuration, and customer wishlists.

The project is built with ASP.NET Core and follows a layered architecture to improve separation of concerns, maintainability, and scalability.

## Features

### Product Management
- Create, update, activate, and deactivate products.
- Manage product categories and product groups.
- Support product variants based on colors, sizes, and scents.
- Manage product images, including primary images and display ordering.
- Track product prices, stock quantities, and variant availability.

### Authentication and Authorization
- User authentication using JWT access tokens.
- Refresh token management.
- ASP.NET Core Identity integration.
- Role-based access control for protected operations.
- Account activity and token validation.

### Shopping Cart and Wishlist
- Add products to the shopping cart.
- Update item quantities and remove items.
- Retrieve cart information.
- Add and manage wishlist items.

### Order Management
- Create and process customer orders.
- Manage order information and order items.
- Provide administrative order management.
- Support order status management.

### Delivery Management
- Manage delivery cities and delivery settings.
- Configure pickup points.
- Support different delivery methods.

### Inventory Management
- Track product and variant stock.
- Reserve inventory during order processing.
- Automatically expire inventory reservations.
- Use background services to handle inventory-related tasks.

### Caching and Performance
- Redis distributed caching.
- HybridCache integration.
- Cache invalidation services for products, categories, orders, delivery settings, carts, wishlists, and product groups.

### Backend Infrastructure
- Centralized exception-handling middleware.
- FluentValidation integration.
- Entity Framework Core for database access.
- Repository and Unit of Work patterns.
- Database migrations.
- Background processing with an Outbox pattern.

## Technology Stack

- **Language:** C#
- **Framework:** ASP.NET Core Web API
- **Database:** Microsoft SQL Server
- **ORM:** Entity Framework Core
- **Authentication:** ASP.NET Core Identity and JWT
- **Caching:** Redis and HybridCache
- **Image Storage:** Cloudinary
- **Validation:** FluentValidation
- **API Documentation:** Swagger
- **Architecture:** Layered Architecture

## Project Structure

The solution is organized into four projects:

```text
Feryal/
├── Feryal.API/
│   ├── Controllers/
│   ├── Contracts/
│   ├── Middleware/
│   └── Program.cs
│
├── Feryal.Application/
│   ├── DTOs/
│   ├── Interfaces/
│   ├── Services/
│   ├── Events/
│   ├── Exceptions/
│   └── Validators/
│
├── Feryal.Domain/
│   ├── Entities/
│   └── Enums/
│
├── Feryal.Infrastructure/
│   ├── Authentication/
│   ├── BackgroundServices/
│   ├── Caching/
│   ├── CloudinaryStorage/
│   ├── Identity/
│   ├── Migrations/
│   ├── Persistence/
│   ├── Products/
│   ├── Repositories/
│   └── Settings/
│
└── Feryal.slnx
```

### Architecture Layers

- **Feryal.API:** Exposes REST endpoints, handles HTTP requests, and configures middleware and authentication.
- **Feryal.Application:** Contains application services, DTOs, interfaces, validation, and application-level events.
- **Feryal.Domain:** Defines the core business entities and enumerations.
- **Feryal.Infrastructure:** Implements persistence, authentication, caching, image storage, repositories, and background services.

## Future Improvements

Potential future enhancements include automated testing, CI/CD workflows, containerization, and expanded API documentation.

