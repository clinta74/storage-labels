using FluentValidation;

namespace StorageLabelsApi.Handlers.Boxes;

public class CreateBoxValidator : AbstractValidator<CreateBox>
{
    public CreateBoxValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
