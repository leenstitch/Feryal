
//using FluentValidation;
//using Feryal.Application.DTOs.Products;

//namespace Feryal.Application.Validators;

//public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
//{
//    public CreateProductRequestValidator()
//    {
//        RuleFor(x => x.CategoryId)
//            .GreaterThan(0)
//            .WithMessage("Category is required.");

//        RuleFor(x => x.ProductGroupId)
//            .GreaterThan(0)
//            .When(x => x.ProductGroupId.HasValue)
//            .WithMessage("Product group ID must be greater than zero.");

//        RuleFor(x => x.Name)
//            .NotEmpty()
//            .WithMessage("Product name is required.")
//            .MaximumLength(200)
//            .WithMessage("Product name cannot exceed 200 characters.");

//        RuleFor(x => x.Description)
//            .MaximumLength(2000)
//            .WithMessage("Product description cannot exceed 2000 characters.");

//        RuleFor(x => x.Variants)
//            .NotEmpty()
//            .WithMessage("At least one product variant is required.");

//        RuleForEach(x => x.Variants)
//            .SetValidator(new CreateProductVariantRequestValidator());

//        RuleForEach(x => x.Images)
//            .SetValidator(new CreateProductImageRequestValidator());
//    }
//}