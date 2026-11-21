using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Patients;

public sealed record DownloadPatientFileQuery(Guid PatientId, Guid FileId) : IRequest<PatientFileContentDto>;

public sealed class DownloadPatientFileQueryHandler(IPatientProfileService profiles)
    : IRequestHandler<DownloadPatientFileQuery, PatientFileContentDto>
{
    public Task<PatientFileContentDto> Handle(DownloadPatientFileQuery request, CancellationToken cancellationToken) =>
        profiles.DownloadFileAsync(request.PatientId, request.FileId, cancellationToken);
}

public sealed class DownloadPatientFileQueryValidator : AbstractValidator<DownloadPatientFileQuery>
{
    public DownloadPatientFileQueryValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.FileId).NotEmpty();
    }
}
