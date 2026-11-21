using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Hr;

public sealed record UpdatePayrollCommand(Guid UserId, UpdatePayrollRequest Request) : IRequest<UserDto>;

public sealed class UpdatePayrollCommandHandler(IHrService hr) : IRequestHandler<UpdatePayrollCommand, UserDto>
{
    public Task<UserDto> Handle(UpdatePayrollCommand request, CancellationToken cancellationToken) =>
        hr.UpdatePayrollAsync(request.UserId, request.Request, cancellationToken);
}

public sealed class UpdatePayrollCommandValidator : AbstractValidator<UpdatePayrollCommand>
{
    public UpdatePayrollCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request.SalaryAmount).GreaterThanOrEqualTo(0).When(x => x.Request.SalaryAmount is not null);
        RuleFor(x => x.Request.BankAccountNumber).MaximumLength(50);
    }
}
