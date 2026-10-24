
//using FluentValidation;
//using Feryal.Application.DTOs.Products.Request;

//namespace Feryal.Application.Validators;

//public class CreateProductImageRequestValidator
//    : AbstractValidator<CreateProductImageRequest>
//{
//    public CreateProductImageRequestValidator()
//    {
//        RuleFor(x => x.ImageUrl)
//            .NotEmpty()
//            .WithMessage("Image URL is required.")
//            .MaximumLength(500)
//            .WithMessage("Image URL cannot exceed 500 characters.");

//        RuleFor(x => x.DisplayOrder)
//            .GreaterThanOrEqualTo(0)
//            .WithMessage("Display order cannot be negative.");
//    }
//}