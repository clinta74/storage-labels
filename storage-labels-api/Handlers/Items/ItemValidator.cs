using FluentValidation;

namespace StorageLabelsApi.Handlers.Items;

public class CreateItemValidator : AbstractValidator<CreateItem>
{
    public CreateItemValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class UpdateItemValidator : AbstractValidator<UpdateItem>
{
    public UpdateItemValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
