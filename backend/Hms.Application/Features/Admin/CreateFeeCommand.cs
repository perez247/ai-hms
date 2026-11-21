using System.Text.Json;
using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Admin;

public sealed record CreateFeeCommand(CreateFeeRequest Request) : IRequest<FeeDto>;

public sealed class CreateFeeCommandHandler(IAdminService admin) : IRequestHandler<CreateFeeCommand, FeeDto>
{
    public Task<FeeDto> Handle(CreateFeeCommand request, CancellationToken cancellationToken) =>
        admin.CreateFeeAsync(request.Request, cancellationToken);
}

public sealed class CreateFeeCommandValidator : AbstractValidator<CreateFeeCommand>
{
    public CreateFeeCommandValidator()
    {
        RuleFor(x => x.Request.Code).IsInEnum();
        RuleFor(x => x.Request.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Request.EffectiveTo).GreaterThan(x => x.Request.EffectiveFrom).When(x => x.Request.EffectiveTo is not null);
    }
}
