
using System.Text.Json;
using FluentValidation;
using Feryal.Application.DTOs.Categories;

namespace Feryal.Application.Validators.Categories;

public class UpdateCategoryRequestValidator
    : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(name =>
                !name.HasValue ||
                (name.Value.ValueKind == JsonValueKind.String &&
                 !string.IsNullOrWhiteSpace(name.Value.GetString())))
            .WithMessage("Category name must be a non-empty string.");

        RuleFor(x => x.Name)
            .Must(name =>
                !name.HasValue ||
                (name.Value.ValueKind == JsonValueKind.String &&
                 name.Value.GetString()!.Length <= 100))
            .WithMessage("Category name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .Must(description =>
                !description.HasValue ||
                description.Value.ValueKind == JsonValueKind.Null ||
                (description.Value.ValueKind == JsonValueKind.String &&
                 description.Value.GetString()!.Length <= 2000))
            .WithMessage(
                "Category description must be a string or null and cannot exceed 2000 characters.");
    }
}

