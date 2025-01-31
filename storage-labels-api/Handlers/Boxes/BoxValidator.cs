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

public class UpdateBoxValidator : AbstractValidator<UpdateBox>
{
    public UpdateBoxValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}