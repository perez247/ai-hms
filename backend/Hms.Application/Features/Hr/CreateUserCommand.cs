using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Hr;

/// <summary>HR action: creates the Identity account with role, salary and bank metadata.</summary>
public sealed record CreateUserCommand(CreateUserRequest Request) : IRequest<UserDto>;

public sealed class CreateUserCommandHandler(IHrService hr) : IRequestHandler<CreateUserCommand, UserDto>
{
    public Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken) =>
        hr.CreateAsync(request.Request, cancellationToken);
}

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Request.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Request.Password).NotEmpty().MinimumLength(10).MaximumLength(200);
        RuleFor(x => x.Request.StaffRole).IsInEnum();
        RuleFor(x => x.Request.EmployeeNumber).MaximumLength(50);
        RuleFor(x => x.Request.Phone).MaximumLength(40);
        RuleFor(x => x.Request.SalaryAmount).GreaterThanOrEqualTo(0).When(x => x.Request.SalaryAmount is not null);
        RuleFor(x => x.Request.BankAccountNumber).MaximumLength(50);
    }
}
