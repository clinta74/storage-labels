using FluentValidation;

namespace StorageLabelsApi.Handlers.Items;

public class ItemValidator : AbstractValidator<CreateItem>
{
    public ItemValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
