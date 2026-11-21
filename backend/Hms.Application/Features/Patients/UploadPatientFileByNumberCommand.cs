using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Patients;

public sealed record UploadPatientFileByNumberCommand(string PatientNumber, string FileName, string ContentType, byte[] Content)
    : IRequest<PatientFileDto>;

public sealed class UploadPatientFileByNumberCommandHandler(IPatientProfileService profiles)
    : IRequestHandler<UploadPatientFileByNumberCommand, PatientFileDto>
{
    public async Task<PatientFileDto> Handle(UploadPatientFileByNumberCommand request, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(request.Content, writable: false);
        return await profiles.UploadFileByNumberAsync(request.PatientNumber, request.FileName, request.ContentType, stream, cancellationToken);
    }
}

public sealed class UploadPatientFileByNumberCommandValidator : AbstractValidator<UploadPatientFileByNumberCommand>
{
    public UploadPatientFileByNumberCommandValidator()
    {
        RuleFor(x => x.PatientNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Content).NotEmpty().Must(x => x.Length <= 20_000_000).WithMessage("File must be 20 MB or smaller.");
    }
}
