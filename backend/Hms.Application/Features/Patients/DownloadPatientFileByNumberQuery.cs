using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Patients;

public sealed record DownloadPatientFileByNumberQuery(string PatientNumber, Guid FileId) : IRequest<PatientFileContentDto>;

public sealed class DownloadPatientFileByNumberQueryHandler(IPatientProfileService profiles)
    : IRequestHandler<DownloadPatientFileByNumberQuery, PatientFileContentDto>
{
    public Task<PatientFileContentDto> Handle(DownloadPatientFileByNumberQuery request, CancellationToken cancellationToken) =>
        profiles.DownloadFileByNumberAsync(request.PatientNumber, request.FileId, cancellationToken);
}

public sealed class DownloadPatientFileByNumberQueryValidator : AbstractValidator<DownloadPatientFileByNumberQuery>
{
    public DownloadPatientFileByNumberQueryValidator()
    {
        RuleFor(x => x.PatientNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FileId).NotEmpty();
    }
}
