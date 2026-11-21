using System.Text.Json;
using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Admin;

public sealed record UpdateClinicSettingCommand(string Key, UpdateSettingRequest Request) : IRequest<ClinicSettingDto>;

public sealed class UpdateClinicSettingCommandHandler(IAdminService admin)
    : IRequestHandler<UpdateClinicSettingCommand, ClinicSettingDto>
{
    public Task<ClinicSettingDto> Handle(UpdateClinicSettingCommand request, CancellationToken cancellationToken) =>
        admin.UpdateSettingAsync(request.Key, request.Request, cancellationToken);
}

public sealed class UpdateClinicSettingCommandValidator : AbstractValidator<UpdateClinicSettingCommand>
{
    public UpdateClinicSettingCommandValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.ValueJson).NotEmpty().Must(BeValidJson).WithMessage("Value must be valid JSON.");
    }

    private static bool BeValidJson(string value)
    {
        try { using var _ = JsonDocument.Parse(value); return true; }
        catch (JsonException) { return false; }
    }
}
