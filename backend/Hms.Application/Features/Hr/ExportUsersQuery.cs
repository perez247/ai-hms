using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Hr;

public sealed record ExportUsersQuery(bool? Active) : IRequest<byte[]>;

public sealed class ExportUsersQueryHandler(IHrService hr) : IRequestHandler<ExportUsersQuery, byte[]>
{
    public Task<byte[]> Handle(ExportUsersQuery request, CancellationToken cancellationToken) =>
        hr.ExportCsvAsync(request.Active, cancellationToken);
}
