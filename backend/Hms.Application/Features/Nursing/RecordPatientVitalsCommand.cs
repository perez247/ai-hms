using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Nursing;

public sealed record RecordPatientVitalsCommand(RecordPatientVitalsRequest Request) : IRequest<VitalDto>;

public sealed class RecordPatientVitalsCommandHandler(INurseService nurse) : IRequestHandler<RecordPatientVitalsCommand, VitalDto>
{
    public Task<VitalDto> Handle(RecordPatientVitalsCommand request, CancellationToken cancellationToken) =>
        nurse.RecordPatientVitalsAsync(request.Request, cancellationToken);
}

public sealed class RecordPatientVitalsCommandValidator : AbstractValidator<RecordPatientVitalsCommand>
{
    public RecordPatientVitalsCommandValidator()
    {
        RuleFor(x => x.Request.PatientNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Request.TemperatureC).InclusiveBetween(1m, 100m).When(x => x.Request.TemperatureC is not null);
        RuleFor(x => x.Request.Systolic).InclusiveBetween(10, 300).When(x => x.Request.Systolic is not null);
        RuleFor(x => x.Request.Diastolic).InclusiveBetween(10, 200).When(x => x.Request.Diastolic is not null);
        RuleFor(x => x.Request.Pulse).InclusiveBetween(10, 300).When(x => x.Request.Pulse is not null);
        RuleFor(x => x.Request.RespiratoryRate).InclusiveBetween(4, 100).When(x => x.Request.RespiratoryRate is not null);
        RuleFor(x => x.Request.Spo2).InclusiveBetween(0, 100).When(x => x.Request.Spo2 is not null);
        RuleFor(x => x.Request.PainScore).InclusiveBetween(0, 10).When(x => x.Request.PainScore is not null);
    }
}
