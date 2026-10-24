
//using FluentValidation;
//using Feryal.Application.Products.DTOs;

//namespace Feryal.Application.Validators;

//public class CreateProductVariantRequestValidator
//    : AbstractValidator<CreateProductVariantRequest>
//{
//    public CreateProductVariantRequestValidator()
//    {
//        RuleFor(x => x.SKU)
//            .NotEmpty()
//            .WithMessage("SKU is required.")
//            .MaximumLength(100)
//            .WithMessage("SKU cannot exceed 100 characters.");

//        RuleFor(x => x.Price)
//            .GreaterThanOrEqualTo(0)
//            .WithMessage("Price cannot be negative.");

//        RuleFor(x => x.StockQuantity)
//            .GreaterThanOrEqualTo(0)
//            .WithMessage("Stock quantity cannot be negative.");

//        RuleFor(x => x.ScentId)
//            .GreaterThan(0)
//            .When(x => x.ScentId.HasValue);

//        RuleFor(x => x.SizeId)
//            .GreaterThan(0)
//            .When(x => x.SizeId.HasValue);

//        RuleFor(x => x.ColorId)
//            .GreaterThan(0)
//            .When(x => x.ColorId.HasValue);
//    }
//}