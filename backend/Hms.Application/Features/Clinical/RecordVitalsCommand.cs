using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Clinical;

public sealed record RecordVitalsCommand(RecordVitalsRequest Request) : IRequest<VitalDto>;

public sealed class RecordVitalsCommandHandler(IClinicalService clinical) : IRequestHandler<RecordVitalsCommand, VitalDto>
{
    public Task<VitalDto> Handle(RecordVitalsCommand request, CancellationToken cancellationToken) =>
        clinical.RecordVitalsAsync(request.Request, cancellationToken);
}

public sealed class RecordVitalsCommandValidator : AbstractValidator<RecordVitalsCommand>
{
    public RecordVitalsCommandValidator()
    {
        RuleFor(x => x.Request.EncounterId).NotEmpty();
        RuleFor(x => x.Request.TemperatureC).InclusiveBetween(25m, 45m).When(x => x.Request.TemperatureC is not null);
        RuleFor(x => x.Request.Systolic).InclusiveBetween(40, 300).When(x => x.Request.Systolic is not null);
        RuleFor(x => x.Request.Diastolic).InclusiveBetween(20, 200).When(x => x.Request.Diastolic is not null);
        RuleFor(x => x.Request.Pulse).InclusiveBetween(20, 300).When(x => x.Request.Pulse is not null);
        RuleFor(x => x.Request.RespiratoryRate).InclusiveBetween(4, 80).When(x => x.Request.RespiratoryRate is not null);
        RuleFor(x => x.Request.Spo2).InclusiveBetween(0, 100).When(x => x.Request.Spo2 is not null);
        RuleFor(x => x.Request.PainScore).InclusiveBetween(0, 10).When(x => x.Request.PainScore is not null);
    }
}
