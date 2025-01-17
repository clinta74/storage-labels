using FluentValidation;

namespace StorageLabelsApi.Handlers.Boxes;

public class BoxValidator : AbstractValidator<CreateBox>
{
    public BoxValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
