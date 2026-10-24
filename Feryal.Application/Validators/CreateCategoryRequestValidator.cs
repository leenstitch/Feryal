using FluentValidation;
using Feryal.Application.DTOs.Categories;

namespace Feryal.Application.Validators.Categories;

public class CreateCategoryRequestValidator
    : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Category name is required.")
            .MaximumLength(100)
            .WithMessage("Category name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .WithMessage("Category description cannot exceed 2000 characters.");
    }
}